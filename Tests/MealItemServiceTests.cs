using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class MealItemServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MealItemService _service;

    public MealItemServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new MealItemService(_db);
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

    private async Task<Meal> SeedMealAsync()
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

        var dailyMenu = new DailyMenu
        {
            Date = new DateOnly(2026, 1, 15),
            CustomerId = customer.Id
        };
        _db.DailyMenus.Add(dailyMenu);
        await _db.SaveChangesAsync();

        var meal = new Meal
        {
            MealType = MealType.Breakfast,
            DailyMenuId = dailyMenu.Id
        };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();
        return meal;
    }

    private async Task<MealItem> SeedMealItemAsync(Meal meal, Item item)
    {
        var mi = new MealItem
        {
            Quantity = 2.5m,
            Notes = "test note",
            MealId = meal.Id,
            ItemId = item.Id
        };
        _db.MealItems.Add(mi);
        await _db.SaveChangesAsync();
        return mi;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMealItems()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        await SeedMealItemAsync(meal, item);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].ItemName.Should().Be(item.Name);
        result[0].Quantity.Should().Be(2.5m);
        result[0].PackageSize.Should().Be(1);
        result[0].Unit.Should().Be(MeasurementUnit.Piece);
        result[0].UnitPrice.Should().BeNull();
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMealItem()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.GetByIdAsync(mi.Id);

        result.Should().NotBeNull();
        result!.ItemName.Should().Be(item.Name);
        result.Quantity.Should().Be(2.5m);
        result.Notes.Should().Be("test note");
        result.PackageSize.Should().Be(1);
        result.Unit.Should().Be(MeasurementUnit.Piece);
        result.UnitPrice.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenMealNotFound()
    {
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = 999,
            ItemId = item.Id,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenItemNotFound()
    {
        var meal = await SeedMealAsync();

        var result = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id,
            ItemId = 999,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsWithCorrectData()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id,
            ItemId = item.Id,
            Quantity = 3.5m,
            Notes = "some notes"
        });

        result.Should().NotBeNull();
        result!.ItemId.Should().Be(item.Id);
        result.ItemName.Should().Be(item.Name);
        result.Quantity.Should().Be(3.5m);
        result.Notes.Should().Be("some notes");
        result.MealId.Should().Be(meal.Id);
        result.PackageSize.Should().Be(1);
        result.Unit.Should().Be(MeasurementUnit.Piece);
        result.UnitPrice.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsUnitPrice_WhenAvailableSupplierExists()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();

        var supplier = new Supplier
        {
            Name = "Supplier A",
            CompanyName = "Co A",
            Siret = "12345678901234",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 2.50m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id,
            ItemId = item.Id,
            Quantity = 1m
        });

        result.Should().NotBeNull();
        result!.UnitPrice.Should().Be(2.50m);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullUnitPrice_WhenNoAvailableSupplier()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var supplier = new Supplier
        {
            Name = "Supplier B",
            CompanyName = "Co B",
            Siret = "12345678901235",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 3.00m,
            IsAvailable = false,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(mi.Id);

        result.Should().NotBeNull();
        result!.UnitPrice.Should().BeNull();
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateMealItemRequest
        {
            MealId = 1,
            ItemId = 1,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenMealNotFound()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.UpdateAsync(mi.Id, new UpdateMealItemRequest
        {
            MealId = 999,
            ItemId = item.Id,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenItemNotFound()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.UpdateAsync(mi.Id, new UpdateMealItemRequest
        {
            MealId = meal.Id,
            ItemId = 999,
            Quantity = 1m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsUpdatedItem()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.UpdateAsync(mi.Id, new UpdateMealItemRequest
        {
            MealId = meal.Id,
            ItemId = item.Id,
            Quantity = 10m,
            Notes = "updated"
        });

        result.Should().NotBeNull();
        result!.Quantity.Should().Be(10m);
        result.Notes.Should().Be("updated");
    }

    // ── MoveAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.MoveAsync(999, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 16),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task MoveAsync_MovesToSameSlot_UpdatesOrder()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.MoveAsync(mi.Id, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Breakfast,
            NewOrder = 0
        });

        result.Should().NotBeNull();
        result!.MealId.Should().Be(meal.Id);
        var entity = await _db.MealItems.FindAsync(mi.Id);
        entity!.Order.Should().Be(1);
    }

    [Fact]
    public async Task MoveAsync_MovesToDifferentMeal_ExistingMeal()
    {
        var meal = await SeedMealAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        // Create a Lunch meal on the same day
        var dailyMenu = await _db.DailyMenus.FirstAsync();
        var lunchMeal = new Meal { MealType = MealType.Lunch, DailyMenuId = dailyMenu.Id };
        _db.Meals.Add(lunchMeal);
        await _db.SaveChangesAsync();

        var result = await _service.MoveAsync(mi.Id, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        result.Should().NotBeNull();
        result!.MealId.Should().Be(lunchMeal.Id);
    }

    [Fact]
    public async Task MoveAsync_CreatesMealOnDemand_WhenTargetMealMissing()
    {
        var meal = await SeedMealAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.MoveAsync(mi.Id, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Dinner,
            NewOrder = 0
        });

        result.Should().NotBeNull();
        var newMeal = await _db.Meals
            .FirstOrDefaultAsync(m => m.MealType == MealType.Dinner);
        newMeal.Should().NotBeNull();
        result!.MealId.Should().Be(newMeal!.Id);
    }

    [Fact]
    public async Task MoveAsync_CreatesDailyMenuAndMealOnDemand_WhenTargetDateMissing()
    {
        var meal = await SeedMealAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var targetDate = new DateOnly(2026, 1, 20);

        var result = await _service.MoveAsync(mi.Id, new MoveMealItemRequest
        {
            TargetDate = targetDate,
            TargetMealType = MealType.Lunch,
            NewOrder = 1
        });

        result.Should().NotBeNull();
        var newDailyMenu = await _db.DailyMenus
            .FirstOrDefaultAsync(dm => dm.Date == targetDate);
        newDailyMenu.Should().NotBeNull();
        var newMeal = await _db.Meals
            .FirstOrDefaultAsync(m => m.DailyMenuId == newDailyMenu!.Id && m.MealType == MealType.Lunch);
        newMeal.Should().NotBeNull();
        result!.MealId.Should().Be(newMeal!.Id);
    }

    // ── ReorderAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderAsync_ReordersItemsCorrectly()
    {
        var meal = await SeedMealAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var mi1 = await SeedMealItemAsync(meal, item1);
        var mi2 = await SeedMealItemAsync(meal, item2);
        var mi3 = await SeedMealItemAsync(meal, item3);

        var result = await _service.ReorderAsync(new ReorderMealItemsRequest
        {
            MealId = meal.Id,
            OrderedItemIds = [mi3.Id, mi1.Id, mi2.Id]
        });

        result.Should().BeTrue();
        (await _db.MealItems.FindAsync(mi3.Id))!.Order.Should().Be(1);
        (await _db.MealItems.FindAsync(mi1.Id))!.Order.Should().Be(2);
        (await _db.MealItems.FindAsync(mi2.Id))!.Order.Should().Be(3);
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenUnknownIdInList()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.ReorderAsync(new ReorderMealItemsRequest
        {
            MealId = meal.Id,
            OrderedItemIds = [mi.Id, 999]
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenMealEmpty()
    {
        var meal = await SeedMealAsync();

        var result = await _service.ReorderAsync(new ReorderMealItemsRequest
        {
            MealId = meal.Id,
            OrderedItemIds = []
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenPartialList()
    {
        var meal = await SeedMealAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var mi1 = await SeedMealItemAsync(meal, item1);
        await SeedMealItemAsync(meal, item2);

        var result = await _service.ReorderAsync(new ReorderMealItemsRequest
        {
            MealId = meal.Id,
            OrderedItemIds = [mi1.Id]
        });

        result.Should().BeFalse();
    }

    // ── CreateAsync Order ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AssignsSequentialOrder()
    {
        var meal = await SeedMealAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");

        var r1 = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id, ItemId = item1.Id, Quantity = 1
        });
        var r2 = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id, ItemId = item2.Id, Quantity = 1
        });
        var r3 = await _service.CreateAsync(new CreateMealItemRequest
        {
            MealId = meal.Id, ItemId = item3.Id, Quantity = 1
        });

        r1!.Order.Should().Be(1);
        r2!.Order.Should().Be(2);
        r3!.Order.Should().Be(3);
    }

    // ── MoveAsync renumbering ────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_RenumbersSourceMeal_AfterMove()
    {
        var meal = await SeedMealAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var mi1 = await SeedMealItemAsync(meal, item1);
        var mi2 = await SeedMealItemAsync(meal, item2);
        var mi3 = await SeedMealItemAsync(meal, item3);
        mi1.Order = 1; mi2.Order = 2; mi3.Order = 3;
        await _db.SaveChangesAsync();

        await _service.MoveAsync(mi2.Id, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        var source = await _db.MealItems
            .Where(mi => mi.MealId == meal.Id)
            .OrderBy(mi => mi.Order)
            .ToListAsync();
        source.Should().HaveCount(2);
        source[0].Id.Should().Be(mi1.Id);
        source[0].Order.Should().Be(1);
        source[1].Id.Should().Be(mi3.Id);
        source[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task MoveAsync_InsertsAtCorrectPosition_InTargetMeal()
    {
        var meal = await SeedMealAsync(); // Breakfast
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var mi1 = await SeedMealItemAsync(meal, item1);
        var mi2 = await SeedMealItemAsync(meal, item2);
        mi1.Order = 1; mi2.Order = 2;
        await _db.SaveChangesAsync();

        var dailyMenu = await _db.DailyMenus.FirstAsync();
        var lunchMeal = new Meal { MealType = MealType.Lunch, DailyMenuId = dailyMenu.Id };
        _db.Meals.Add(lunchMeal);
        await _db.SaveChangesAsync();
        var mi3 = new MealItem { MealId = lunchMeal.Id, ItemId = item3.Id, Quantity = 1, Order = 1 };
        _db.MealItems.Add(mi3);
        await _db.SaveChangesAsync();

        await _service.MoveAsync(mi1.Id, new MoveMealItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        var target = await _db.MealItems
            .Where(mi => mi.MealId == lunchMeal.Id)
            .OrderBy(mi => mi.Order)
            .ToListAsync();
        target.Should().HaveCount(2);
        target[0].Id.Should().Be(mi1.Id);
        target[0].Order.Should().Be(1);
        target[1].Id.Should().Be(mi3.Id);
        target[1].Order.Should().Be(2);

        var source = await _db.MealItems
            .Where(mi => mi.MealId == meal.Id)
            .OrderBy(mi => mi.Order)
            .ToListAsync();
        source.Should().HaveCount(1);
        source[0].Id.Should().Be(mi2.Id);
        source[0].Order.Should().Be(1);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesMealItem()
    {
        var meal = await SeedMealAsync();
        var item = await SeedItemAsync();
        var mi = await SeedMealItemAsync(meal, item);

        var result = await _service.DeleteAsync(mi.Id);

        result.Should().BeTrue();
        _db.MealItems.Should().BeEmpty();
    }
}
