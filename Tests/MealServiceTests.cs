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
}
