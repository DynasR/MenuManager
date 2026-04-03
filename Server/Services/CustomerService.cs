using MenuManager.Server.Data;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Server.Services;

public interface ICustomerService
{
    Task<List<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse?> GetByIdAsync(int id);
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteAsync(int id);
}

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerResponse>> GetAllAsync()
    {
        var customers = await _db.Customers
            .AsNoTracking()
            .ToListAsync();

        return customers.Select(MapToResponse).ToList();
    }

    public async Task<CustomerResponse?> GetByIdAsync(int id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return customer is null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            CreatedAt = now,
            UpdatedAt = now,
            PasswordHash = Array.Empty<byte>(),
            PasswordSalt = Array.Empty<byte>()
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return MapToResponse(customer);
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return null;

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.PostalCode = request.PostalCode;
        customer.Country = request.Country;
        customer.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToResponse(customer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.MenuPlans)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return false;
        if (customer.MenuPlans.Count > 0) return false;

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        return true;
    }

    private static CustomerResponse MapToResponse(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        City = c.City,
        PostalCode = c.PostalCode,
        Country = c.Country,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
