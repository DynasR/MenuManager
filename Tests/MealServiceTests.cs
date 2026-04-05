using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class MealServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MealService _service;

    public MealServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new MealService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private async Task<DailyMenu> SeedDailyMenuAsync()
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
        return dailyMenu;
    }

    private async Task<Meal> SeedMealAsync(DailyMenu dailyMenu, MealType mealType = MealType.Breakfast)
    {
        var meal = new Meal
        {
            MealType = mealType,
            DailyMenuId = dailyMenu.Id
        };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();
        return meal;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMeals()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        await SeedMealAsync(dailyMenu);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].DailyMenuId.Should().Be(dailyMenu.Id);
        result[0].MealType.Should().Be(MealType.Breakfast);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMeal()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        var meal = await SeedMealAsync(dailyMenu);

        var result = await _service.GetByIdAsync(meal.Id);

        result.Should().NotBeNull();
        result!.MealType.Should().Be(MealType.Breakfast);
        result.DailyMenuId.Should().Be(dailyMenu.Id);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsDailyMenuNotFound_WhenDailyMenuDoesNotExist()
    {
        var result = await _service.CreateAsync(new CreateMealRequest
        {
            MealType = MealType.Lunch,
            DailyMenuId = 999
        });

        result.Error.Should().Be(CreateMealError.DailyMenuNotFound);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsAlreadyExists_WhenDuplicateMealTypeOnSameDailyMenu()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        await SeedMealAsync(dailyMenu, MealType.Lunch);

        var result = await _service.CreateAsync(new CreateMealRequest
        {
            MealType = MealType.Lunch,
            DailyMenuId = dailyMenu.Id
        });

        result.Error.Should().Be(CreateMealError.AlreadyExists);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsWithCorrectData()
    {
        var dailyMenu = await SeedDailyMenuAsync();

        var result = await _service.CreateAsync(new CreateMealRequest
        {
            MealType = MealType.Dinner,
            DailyMenuId = dailyMenu.Id
        });

        result.Error.Should().BeNull();
        result.Response.Should().NotBeNull();
        result.Response!.MealType.Should().Be(MealType.Dinner);
        result.Response.DailyMenuId.Should().Be(dailyMenu.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateMealRequest
        {
            MealType = MealType.Lunch,
            DailyMenuId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenDailyMenuNotFound()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        var meal = await SeedMealAsync(dailyMenu);

        var result = await _service.UpdateAsync(meal.Id, new UpdateMealRequest
        {
            MealType = MealType.Lunch,
            DailyMenuId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMealTypeAndReturnsUpdatedMeal()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        var meal = await SeedMealAsync(dailyMenu);

        var result = await _service.UpdateAsync(meal.Id, new UpdateMealRequest
        {
            MealType = MealType.Dinner,
            DailyMenuId = dailyMenu.Id
        });

        result.Should().NotBeNull();
        result!.MealType.Should().Be(MealType.Dinner);
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesMeal()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        var meal = await SeedMealAsync(dailyMenu);

        var result = await _service.DeleteAsync(meal.Id);

        result.Should().BeTrue();
        _db.Meals.Should().BeEmpty();
    }

    // ── DeleteBatchAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBatchAsync_RemovesKnownIds_IgnoresUnknownIds()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        var meal1 = await SeedMealAsync(dailyMenu, MealType.Breakfast);
        var meal2 = await SeedMealAsync(dailyMenu, MealType.Lunch);

        await _service.DeleteBatchAsync([meal1.Id, 999]);

        _db.Meals.Should().HaveCount(1);
        _db.Meals.Single().Id.Should().Be(meal2.Id);
    }

    [Fact]
    public async Task DeleteBatchAsync_EmptyList_DoesNothing()
    {
        var dailyMenu = await SeedDailyMenuAsync();
        await SeedMealAsync(dailyMenu);

        await _service.DeleteBatchAsync([]);

        _db.Meals.Should().HaveCount(1);
    }

    // ── RandomFillAsync ──────────────────────────────────────────────────────

    private async Task<(Customer customer, Item item)> SeedCustomerAndAvailableItemAsync()
    {
        var customer = new Customer
        {
            Name = "Bob",
            PasswordHash = [],
            PasswordSalt = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);

        var category = new Category { Name = "Food" };
        _db.Categories.Add(category);

        await _db.SaveChangesAsync();

        var supplier = new Supplier
        {
            Name = "Shop",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);

        var item = new Item
        {
            Name = "Apple",
            Unit = MeasurementUnit.Piece,
            PackageSize = 1,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);

        await _db.SaveChangesAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = 1.50m,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return (customer, item);
    }

    [Fact]
    public async Task RandomFillAsync_CustomerNotFound_ReturnsEmpty()
    {
        var result = await _service.RandomFillAsync(new RandomFillRequest
        {
            CustomerId = 999,
            Year = 2026,
            Month = 3
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RandomFillAsync_NoAvailableItems_ReturnsEmpty()
    {
        var dailyMenu = await SeedDailyMenuAsync(); // seeds a customer

        var result = await _service.RandomFillAsync(new RandomFillRequest
        {
            CustomerId = dailyMenu.CustomerId,
            Year = 2026,
            Month = 3
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RandomFillAsync_CreatesOneDailyMenuPerDayOfMonth()
    {
        var (customer, _) = await SeedCustomerAndAvailableItemAsync();
        var daysInMonth = DateTime.DaysInMonth(2026, 3);

        var result = await _service.RandomFillAsync(new RandomFillRequest
        {
            CustomerId = customer.Id,
            Year = 2026,
            Month = 3
        });

        // Lunch (1-3) and Dinner (1-3) guarantee every day has at least one meal
        result.Should().HaveCount(daysInMonth);
        result.Should().AllSatisfy(dm => dm.CustomerId.Should().Be(customer.Id));
        result.Should().AllSatisfy(dm => dm.Meals.Should().NotBeEmpty());
    }

    [Fact]
    public async Task RandomFillAsync_SkipsDaysWithExistingMeals()
    {
        var (customer, _) = await SeedCustomerAndAvailableItemAsync();

        // Seed an existing daily menu with a meal for March 1
        var existing = new DailyMenu { Date = new DateOnly(2026, 3, 1), CustomerId = customer.Id };
        _db.DailyMenus.Add(existing);
        await _db.SaveChangesAsync();
        var existingMeal = new Meal { MealType = MealType.Breakfast, DailyMenuId = existing.Id };
        _db.Meals.Add(existingMeal);
        await _db.SaveChangesAsync();

        await _service.RandomFillAsync(new RandomFillRequest
        {
            CustomerId = customer.Id,
            Year = 2026,
            Month = 3
        });

        // Existing meal on March 1 must still be there (day was skipped)
        var stillExists = await _db.Meals.AnyAsync(m => m.Id == existingMeal.Id);
        stillExists.Should().BeTrue();

        // Only Breakfast was seeded on March 1 — no new meals added for that day
        var mealsOnMarch1 = await _db.Meals
            .Where(m => m.DailyMenuId == existing.Id)
            .ToListAsync();
        mealsOnMarch1.Should().HaveCount(1);
    }
}
