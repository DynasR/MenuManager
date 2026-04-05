using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class ItemSupplierServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ItemSupplierService _service;

    public ItemSupplierServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ItemSupplierService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Item> SeedItemAsync(string name = "Rice")
    {
        var cat = new Category { Name = "Food" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = name,
            PurchaseUnit = MeasurementUnit.Piece,
            ContentQuantity = 1,
            ContentUnit = MeasurementUnit.Piece,
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<Supplier> SeedSupplierAsync(string name = "ACME", PaymentType paymentType = PaymentType.CB)
    {
        var supplier = new Supplier
        {
            Name = name,
            CompanyName = name,
            PaymentType = paymentType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return supplier;
    }

    private async Task<(Item, Supplier)> SeedItemSupplierAsync()
    {
        var item = await SeedItemAsync();
        var supplier = await SeedSupplierAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 5.00m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (item, supplier);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItemSuppliers_WithItemAndSupplierNames()
    {
        var (item, supplier) = await SeedItemSupplierAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].ItemName.Should().Be(item.Name);
        result[0].SupplierName.Should().Be(supplier.Name);
    }

    [Fact]
    public async Task GetByItemAsync_ReturnsOnlyItemSuppliersForGivenItem()
    {
        var (item, supplier) = await SeedItemSupplierAsync();
        // seed a second item with its own ItemSupplier
        var item2 = await SeedItemAsync("Wheat");
        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item2.Id,
            SupplierId = supplier.Id,
            UnitPrice = 3.00m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByItemAsync(item.Id);

        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(item.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownCombo()
    {
        var result = await _service.GetByIdAsync(999, 999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsItemNotFound_WhenItemDoesNotExist()
    {
        var supplier = await SeedSupplierAsync();

        var result = await _service.CreateAsync(new CreateItemSupplierRequest
        {
            ItemId = 999,
            SupplierId = supplier.Id,
            UnitPrice = 10m
        });

        result.Error.Should().Be(CreateItemSupplierError.ItemNotFound);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsSupplierNotFound_WhenSupplierDoesNotExist()
    {
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new CreateItemSupplierRequest
        {
            ItemId = item.Id,
            SupplierId = 999,
            UnitPrice = 10m
        });

        result.Error.Should().Be(CreateItemSupplierError.SupplierNotFound);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsAlreadyExists_WhenComboAlreadyExists()
    {
        var (item, supplier) = await SeedItemSupplierAsync();

        var result = await _service.CreateAsync(new CreateItemSupplierRequest
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 10m
        });

        result.Error.Should().Be(CreateItemSupplierError.AlreadyExists);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsWithCorrectData()
    {
        var item = await SeedItemAsync();
        var supplier = await SeedSupplierAsync();

        var result = await _service.CreateAsync(new CreateItemSupplierRequest
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 7.50m,
            SupplierReference = "REF-001",
            IsAvailable = true
        });

        result.Error.Should().BeNull();
        result.Response.Should().NotBeNull();
        result.Response!.ItemId.Should().Be(item.Id);
        result.Response.SupplierId.Should().Be(supplier.Id);
        result.Response.UnitPrice.Should().Be(7.50m);
        result.Response.SupplierReference.Should().Be("REF-001");
        result.Response.IsAvailable.Should().BeTrue();
        result.Response.ItemName.Should().Be(item.Name);
        result.Response.SupplierName.Should().Be(supplier.Name);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownCombo()
    {
        var result = await _service.UpdateAsync(999, 999, new UpdateItemSupplierRequest { UnitPrice = 1m });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves()
    {
        var (item, supplier) = await SeedItemSupplierAsync();

        var result = await _service.DeleteAsync(item.Id, supplier.Id);

        result.Should().BeTrue();
        _db.ItemSuppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForNonExistentCombo()
    {
        var result = await _service.DeleteAsync(999, 999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByItemsAsync_ReturnsEmptyList_ForEmptyRequest()
    {
        var result = await _service.GetByItemsAsync([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByItemsAsync_ReturnsOnlyAvailableSuppliersForRequestedItems()
    {
        var item = await SeedItemAsync("Rice");
        var supplier = await SeedSupplierAsync("ACME");
        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 2.50m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        // unavailable supplier — should be excluded
        var supplier2 = await SeedSupplierAsync("OtherCo");
        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier2.Id,
            UnitPrice = 1.00m,
            IsAvailable = false,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByItemsAsync([item.Id]);

        result.Should().HaveCount(1);
        result[0].ItemId.Should().Be(item.Id);
        result[0].UnitPrice.Should().Be(2.50m);
        result[0].ContentQuantity.Should().Be(item.ContentQuantity);
    }

    [Fact]
    public async Task GetByItemsAsync_ReturnsEmpty_ForItemWithNoSuppliers()
    {
        var item = await SeedItemAsync("Lone Item");

        var result = await _service.GetByItemsAsync([item.Id]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByItemsAsync_TwoSuppliersDifferentPaymentType_RetainedIsTheCheapest()
    {
        var item = await SeedItemAsync("Dual Item");
        var trSupplier = await SeedSupplierAsync("Carrefour", PaymentType.TR);
        var cbSupplier = await SeedSupplierAsync("Leclerc", PaymentType.CB);

        _db.ItemSuppliers.AddRange(
            new ItemSupplier { ItemId = item.Id, SupplierId = trSupplier.Id, UnitPrice = 3.00m, IsAvailable = true, UpdatedAt = DateTime.UtcNow },
            new ItemSupplier { ItemId = item.Id, SupplierId = cbSupplier.Id, UnitPrice = 1.50m, IsAvailable = true, UpdatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var result = await _service.GetByItemsAsync([item.Id]);

        result.Should().HaveCount(2);
        var cheapest = result.MinBy(r => r.UnitPrice);
        cheapest!.UnitPrice.Should().Be(1.50m);
        cheapest.Supplier.PaymentType.Should().Be(PaymentType.CB);
    }
}
