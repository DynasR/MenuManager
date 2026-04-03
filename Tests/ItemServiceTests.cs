using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class ItemServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new ItemService(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Category> SeedCategoryAsync(string name = "Food")
    {
        var cat = new Category { Name = name };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return cat;
    }

    private async Task<Item> SeedItemAsync(int categoryId, string name = "Rice")
    {
        var item = new Item
        {
            Name = name,
            Unit = "kg",
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems_WithCategoryName()
    {
        var cat = await SeedCategoryAsync("Dairy");
        await SeedItemAsync(cat.Id, "Milk");
        await SeedItemAsync(cat.Id, "Cheese");

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.CategoryName.Should().Be("Dairy"));
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsOnlyItemsOfGivenCategory()
    {
        var cat1 = await SeedCategoryAsync("Grains");
        var cat2 = await SeedCategoryAsync("Dairy");
        await SeedItemAsync(cat1.Id, "Rice");
        await SeedItemAsync(cat1.Id, "Wheat");
        await SeedItemAsync(cat2.Id, "Milk");

        var result = await _service.GetByCategoryAsync(cat1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.CategoryId.Should().Be(cat1.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtAndUpdatedAt()
    {
        var cat = await SeedCategoryAsync();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await _service.CreateAsync(new CreateItemRequest
        {
            Name = "Sugar",
            Unit = "kg",
            CategoryId = cat.Id
        });

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeAfter(before);
        result.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateItemRequest
        {
            Name = "X",
            Unit = "kg",
            CategoryId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenItemHasItemSuppliers()
    {
        var cat = await SeedCategoryAsync();
        var item = await SeedItemAsync(cat.Id);

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = 1,   // fake — InMemory does not enforce FK
            UnitPrice = 5.00m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(item.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenItemHasMealSlotItems()
    {
        var cat = await SeedCategoryAsync();
        var item = await SeedItemAsync(cat.Id);

        _db.MealSlotItems.Add(new MealSlotItem
        {
            ItemId = item.Id,
            MealSlotId = 1,   // fake — InMemory does not enforce FK
            Quantity = 2
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(item.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves_WhenClean()
    {
        var cat = await SeedCategoryAsync();
        var item = await SeedItemAsync(cat.Id);

        var result = await _service.DeleteAsync(item.Id);

        result.Should().BeTrue();
        _db.Items.Should().BeEmpty();
    }
}
