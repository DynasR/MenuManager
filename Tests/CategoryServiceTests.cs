using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new CategoryService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        _db.Categories.AddRange(
            new Category { Name = "Alpha" },
            new Category { Name = "Beta" });
        await _db.SaveChangesAsync();

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsCategory()
    {
        var request = new CreateCategoryRequest { Name = "NewCat", Description = "Desc" };

        var result = await _service.CreateAsync(request);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("NewCat");
        result.Description.Should().Be("Desc");
        _db.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateCategoryRequest { Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenCategoryHasSubCategories()
    {
        var parent = new Category { Name = "Parent" };
        _db.Categories.Add(parent);
        await _db.SaveChangesAsync();

        _db.Categories.Add(new Category { Name = "Child", ParentCategoryId = parent.Id });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(parent.Id);

        result.Should().BeFalse();
        _db.Categories.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenCategoryHasItems()
    {
        var category = new Category { Name = "Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        _db.Items.Add(new Item
        {
            Name = "Item",
            Unit = MeasurementUnit.Piece,
            PackageSize = 1,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(category.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves_WhenEmpty()
    {
        var category = new Category { Name = "Empty" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(category.Id);

        result.Should().BeTrue();
        _db.Categories.Should().BeEmpty();
    }
}
