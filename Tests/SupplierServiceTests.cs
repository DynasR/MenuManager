using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class SupplierServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SupplierService _service;

    public SupplierServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new SupplierService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Supplier> SeedSupplierAsync(string name = "ACME")
    {
        var supplier = new Supplier
        {
            Name = name,
            CompanyName = "ACME Corp",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return supplier;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSuppliers()
    {
        await SeedSupplierAsync("Alpha");
        await SeedSupplierAsync("Beta");

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturns_WithCreatedAtPopulated()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await _service.CreateAsync(new CreateSupplierRequest
        {
            Name = "FreshSupplier",
            CompanyName = "Fresh Co",
            Email = "contact@fresh.com"
        });

        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("FreshSupplier");
        result.CompanyName.Should().Be("Fresh Co");
        result.Email.Should().Be("contact@fresh.com");
        result.CreatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateSupplierRequest { Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsCorrectly()
    {
        var supplier = await SeedSupplierAsync("OldName");

        var result = await _service.UpdateAsync(supplier.Id, new UpdateSupplierRequest
        {
            Name = "NewName",
            CompanyName = "New Corp",
            City = "Paris",
            Email = "new@corp.com"
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("NewName");
        result.CompanyName.Should().Be("New Corp");
        result.City.Should().Be("Paris");
        result.Email.Should().Be("new@corp.com");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenSupplierHasItemSuppliers()
    {
        var supplier = await SeedSupplierAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = 1,            // fake — InMemory does not enforce FK
            SupplierId = supplier.Id,
            UnitPrice = 10.00m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(supplier.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves_WhenClean()
    {
        var supplier = await SeedSupplierAsync();

        var result = await _service.DeleteAsync(supplier.Id);

        result.Should().BeTrue();
        _db.Suppliers.Should().BeEmpty();
    }
}
