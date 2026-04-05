using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IItemService
{
    Task<List<ItemResponse>> GetAllAsync();
    Task<List<ItemResponse>> GetByCategoryAsync(int categoryId);
    Task<ItemResponse?> GetByIdAsync(int id);
    Task<ItemResponse?> CreateAsync(CreateItemRequest request);
    Task<ItemResponse?> UpdateAsync(int id, UpdateItemRequest request);
    Task<bool> DeleteAsync(int id);
}

public class ItemService : IItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ItemResponse>> GetAllAsync()
    {
        var items = await _db.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<List<ItemResponse>> GetByCategoryAsync(int categoryId)
    {
        var items = await _db.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.CategoryId == categoryId)
            .ToListAsync();

        return items.Select(MapToResponse).ToList();
    }

    public async Task<ItemResponse?> GetByIdAsync(int id)
    {
        var item = await _db.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);

        return item is null ? null : MapToResponse(item);
    }

    public async Task<ItemResponse?> CreateAsync(CreateItemRequest request)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return null;

        var now = DateTime.UtcNow;
        var item = new Item
        {
            Name = request.Name,
            Description = request.Description,
            PurchaseUnit = request.PurchaseUnit,
            ContentQuantity = request.ContentQuantity,
            ContentUnit = request.ContentUnit,
            CategoryId = request.CategoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(i => i.Category).LoadAsync();

        return MapToResponse(item);
    }

    public async Task<ItemResponse?> UpdateAsync(int id, UpdateItemRequest request)
    {
        var item = await _db.Items
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null) return null;

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return null;

        item.Name = request.Name;
        item.Description = request.Description;
        item.PurchaseUnit = request.PurchaseUnit;
        item.ContentQuantity = request.ContentQuantity;
        item.ContentUnit = request.ContentUnit;
        item.CategoryId = request.CategoryId;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(i => i.Category).LoadAsync();

        return MapToResponse(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _db.Items
            .Include(i => i.ItemSuppliers)
            .Include(i => i.MealItems)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null) return false;
        if (item.ItemSuppliers.Count > 0) return false;
        if (item.MealItems.Count > 0) return false;

        _db.Items.Remove(item);
        await _db.SaveChangesAsync();

        return true;
    }

    private static ItemResponse MapToResponse(Item i) => new()
    {
        Id = i.Id,
        Name = i.Name,
        Description = i.Description,
        PurchaseUnit = i.PurchaseUnit,
        ContentQuantity = i.ContentQuantity,
        ContentUnit = i.ContentUnit,
        CategoryId = i.CategoryId,
        CategoryName = i.Category.Name,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };
}
