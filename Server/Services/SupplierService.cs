using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface ISupplierService
{
    Task<List<SupplierResponse>> GetAllAsync();
    Task<SupplierResponse?> GetByIdAsync(int id);
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request);
    Task<SupplierResponse?> UpdateAsync(int id, UpdateSupplierRequest request);
    Task<bool> DeleteAsync(int id);
}

public class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SupplierResponse>> GetAllAsync()
    {
        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .ToListAsync();

        return suppliers.Select(MapToResponse).ToList();
    }

    public async Task<SupplierResponse?> GetByIdAsync(int id)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        return supplier is null ? null : MapToResponse(supplier);
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        var now = DateTime.UtcNow;
        var supplier = new Supplier
        {
            Name = request.Name,
            CompanyName = request.CompanyName,
            Siret = request.Siret,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            PaymentType = request.PaymentType,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return MapToResponse(supplier);
    }

    public async Task<SupplierResponse?> UpdateAsync(int id, UpdateSupplierRequest request)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier is null) return null;

        supplier.Name = request.Name;
        supplier.CompanyName = request.CompanyName;
        supplier.Siret = request.Siret;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.City = request.City;
        supplier.PostalCode = request.PostalCode;
        supplier.Country = request.Country;
        supplier.PaymentType = request.PaymentType;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToResponse(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _db.Suppliers
            .Include(s => s.ItemSuppliers)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return false;
        if (supplier.ItemSuppliers.Count > 0) return false;

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();

        return true;
    }

    private static SupplierResponse MapToResponse(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        CompanyName = s.CompanyName,
        Siret = s.Siret,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        City = s.City,
        PostalCode = s.PostalCode,
        Country = s.Country,
        PaymentType = s.PaymentType,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
