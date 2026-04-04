using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class DayPlanServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DayPlanService _service;

    public DayPlanServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new DayPlanService(_db);
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

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllDayPlans()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        await SeedDayPlanAsync(plan);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].MenuPlanId.Should().Be(plan.Id);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDayPlan_WithMealSlots()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        var dayPlan = await SeedDayPlanAsync(plan);

        var result = await _service.GetByIdAsync(dayPlan.Id);

        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateOnly(2026, 1, 15));
        result.MenuPlanId.Should().Be(plan.Id);
        result.MealSlots.Should().BeEmpty();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenMenuPlanNotFound()
    {
        var result = await _service.CreateAsync(new CreateDayPlanRequest
        {
            Date = new DateOnly(2026, 3, 1),
            MenuPlanId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsDayPlan()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);

        var result = await _service.CreateAsync(new CreateDayPlanRequest
        {
            Date = new DateOnly(2026, 3, 1),
            MenuPlanId = plan.Id
        });

        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateOnly(2026, 3, 1));
        result.MenuPlanId.Should().Be(plan.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateDayPlanRequest
        {
            Date = new DateOnly(2026, 3, 1),
            MenuPlanId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenMenuPlanNotFound()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        var dayPlan = await SeedDayPlanAsync(plan);

        var result = await _service.UpdateAsync(dayPlan.Id, new UpdateDayPlanRequest
        {
            Date = new DateOnly(2026, 6, 1),
            MenuPlanId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsUpdatedDayPlan()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        var dayPlan = await SeedDayPlanAsync(plan);

        var result = await _service.UpdateAsync(dayPlan.Id, new UpdateDayPlanRequest
        {
            Date = new DateOnly(2026, 6, 15),
            MenuPlanId = plan.Id
        });

        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateOnly(2026, 6, 15));
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesDayPlan()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);
        var dayPlan = await SeedDayPlanAsync(plan);

        var result = await _service.DeleteAsync(dayPlan.Id);

        result.Should().BeTrue();
        _db.DayPlans.Should().BeEmpty();
    }
}
