using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class DailyMenuServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DailyMenuService _service;

    public DailyMenuServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new DailyMenuService(_db);
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

    private async Task<DailyMenu> SeedDailyMenuAsync(Customer customer, DateOnly? date = null)
    {
        var dailyMenu = new DailyMenu
        {
            Date = date ?? new DateOnly(2026, 1, 15),
            CustomerId = customer.Id
        };
        _db.DailyMenus.Add(dailyMenu);
        await _db.SaveChangesAsync();
        return dailyMenu;
    }

    private async Task<Item> SeedItemAsync(string name = "Rice", decimal contentQuantity = 1, decimal unitPrice = 2.00m)
    {
        var cat = new Category { Name = "Food" };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = name,
            PurchaseUnit = MeasurementUnit.Piece,
            ContentQuantity = contentQuantity,
            ContentUnit = MeasurementUnit.Piece,
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var supplier = new Supplier
        {
            Name = "S", CompanyName = "S",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        _db.ItemSuppliers.Add(new ItemSupplier
        {
            ItemId = item.Id,
            SupplierId = supplier.Id,
            UnitPrice = unitPrice,
            IsAvailable = true,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return item;
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllDailyMenus()
    {
        var customer = await SeedCustomerAsync();
        await SeedDailyMenuAsync(customer);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].CustomerId.Should().Be(customer.Id);
    }

    // ── GetByCustomerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetByCustomerAsync_ReturnsOnlyThatCustomersMenus()
    {
        var alice = await SeedCustomerAsync("Alice");
        var bob = await SeedCustomerAsync("Bob");
        await SeedDailyMenuAsync(alice);
        await SeedDailyMenuAsync(alice, new DateOnly(2026, 1, 16));
        await SeedDailyMenuAsync(bob);

        var result = await _service.GetByCustomerAsync(alice.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dm => dm.CustomerId.Should().Be(alice.Id));
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDailyMenu_WithMeals()
    {
        var customer = await SeedCustomerAsync();
        var dailyMenu = await SeedDailyMenuAsync(customer);

        var result = await _service.GetByIdAsync(dailyMenu.Id);

        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateOnly(2026, 1, 15));
        result.CustomerId.Should().Be(customer.Id);
        result.Meals.Should().BeEmpty();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenCustomerNotFound()
    {
        var result = await _service.CreateAsync(new CreateDailyMenuRequest
        {
            Date = new DateOnly(2026, 3, 1),
            CustomerId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsDailyMenu()
    {
        var customer = await SeedCustomerAsync();

        var result = await _service.CreateAsync(new CreateDailyMenuRequest
        {
            Date = new DateOnly(2026, 3, 1),
            CustomerId = customer.Id
        });

        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateOnly(2026, 3, 1));
        result.CustomerId.Should().Be(customer.Id);
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateDailyMenuRequest
        {
            Date = new DateOnly(2026, 3, 1),
            CustomerId = 1
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenCustomerNotFound()
    {
        var customer = await SeedCustomerAsync();
        var dailyMenu = await SeedDailyMenuAsync(customer);

        var result = await _service.UpdateAsync(dailyMenu.Id, new UpdateDailyMenuRequest
        {
            Date = new DateOnly(2026, 6, 1),
            CustomerId = 999
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndReturnsDailyMenu()
    {
        var customer = await SeedCustomerAsync();
        var dailyMenu = await SeedDailyMenuAsync(customer);

        var result = await _service.UpdateAsync(dailyMenu.Id, new UpdateDailyMenuRequest
        {
            Date = new DateOnly(2026, 6, 15),
            CustomerId = customer.Id
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
    public async Task DeleteAsync_ReturnsTrue_AndRemovesDailyMenu()
    {
        var customer = await SeedCustomerAsync();
        var dailyMenu = await SeedDailyMenuAsync(customer);

        var result = await _service.DeleteAsync(dailyMenu.Id);

        result.Should().BeTrue();
        _db.DailyMenus.Should().BeEmpty();
    }

    // ── GetMonthlySummaryAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetMonthlySummaryAsync_ReturnsEmpty_ForCustomerWithNoMenus()
    {
        var customer = await SeedCustomerAsync();

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ReturnsEmpty_ForUnknownCustomer()
    {
        var result = await _service.GetMonthlySummaryAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_GroupsByYearMonth()
    {
        var customer = await SeedCustomerAsync();
        // 2 days in Jan 2026, 1 day in Feb 2026
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 10));
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 20));
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 2, 5));

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().HaveCount(2);
        result[0].Year.Should().Be(2026); result[0].Month.Should().Be(1);
        result[1].Year.Should().Be(2026); result[1].Month.Should().Be(2);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_HasMeals_FalseWhenNoMealItems()
    {
        var customer = await SeedCustomerAsync();
        var dm = await SeedDailyMenuAsync(customer);
        // Add a Meal with no MealItems
        _db.Meals.Add(new Meal { MealType = MealType.Lunch, DailyMenuId = dm.Id });
        await _db.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().HaveCount(1);
        result[0].HasMeals.Should().BeFalse();
        result[0].MonthlyCost.Should().Be(0m);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ComputesMonthlyCostCorrectly()
    {
        // Item: ContentQuantity=6, UnitPrice=3.00 → 7 qty → ceil(7/6)=2 packages → cost=6.00
        var customer = await SeedCustomerAsync();
        var dm = await SeedDailyMenuAsync(customer);
        var item = await SeedItemAsync("IceCream", contentQuantity: 6, unitPrice: 3.00m);

        var meal = new Meal { MealType = MealType.Lunch, DailyMenuId = dm.Id };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();

        _db.MealItems.Add(new MealItem { MealId = meal.Id, ItemId = item.Id, Quantity = 7 });
        await _db.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().HaveCount(1);
        result[0].HasMeals.Should().BeTrue();
        result[0].MonthlyCost.Should().Be(6.00m); // ceil(7/6)=2 * 3.00
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_SumsQuantitiesAcrossDaysForSameItem()
    {
        // Same item on 2 different days in the same month:
        // Day1: qty=3, Day2: qty=4 → ceil(3/6)+ceil(4/6) = 1+1 = 2 packages → 2*3.00=6.00
        var customer = await SeedCustomerAsync();
        var item = await SeedItemAsync("Yogurt", contentQuantity: 6, unitPrice: 3.00m);

        var dm1 = await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 5));
        var meal1 = new Meal { MealType = MealType.Breakfast, DailyMenuId = dm1.Id };
        _db.Meals.Add(meal1);
        await _db.SaveChangesAsync();
        _db.MealItems.Add(new MealItem { MealId = meal1.Id, ItemId = item.Id, Quantity = 3 });

        var dm2 = await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 15));
        var meal2 = new Meal { MealType = MealType.Lunch, DailyMenuId = dm2.Id };
        _db.Meals.Add(meal2);
        await _db.SaveChangesAsync();
        _db.MealItems.Add(new MealItem { MealId = meal2.Id, ItemId = item.Id, Quantity = 4 });

        await _db.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().HaveCount(1);
        result[0].MonthlyCost.Should().Be(6.00m);
    }

    // ── DuplicateMonthAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateMonthAsync_ReturnsFalse_WhenCustomerNotFound()
    {
        var result = await _service.DuplicateMonthAsync(new DuplicateMonthRequest
        {
            CustomerId = 999,
            SourceYear = 2026, SourceMonth = 1,
            TargetYear = 2026, TargetMonth = 2
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DuplicateMonthAsync_CopiesMealItemsToTargetMonth()
    {
        var customer = await SeedCustomerAsync();
        var item = await SeedItemAsync();

        // Source: Jan 15 with Lunch containing 1 meal item
        var dm = await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 15));
        var meal = new Meal { MealType = MealType.Lunch, DailyMenuId = dm.Id };
        _db.Meals.Add(meal);
        await _db.SaveChangesAsync();
        _db.MealItems.Add(new MealItem { MealId = meal.Id, ItemId = item.Id, Quantity = 2, Order = 1 });
        await _db.SaveChangesAsync();

        var result = await _service.DuplicateMonthAsync(new DuplicateMonthRequest
        {
            CustomerId = customer.Id,
            SourceYear = 2026, SourceMonth = 1,
            TargetYear = 2026, TargetMonth = 2
        });

        result.Should().BeTrue();

        var targetMenus = await _db.DailyMenus
            .Include(d => d.Meals).ThenInclude(m => m.MealItems)
            .Where(d => d.CustomerId == customer.Id && d.Date.Year == 2026 && d.Date.Month == 2)
            .ToListAsync();

        targetMenus.Should().HaveCount(1);
        targetMenus[0].Date.Day.Should().Be(15);
        targetMenus[0].Meals.Should().HaveCount(1);
        var copiedMeal = targetMenus[0].Meals.First();
        copiedMeal.MealType.Should().Be(MealType.Lunch);
        copiedMeal.MealItems.Should().HaveCount(1);
        var copiedItem = copiedMeal.MealItems.First();
        copiedItem.ItemId.Should().Be(item.Id);
        copiedItem.Quantity.Should().Be(2);
        copiedItem.Order.Should().Be(1);
    }

    [Fact]
    public async Task DuplicateMonthAsync_ClearsTargetMonth_BeforeCopying()
    {
        var customer = await SeedCustomerAsync();

        // Source: Jan 10
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 10));

        // Target: Feb 5 with existing data
        var existingTarget = await SeedDailyMenuAsync(customer, new DateOnly(2026, 2, 5));
        var existingMeal = new Meal { MealType = MealType.Breakfast, DailyMenuId = existingTarget.Id };
        _db.Meals.Add(existingMeal);
        await _db.SaveChangesAsync();

        var result = await _service.DuplicateMonthAsync(new DuplicateMonthRequest
        {
            CustomerId = customer.Id,
            SourceYear = 2026, SourceMonth = 1,
            TargetYear = 2026, TargetMonth = 2
        });

        result.Should().BeTrue();

        // Feb 5 should be gone (only Feb 10 from source)
        var targetMenus = await _db.DailyMenus
            .Where(d => d.CustomerId == customer.Id && d.Date.Year == 2026 && d.Date.Month == 2)
            .ToListAsync();

        targetMenus.Should().HaveCount(1);
        targetMenus[0].Date.Day.Should().Be(10);
    }

    [Fact]
    public async Task DuplicateMonthAsync_SkipsDays_WhenTargetMonthIsShorter()
    {
        var customer = await SeedCustomerAsync();

        // Source: Jan 31 (day 31 doesn't exist in Feb)
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 31));
        // Also Jan 15 (exists in Feb)
        await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 15));

        var result = await _service.DuplicateMonthAsync(new DuplicateMonthRequest
        {
            CustomerId = customer.Id,
            SourceYear = 2026, SourceMonth = 1,
            TargetYear = 2026, TargetMonth = 2
        });

        result.Should().BeTrue();

        var targetMenus = await _db.DailyMenus
            .Where(d => d.CustomerId == customer.Id && d.Date.Year == 2026 && d.Date.Month == 2)
            .ToListAsync();

        // Only Feb 15 created; Feb 31 skipped
        targetMenus.Should().HaveCount(1);
        targetMenus[0].Date.Day.Should().Be(15);
    }

    [Fact]
    public async Task DuplicateMonthAsync_ReturnsTrue_WhenSourceIsEmpty()
    {
        var customer = await SeedCustomerAsync();

        // No source data — should succeed and produce an empty target
        var result = await _service.DuplicateMonthAsync(new DuplicateMonthRequest
        {
            CustomerId = customer.Id,
            SourceYear = 2026, SourceMonth = 1,
            TargetYear = 2026, TargetMonth = 2
        });

        result.Should().BeTrue();
        _db.DailyMenus.Where(d => d.CustomerId == customer.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_AppliesCeilPerLine_NotPerGroup()
    {
        // Same item on 2 days, qty=2 each, ContentQuantity=6, UnitPrice=3.00
        // Per-line:  ceil(2/6) + ceil(2/6) = 1 + 1 = 2 packages → 6.00
        // If grouped: ceil(4/6) = 1 package → 3.00  ← would be wrong
        var customer = await SeedCustomerAsync();
        var item = await SeedItemAsync("Yogurt", contentQuantity: 6, unitPrice: 3.00m);

        var dm1 = await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 5));
        var meal1 = new Meal { MealType = MealType.Breakfast, DailyMenuId = dm1.Id };
        _db.Meals.Add(meal1);
        await _db.SaveChangesAsync();
        _db.MealItems.Add(new MealItem { MealId = meal1.Id, ItemId = item.Id, Quantity = 2 });

        var dm2 = await SeedDailyMenuAsync(customer, new DateOnly(2026, 1, 15));
        var meal2 = new Meal { MealType = MealType.Lunch, DailyMenuId = dm2.Id };
        _db.Meals.Add(meal2);
        await _db.SaveChangesAsync();
        _db.MealItems.Add(new MealItem { MealId = meal2.Id, ItemId = item.Id, Quantity = 2 });

        await _db.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(customer.Id);

        result.Should().HaveCount(1);
        result[0].MonthlyCost.Should().Be(6.00m); // 2 * ceil(2/6) * 3.00 = 2 * 1 * 3.00
    }
}
