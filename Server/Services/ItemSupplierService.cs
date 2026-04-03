using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface IItemSupplierService
{
    Task<List<ItemSupplierResponse>> GetAllAsync();
    Task<List<ItemSupplierResponse>> GetByItemAsync(int itemId);
    Task<ItemSupplierResponse?> GetByIdAsync(int itemId, int supplierId);
    Task<CreateItemSupplierResult> CreateAsync(CreateItemSupplierRequest request);
    Task<ItemSupplierResponse?> UpdateAsync(int itemId, int supplierId, UpdateItemSupplierRequest request);
    Task<bool> DeleteAsync(int itemId, int supplierId);
}

public class ItemSupplierService : IItemSupplierService
{
    private readonly AppDbContext _db;

    public ItemSupplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ItemSupplierResponse>> GetAllAsync()
    {
        var rows = await _db.ItemSuppliers
            .AsNoTracking()
            .Include(isp => isp.Item)
            .Include(isp => isp.Supplier)
            .ToListAsync();

        return rows.Select(MapToResponse).ToList();
    }

    public async Task<List<ItemSupplierResponse>> GetByItemAsync(int itemId)
    {
        var rows = await _db.ItemSuppliers
            .AsNoTracking()
            .Include(isp => isp.Item)
            .Include(isp => isp.Supplier)
            .Where(isp => isp.ItemId == itemId)
            .ToListAsync();

        return rows.Select(MapToResponse).ToList();
    }

    public async Task<ItemSupplierResponse?> GetByIdAsync(int itemId, int supplierId)
    {
        var row = await _db.ItemSuppliers
            .AsNoTracking()
            .Include(isp => isp.Item)
            .Include(isp => isp.Supplier)
            .FirstOrDefaultAsync(isp => isp.ItemId == itemId && isp.SupplierId == supplierId);

        return row is null ? null : MapToResponse(row);
    }

    public async Task<CreateItemSupplierResult> CreateAsync(CreateItemSupplierRequest request)
    {
        var itemExists = await _db.Items.AnyAsync(i => i.Id == request.ItemId);
        if (!itemExists)
            return new CreateItemSupplierResult(null, CreateItemSupplierError.ItemNotFound);

        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
        if (!supplierExists)
            return new CreateItemSupplierResult(null, CreateItemSupplierError.SupplierNotFound);

        var alreadyExists = await _db.ItemSuppliers.AnyAsync(
            isp => isp.ItemId == request.ItemId && isp.SupplierId == request.SupplierId);
        if (alreadyExists)
            return new CreateItemSupplierResult(null, CreateItemSupplierError.AlreadyExists);

        var row = new ItemSupplier
        {
            ItemId = request.ItemId,
            SupplierId = request.SupplierId,
            UnitPrice = request.UnitPrice,
            SupplierReference = request.SupplierReference,
            IsAvailable = request.IsAvailable,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ItemSuppliers.Add(row);
        await _db.SaveChangesAsync();

        await _db.Entry(row).Reference(isp => isp.Item).LoadAsync();
        await _db.Entry(row).Reference(isp => isp.Supplier).LoadAsync();

        return new CreateItemSupplierResult(MapToResponse(row), null);
    }

    public async Task<ItemSupplierResponse?> UpdateAsync(int itemId, int supplierId, UpdateItemSupplierRequest request)
    {
        var row = await _db.ItemSuppliers
            .Include(isp => isp.Item)
            .Include(isp => isp.Supplier)
            .FirstOrDefaultAsync(isp => isp.ItemId == itemId && isp.SupplierId == supplierId);

        if (row is null) return null;

        row.UnitPrice = request.UnitPrice;
        row.SupplierReference = request.SupplierReference;
        row.IsAvailable = request.IsAvailable;
        row.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToResponse(row);
    }

    public async Task<bool> DeleteAsync(int itemId, int supplierId)
    {
        var row = await _db.ItemSuppliers
            .FirstOrDefaultAsync(isp => isp.ItemId == itemId && isp.SupplierId == supplierId);

        if (row is null) return false;

        _db.ItemSuppliers.Remove(row);
        await _db.SaveChangesAsync();

        return true;
    }

    private static ItemSupplierResponse MapToResponse(ItemSupplier isp) => new()
    {
        ItemId = isp.ItemId,
        SupplierId = isp.SupplierId,
        ItemName = isp.Item.Name,
        SupplierName = isp.Supplier.Name,
        UnitPrice = isp.UnitPrice,
        SupplierReference = isp.SupplierReference,
        IsAvailable = isp.IsAvailable,
        UpdatedAt = isp.UpdatedAt
    };
}
