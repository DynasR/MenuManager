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

        var mealSlotItem = new MealSlotItem
        {
            Quantity = request.Quantity,
            Notes = request.Notes,
            MealSlotId = request.MealSlotId,
            ItemId = request.ItemId
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static MealSlotItemResponse MapToResponse(MealSlotItem msi) => new()
    {
        Id = msi.Id,
        ItemId = msi.ItemId,
        ItemName = msi.Item.Name,
        Quantity = msi.Quantity,
        Notes = msi.Notes,
        MealSlotId = msi.MealSlotId
    };
}
