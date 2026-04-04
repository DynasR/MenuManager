using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class MealSlotServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MealSlotService _service;

    public MealSlotServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new MealSlotService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── Seed helpers ─────────────────────────────────────────────────────────

    private async Task<Customer> SeedCustomerAsync(string name = "Alice")
    {
        var customer = new Customer
        {
            Name = name,
            PasswordHash = [],
            PasswordSalt = [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    private async Task<MenuPlan> SeedMenuPlanAsync(Customer customer)
    {
        var plan = new MenuPlan
        {
            Name = "January Plan",
            Month = 1,
            Year = 2026,
            CustomerId = customer.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.MenuPlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    private async Task<DayPlan> SeedDayPlanAsync(MenuPlan menuPlan)
    {
        var dayPlan = new DayPlan
        {
            Date = new DateOnly(2026, 1, 15),
            MenuPlanId = menuPlan.Id
        };
        _db.DayPlans.Add(dayPlan);
        await _db.SaveChangesAsync();
        return dayPlan;
    }

    private async Task<DayPlan> SeedFullChainAsync()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        return await SeedDayPlanAsync(plan);
    }

    private async Task<MealSlot> SeedMealSlotAsync(DayPlan dayPlan, MealType mealType = MealType.Breakfast)
    {
        var mealSlot = new MealSlot
        {
            MealType = mealType,
            DayPlanId = dayPlan.Id
        };
        _db.MealSlots.Add(mealSlot);
        await _db.SaveChangesAsync();
        return mealSlot;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMealSlots()
    {
        var dayPlan = await SeedFullChainAsync();
        await SeedMealSlotAsync(dayPlan);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].DayPlanId.Should().Be(dayPlan.Id);
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
    public async Task GetByIdAsync_ReturnsMealSlot()
    {
        var dayPlan = await SeedFullChainAsync();
        var mealSlot = await SeedMealSlotAsync(dayPlan);

        var result = await _service.GetByIdAsync(mealSlot.Id);

        result.Should().NotBeNull();
        result!.MealType.Should().Be(MealType.Breakfast);
        result.DayPlanId.Should().Be(dayPlan.Id);
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsDayPlanNotFound_WhenDayPlanDoesNotExist()
    {
        var result = await _service.CreateAsync(new CreateMealSlotRequest
        {
            MealType = MealType.Lunch,
            DayPlanId = 999
        });

        result.Error.Should().Be(CreateMealSlotError.DayPlanNotFound);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ReturnsAlreadyExists_WhenDuplicateMealTypeOnSameDayPlan()
    {
        var dayPlan = await SeedFullChainAsync();
        await SeedMealSlotAsync(dayPlan, MealType.Lunch);

        var result = await _service.CreateAsync(new CreateMealSlotRequest
        {
            MealType = MealType.Lunch,
            DayPlanId = dayPlan.Id
        });

        result.Error.Should().Be(CreateMealSlotError.AlreadyExists);
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsWithCorrectData()
    {
        var dayPlan = await SeedFullChainAsync();

        var result = await _service.CreateAsync(new CreateMealSlotRequest
        {
            MealType = MealType.Dinner,
            DayPlanId = dayPlan.Id
        });

        result.Error.Should().BeNull();
        result.Response.Should().NotBeNull();
        result.Response!.MealType.Should().Be(MealType.Dinner);
        result.Response.DayPlanId.Should().Be(dayPlan.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateMealSlotRequest
        {
            MealType = MealType.Lunch,
            DayPlanId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenDayPlanNotFound()
    {
        var dayPlan = await SeedFullChainAsync();
        var mealSlot = await SeedMealSlotAsync(dayPlan);

        var result = await _service.UpdateAsync(mealSlot.Id, new UpdateMealSlotRequest
        {
            MealType = MealType.Lunch,
            DayPlanId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsUpdatedMealSlot()
    {
        var dayPlan = await SeedFullChainAsync();
        var mealSlot = await SeedMealSlotAsync(dayPlan);

        var result = await _service.UpdateAsync(mealSlot.Id, new UpdateMealSlotRequest
        {
            MealType = MealType.Dinner,
            DayPlanId = dayPlan.Id
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
    public async Task DeleteAsync_ReturnsTrue_AndRemovesMealSlot()
    {
        var dayPlan = await SeedFullChainAsync();
        var mealSlot = await SeedMealSlotAsync(dayPlan);

        var result = await _service.DeleteAsync(mealSlot.Id);

        result.Should().BeTrue();
        _db.MealSlots.Should().BeEmpty();
    }
}
