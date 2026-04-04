using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class MealSlotItemServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MealSlotItemService _service;

    public MealSlotItemServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new MealSlotItemService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private async Task<Item> SeedItemAsync(string name = "Rice")
    {
        var cat = new Category { Name = "Food" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = name,
            Unit = MeasurementUnit.Piece,
            PackageSize = 1,
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    private async Task<MealSlot> SeedMealSlotAsync()
    {
        var customer = new Customer
        {
            Name = "Alice",
            PasswordHash = [],
            PasswordSalt = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        var plan = new MenuPlan
        {
            Name = "Plan",
            Month = 1,
            Year = 2026,
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.MenuPlans.Add(plan);
        await _db.SaveChangesAsync();

        var dayPlan = new DayPlan
        {
            Date = new DateOnly(2026, 1, 15),
            MenuPlanId = plan.Id
        };
        _db.DayPlans.Add(dayPlan);
        await _db.SaveChangesAsync();

        var mealSlot = new MealSlot
        {
            MealType = MealType.Breakfast,
            DayPlanId = dayPlan.Id
        };
        _db.MealSlots.Add(mealSlot);
        await _db.SaveChangesAsync();
        return mealSlot;
    }

    private async Task<MealSlotItem> SeedMealSlotItemAsync(MealSlot mealSlot, Item item)
    {
        var msi = new MealSlotItem
        {
            Quantity = 2.5m,
            Notes = "test note",
            MealSlotId = mealSlot.Id,
            ItemId = item.Id
        };
        _db.MealSlotItems.Add(msi);
        await _db.SaveChangesAsync();
        return msi;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMealSlotItems()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].ItemName.Should().Be(item.Name);
        result[0].Quantity.Should().Be(2.5m);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMealSlotItem()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.GetByIdAsync(msi.Id);

        result.Should().NotBeNull();
        result!.ItemName.Should().Be(item.Name);
        result.Quantity.Should().Be(2.5m);
        result.Notes.Should().Be("test note");
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenMealSlotNotFound()
    {
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = 999,
            ItemId = item.Id,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenItemNotFound()
    {
        var mealSlot = await SeedMealSlotAsync();

        var result = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id,
            ItemId = 999,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsWithCorrectData()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id,
            ItemId = item.Id,
            Quantity = 3.5m,
            Notes = "some notes"
        });

        result.Should().NotBeNull();
        result!.ItemId.Should().Be(item.Id);
        result.ItemName.Should().Be(item.Name);
        result.Quantity.Should().Be(3.5m);
        result.Notes.Should().Be("some notes");
        result.MealSlotId.Should().Be(mealSlot.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateMealSlotItemRequest
        {
            MealSlotId = 1,
            ItemId = 1,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenMealSlotNotFound()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.UpdateAsync(msi.Id, new UpdateMealSlotItemRequest
        {
            MealSlotId = 999,
            ItemId = item.Id,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenItemNotFound()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.UpdateAsync(msi.Id, new UpdateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id,
            ItemId = 999,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsUpdatedItem()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.UpdateAsync(msi.Id, new UpdateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id,
            ItemId = item.Id,
            Quantity = 10m,
            Notes = "updated"
        });

        result.Should().NotBeNull();
        result!.Quantity.Should().Be(10m);
        result.Notes.Should().Be("updated");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesMealSlotItem()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.DeleteAsync(msi.Id);

        result.Should().BeTrue();
        _db.MealSlotItems.Should().BeEmpty();
    }
}
