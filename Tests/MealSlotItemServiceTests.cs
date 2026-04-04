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

    // ── MoveAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.MoveAsync(999, new MoveMealSlotItemRequest
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
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.MoveAsync(msi.Id, new MoveMealSlotItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Breakfast,
            NewOrder = 0
        });

        result.Should().NotBeNull();
        result!.MealSlotId.Should().Be(mealSlot.Id);
        var entity = await _db.MealSlotItems.FindAsync(msi.Id);
        entity!.Order.Should().Be(1);
    }

    [Fact]
    public async Task MoveAsync_MovesToDifferentSlot_ExistingSlot()
    {
        var mealSlot = await SeedMealSlotAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        // Create a Lunch slot on the same day
        var dayPlan = await _db.DayPlans.FirstAsync();
        var lunchSlot = new MealSlot { MealType = MealType.Lunch, DayPlanId = dayPlan.Id };
        _db.MealSlots.Add(lunchSlot);
        await _db.SaveChangesAsync();

        var result = await _service.MoveAsync(msi.Id, new MoveMealSlotItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        result.Should().NotBeNull();
        result!.MealSlotId.Should().Be(lunchSlot.Id);
    }

    [Fact]
    public async Task MoveAsync_CreatesSlotOnDemand_WhenTargetSlotMissing()
    {
        var mealSlot = await SeedMealSlotAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.MoveAsync(msi.Id, new MoveMealSlotItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Dinner, // no Dinner slot exists
            NewOrder = 0
        });

        result.Should().NotBeNull();
        var newSlot = await _db.MealSlots
            .FirstOrDefaultAsync(ms => ms.MealType == MealType.Dinner);
        newSlot.Should().NotBeNull();
        result!.MealSlotId.Should().Be(newSlot!.Id);
    }

    [Fact]
    public async Task MoveAsync_CreatesDayPlanAndSlotOnDemand_WhenTargetDateMissing()
    {
        var mealSlot = await SeedMealSlotAsync(); // Breakfast on 2026-01-15
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var targetDate = new DateOnly(2026, 1, 20); // no DayPlan for this date

        var result = await _service.MoveAsync(msi.Id, new MoveMealSlotItemRequest
        {
            TargetDate = targetDate,
            TargetMealType = MealType.Lunch,
            NewOrder = 1
        });

        result.Should().NotBeNull();
        var newDayPlan = await _db.DayPlans
            .FirstOrDefaultAsync(dp => dp.Date == targetDate);
        newDayPlan.Should().NotBeNull();
        var newSlot = await _db.MealSlots
            .FirstOrDefaultAsync(ms => ms.DayPlanId == newDayPlan!.Id && ms.MealType == MealType.Lunch);
        newSlot.Should().NotBeNull();
        result!.MealSlotId.Should().Be(newSlot!.Id);
    }

    // ── ReorderAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderAsync_ReordersItemsCorrectly()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var msi1 = await SeedMealSlotItemAsync(mealSlot, item1);
        var msi2 = await SeedMealSlotItemAsync(mealSlot, item2);
        var msi3 = await SeedMealSlotItemAsync(mealSlot, item3);

        var result = await _service.ReorderAsync(new ReorderMealSlotItemsRequest
        {
            MealSlotId = mealSlot.Id,
            OrderedItemIds = [msi3.Id, msi1.Id, msi2.Id]
        });

        result.Should().BeTrue();
        (await _db.MealSlotItems.FindAsync(msi3.Id))!.Order.Should().Be(1);
        (await _db.MealSlotItems.FindAsync(msi1.Id))!.Order.Should().Be(2);
        (await _db.MealSlotItems.FindAsync(msi2.Id))!.Order.Should().Be(3);
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenUnknownIdInList()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item = await SeedItemAsync();
        var msi = await SeedMealSlotItemAsync(mealSlot, item);

        var result = await _service.ReorderAsync(new ReorderMealSlotItemsRequest
        {
            MealSlotId = mealSlot.Id,
            OrderedItemIds = [msi.Id, 999]
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenSlotEmpty()
    {
        var mealSlot = await SeedMealSlotAsync();

        var result = await _service.ReorderAsync(new ReorderMealSlotItemsRequest
        {
            MealSlotId = mealSlot.Id,
            OrderedItemIds = []
        });

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_ReturnsFalse_WhenPartialList()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var msi1 = await SeedMealSlotItemAsync(mealSlot, item1);
        await SeedMealSlotItemAsync(mealSlot, item2);

        var result = await _service.ReorderAsync(new ReorderMealSlotItemsRequest
        {
            MealSlotId = mealSlot.Id,
            OrderedItemIds = [msi1.Id]
        });

        result.Should().BeFalse();
    }

    // ── CreateAsync Order ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AssignsSequentialOrder()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");

        var r1 = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id, ItemId = item1.Id, Quantity = 1
        });
        var r2 = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id, ItemId = item2.Id, Quantity = 1
        });
        var r3 = await _service.CreateAsync(new CreateMealSlotItemRequest
        {
            MealSlotId = mealSlot.Id, ItemId = item3.Id, Quantity = 1
        });

        r1!.Order.Should().Be(1);
        r2!.Order.Should().Be(2);
        r3!.Order.Should().Be(3);
    }

    // ── MoveAsync renumbering ────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_RenumbersSourceSlot_AfterMove()
    {
        var mealSlot = await SeedMealSlotAsync();
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var msi1 = await SeedMealSlotItemAsync(mealSlot, item1);
        var msi2 = await SeedMealSlotItemAsync(mealSlot, item2);
        var msi3 = await SeedMealSlotItemAsync(mealSlot, item3);
        msi1.Order = 1; msi2.Order = 2; msi3.Order = 3;
        await _db.SaveChangesAsync();

        // Move middle item to a new slot
        await _service.MoveAsync(msi2.Id, new MoveMealSlotItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        // Source slot should be renumbered 1, 2 without gap
        var source = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == mealSlot.Id)
            .OrderBy(msi => msi.Order)
            .ToListAsync();
        source.Should().HaveCount(2);
        source[0].Id.Should().Be(msi1.Id);
        source[0].Order.Should().Be(1);
        source[1].Id.Should().Be(msi3.Id);
        source[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task MoveAsync_InsertsAtCorrectPosition_InTargetSlot()
    {
        var mealSlot = await SeedMealSlotAsync(); // Breakfast
        var item1 = await SeedItemAsync("Rice");
        var item2 = await SeedItemAsync("Pasta");
        var item3 = await SeedItemAsync("Bread");
        var msi1 = await SeedMealSlotItemAsync(mealSlot, item1);
        var msi2 = await SeedMealSlotItemAsync(mealSlot, item2);
        msi1.Order = 1; msi2.Order = 2;
        await _db.SaveChangesAsync();

        // Create a second slot with one item
        var dayPlan = await _db.DayPlans.FirstAsync();
        var lunchSlot = new MealSlot { MealType = MealType.Lunch, DayPlanId = dayPlan.Id };
        _db.MealSlots.Add(lunchSlot);
        await _db.SaveChangesAsync();
        var msi3 = new MealSlotItem { MealSlotId = lunchSlot.Id, ItemId = item3.Id, Quantity = 1, Order = 1 };
        _db.MealSlotItems.Add(msi3);
        await _db.SaveChangesAsync();

        // Move msi1 to Lunch at position 0 (before existing item)
        await _service.MoveAsync(msi1.Id, new MoveMealSlotItemRequest
        {
            TargetDate = new DateOnly(2026, 1, 15),
            TargetMealType = MealType.Lunch,
            NewOrder = 0
        });

        // Target slot: msi1 at 1, msi3 at 2
        var target = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == lunchSlot.Id)
            .OrderBy(msi => msi.Order)
            .ToListAsync();
        target.Should().HaveCount(2);
        target[0].Id.Should().Be(msi1.Id);
        target[0].Order.Should().Be(1);
        target[1].Id.Should().Be(msi3.Id);
        target[1].Order.Should().Be(2);

        // Source slot: msi2 alone at 1
        var source = await _db.MealSlotItems
            .Where(msi => msi.MealSlotId == mealSlot.Id)
            .OrderBy(msi => msi.Order)
            .ToListAsync();
        source.Should().HaveCount(1);
        source[0].Id.Should().Be(msi2.Id);
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
