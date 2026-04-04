using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IMealSlotItemService
{
    Task<List<MealSlotItemResponse>> GetAllAsync();
    Task<MealSlotItemResponse?> GetByIdAsync(int id);
    Task<MealSlotItemResponse?> CreateAsync(CreateMealSlotItemRequest request);
    Task<MealSlotItemResponse?> UpdateAsync(int id, UpdateMealSlotItemRequest request);
    Task<bool> DeleteAsync(int id);
    Task<MealSlotItemResponse?> MoveAsync(int id, MoveMealSlotItemRequest request);
    Task<bool> ReorderAsync(ReorderMealSlotItemsRequest request);
}

public class MealSlotItemService : IMealSlotItemService
{
    private readonly AppDbContext _db;

    public MealSlotItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<MealSlotItemResponse>> GetAllAsync()
    {
        var items = await _db.MealSlotItems
            .Include(msi => msi.Item)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<MealSlotItemResponse?> GetByIdAsync(int id)
    {
        var item = await _db.MealSlotItems
            .Include(msi => msi.Item)
            .AsNoTracking()
            .FirstOrDefaultAsync(msi => msi.Id == id);

        return item is null ? null : MapToResponse(item);
    }

    public async Task<MealSlotItemResponse?> CreateAsync(CreateMealSlotItemRequest request)
    {
        var mealSlotExists = await _db.MealSlots.AnyAsync(ms => ms.Id == request.MealSlotId);
        if (!mealSlotExists) return null;

        var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists) return null;

        var currentCount = await _db.MealSlotItems.CountAsync(msi => msi.MealSlotId == request.MealSlotId);

        var mealSlotItem = new MealSlotItem
        {
            Quantity = request.Quantity,
            Notes = request.Notes,
            MealSlotId = request.MealSlotId,
            ItemId = request.ItemId,
            Order = currentCount + 1
        };

        _db.MealSlotItems.Add(mealSlotItem);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(mealSlotItem.Id);
    }

    public async Task<MealSlotItemResponse?> UpdateAsync(int id, UpdateMealSlotItemRequest request)
    {
        var mealSlotItem = await _db.MealSlotItems.FindAsync(id);
        if (mealSlotItem is null) return null;

        var mealSlotExists = await _db.MealSlots.AnyAsync(ms => ms.Id == request.MealSlotId);
        if (!mealSlotExists) return null;

        var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists) return null;

        mealSlotItem.Quantity = request.Quantity;
        mealSlotItem.Notes = request.Notes;
        mealSlotItem.MealSlotId = request.MealSlotId;
        mealSlotItem.ItemId = request.ItemId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mealSlotItem = await _db.MealSlotItems.FindAsync(id);
        if (mealSlotItem is null) return false;

        _db.MealSlotItems.Remove(mealSlotItem);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<MealSlotItemResponse?> MoveAsync(int id, MoveMealSlotItemRequest request)
    {
        var mealSlotItem = await _db.MealSlotItems
            .Include(msi => msi.MealSlot)
                .ThenInclude(ms => ms.DayPlan)
            .FirstOrDefaultAsync(msi => msi.Id == id);

        if (mealSlotItem is null) return null;

        var sourceSlotId = mealSlotItem.MealSlotId;
        var menuPlanId = mealSlotItem.MealSlot.DayPlan.MenuPlanId;

        // Resolve target DayPlan (on-demand)
        var targetDayPlan = await _db.DayPlans
            .FirstOrDefaultAsync(dp => dp.MenuPlanId == menuPlanId && dp.Date == request.TargetDate);

        if (targetDayPlan is null)
        {
            targetDayPlan = new DayPlan { Date = request.TargetDate, MenuPlanId = menuPlanId };
            _db.DayPlans.Add(targetDayPlan);
            await _db.SaveChangesAsync();
        }

        // Resolve target MealSlot (on-demand)
        var targetSlot = await _db.MealSlots
            .FirstOrDefaultAsync(ms => ms.DayPlanId == targetDayPlan.Id && ms.MealType == request.TargetMealType);

        if (targetSlot is null)
        {
            targetSlot = new MealSlot { DayPlanId = targetDayPlan.Id, MealType = request.TargetMealType };
            _db.MealSlots.Add(targetSlot);
            await _db.SaveChangesAsync();
        }

        // Move item to target slot
        mealSlotItem.MealSlotId = targetSlot.Id;
        await _db.SaveChangesAsync();

        // Renumber source slot (fill the gap left by removed item)
        if (sourceSlotId != targetSlot.Id)
            await RenumberSlotAsync(sourceSlotId);

        // Renumber target slot with item inserted at desired position
        var targetItems = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == targetSlot.Id)
            .OrderBy(msi => msi.Order)
            .ToListAsync();

        targetItems.Remove(mealSlotItem);
        var insertIdx = Math.Clamp(request.NewOrder, 0, targetItems.Count);
        targetItems.Insert(insertIdx, mealSlotItem);

        for (int i = 0; i < targetItems.Count; i++)
            targetItems[i].Order = i + 1;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> ReorderAsync(ReorderMealSlotItemsRequest request)
    {
        var items = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == request.MealSlotId)
            .ToListAsync();

        if (items.Count == 0) return false;
        if (request.OrderedItemIds.Count != items.Count) return false;

        var itemDict = items.ToDictionary(i => i.Id);

        for (int i = 0; i < request.OrderedItemIds.Count; i++)
        {
            if (!itemDict.TryGetValue(request.OrderedItemIds[i], out var item))
                return false;

            item.Order = i + 1;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task RenumberSlotAsync(int slotId)
    {
        var items = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == slotId)
            .OrderBy(msi => msi.Order)
            .ToListAsync();

        for (int i = 0; i < items.Count; i++)
            items[i].Order = i + 1;

        await _db.SaveChangesAsync();
    }

    private static MealSlotItemResponse MapToResponse(MealSlotItem msi) => new()
    {
        Id = msi.Id,
        ItemId = msi.ItemId ?? 0,
        ItemName = msi.Item?.Name ?? "",
        Quantity = msi.Quantity,
        Notes = msi.Notes,
        Order = msi.Order,
        MealSlotId = msi.MealSlotId
    };
}
