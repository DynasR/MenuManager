using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class MenuPlanServiceTests : IDisposable
{
    // SQLite in-memory is used instead of the InMemory provider so that unique
    // index constraints (MealSlot.DayPlanId + MealType) are properly enforced.
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MenuPlanService _service;

    public MenuPlanServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new MenuPlanService(_db);
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

    private async Task<Item> SeedItemAsync(string name = "Rice")
    {
        var cat = new Category { Name = "Food" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = name,
            Unit = "kg",
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return item;
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

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllMenuPlans()
    {
        var customer = await SeedCustomerAsync();
        await SeedMenuPlanAsync(customer);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].CustomerName.Should().Be(customer.Name);
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenCustomerIdIsInvalid()
    {
        var result = await _service.CreateAsync(new MenuPlanRequest
        {
            Name = "Plan",
            Month = 1,
            Year = 2026,
            CustomerId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturns_WithEagerLoadedGraph()
    {
        var customer = await SeedCustomerAsync();
        var item = await SeedItemAsync();

        var result = await _service.CreateAsync(new MenuPlanRequest
        {
            Name = "March Plan",
            Month = 3,
            Year = 2026,
            CustomerId = customer.Id,
            DayPlans =
            [
                new DayPlanRequest
                {
                    Date = new DateOnly(2026, 3, 1),
                    MealSlots =
                    [
                        new MealSlotRequest
                        {
                            MealType = MealType.Breakfast,
                            MealSlotItems =
                            [
                                new MealSlotItemRequest { ItemId = item.Id, Quantity = 1.5m, Notes = "oatmeal" }
                            ]
                        }
                    ]
                }
            ]
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("March Plan");
        result.CustomerName.Should().Be(customer.Name);
        result.DayPlans.Should().HaveCount(1);
        result.DayPlans[0].MealSlots.Should().HaveCount(1);
        result.DayPlans[0].MealSlots[0].MealType.Should().Be(MealType.Breakfast);
        result.DayPlans[0].MealSlots[0].MealSlotItems.Should().HaveCount(1);
        result.DayPlans[0].MealSlots[0].MealSlotItems[0].ItemName.Should().Be(item.Name);
        result.DayPlans[0].MealSlots[0].MealSlotItems[0].Quantity.Should().Be(1.5m);
    }

    [Fact]
    public async Task CreateAsync_ThrowsDbUpdateException_WhenDuplicateMealSlotTypeOnSameDayPlan()
    {
        var customer = await SeedCustomerAsync();

        var act = async () => await _service.CreateAsync(new MenuPlanRequest
        {
            Name = "Plan",
            Month = 1,
            Year = 2026,
            CustomerId = customer.Id,
            DayPlans =
            [
                new DayPlanRequest
                {
                    Date = new DateOnly(2026, 1, 1),
                    MealSlots =
                    [
                        new MealSlotRequest { MealType = MealType.Lunch },
                        new MealSlotRequest { MealType = MealType.Lunch } // duplicate
                    ]
                }
            ]
        });

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new MenuPlanRequest
        {
            Name = "Plan",
            Month = 1,
            Year = 2026,
            CustomerId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsUpdatedPlan()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);

        var result = await _service.UpdateAsync(plan.Id, new MenuPlanRequest
        {
            Name = "Updated Plan",
            Month = 6,
            Year = 2026,
            CustomerId = customer.Id
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Plan");
        result.Month.Should().Be(6);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesPlan()
    {
        var customer = await SeedCustomerAsync();
        var plan = await SeedMenuPlanAsync(customer);

        var result = await _service.DeleteAsync(plan.Id);

        result.Should().BeTrue();
        _db.MenuPlans.Should().BeEmpty();
    }
}
