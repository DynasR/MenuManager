using FluentAssertions;
using MenuManager.Server.Data;
using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using MenuManager.Shared.Entities;
using MenuManager.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace MenuManager.Tests;

public class RecipeServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RecipeService _service;
    private readonly RecipeIngredientService _ingredientService;

    public RecipeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _service = new RecipeService(_db);
        _ingredientService = new RecipeIngredientService(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Recipe CRUD ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsRecipe()
    {
        var request = new CreateRecipeRequest
        {
            Name = "Pasta Carbonara",
            Description = "Classic Italian",
            BaseServings = 4
        };

        var result = await _service.CreateAsync(request);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Pasta Carbonara");
        result.Description.Should().Be("Classic Italian");
        result.BaseServings.Should().Be(4);
        result.Ingredients.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecipes()
    {
        _db.Recipes.AddRange(
            new Recipe { Name = "Recipe A", BaseServings = 2 },
            new Recipe { Name = "Recipe B", BaseServings = 4 });
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
    public async Task GetByIdAsync_ReturnsRecipeWithIngredients()
    {
        var category = new Category { Name = "Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = "Spaghetti",
            Unit = MeasurementUnit.Gram,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var recipe = new Recipe { Name = "Pasta", BaseServings = 2 };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 200
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(recipe.Id);

        result.Should().NotBeNull();
        result!.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].ItemName.Should().Be("Spaghetti");
        result.Ingredients[0].Quantity.Should().Be(200);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAndReturnsRecipe()
    {
        var recipe = new Recipe { Name = "Old Name", BaseServings = 2 };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        var result = await _service.UpdateAsync(recipe.Id, new UpdateRecipeRequest
        {
            Name = "New Name",
            Description = "Updated desc",
            BaseServings = 6
        });

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Description.Should().Be("Updated desc");
        result.BaseServings.Should().Be(6);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_ForUnknownId()
    {
        var result = await _service.UpdateAsync(999, new UpdateRecipeRequest { Name = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemoves()
    {
        var recipe = new Recipe { Name = "ToDelete", BaseServings = 1 };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        var result = await _service.DeleteAsync(recipe.Id);

        result.Should().BeTrue();
        _db.Recipes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForUnknownId()
    {
        var result = await _service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    // ── RecipeIngredient ─────────────────────────────────────────────────

    private async Task<(Recipe recipe, Item item)> SeedRecipeAndItem()
    {
        var category = new Category { Name = "Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = "Flour",
            Unit = MeasurementUnit.Gram,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);

        var recipe = new Recipe { Name = "Bread", BaseServings = 1 };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        return (recipe, item);
    }

    [Fact]
    public async Task AddIngredientAsync_Success()
    {
        var (recipe, item) = await SeedRecipeAndItem();

        var result = await _ingredientService.AddIngredientAsync(new RecipeIngredientRequest
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 500
        });

        result.Should().NotBeNull();
        result!.RecipeId.Should().Be(recipe.Id);
        result.ItemId.Should().Be(item.Id);
        result.ItemName.Should().Be("Flour");
        result.Quantity.Should().Be(500);
    }

    [Fact]
    public async Task AddIngredientAsync_ReturnsNull_WhenRecipeNotFound()
    {
        var category = new Category { Name = "Cat" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        var item = new Item
        {
            Name = "Flour",
            Unit = MeasurementUnit.Gram,
            CategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync();

        var result = await _ingredientService.AddIngredientAsync(new RecipeIngredientRequest
        {
            RecipeId = 999,
            ItemId = item.Id,
            Quantity = 100
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddIngredientAsync_ReturnsNull_WhenItemNotFound()
    {
        var recipe = new Recipe { Name = "Bread", BaseServings = 1 };
        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync();

        var result = await _ingredientService.AddIngredientAsync(new RecipeIngredientRequest
        {
            RecipeId = recipe.Id,
            ItemId = 999,
            Quantity = 100
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddIngredientAsync_ReturnsNull_WhenAlreadyExists()
    {
        var (recipe, item) = await SeedRecipeAndItem();

        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 100
        });
        await _db.SaveChangesAsync();

        var result = await _ingredientService.AddIngredientAsync(new RecipeIngredientRequest
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 200
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveIngredientAsync_ReturnsTrue()
    {
        var (recipe, item) = await SeedRecipeAndItem();

        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 100
        });
        await _db.SaveChangesAsync();

        var result = await _ingredientService.RemoveIngredientAsync(recipe.Id, item.Id);

        result.Should().BeTrue();
        _db.RecipeIngredients.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveIngredientAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _ingredientService.RemoveIngredientAsync(999, 999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateQuantityAsync_UpdatesAndReturns()
    {
        var (recipe, item) = await SeedRecipeAndItem();

        _db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id,
            ItemId = item.Id,
            Quantity = 100
        });
        await _db.SaveChangesAsync();

        var result = await _ingredientService.UpdateQuantityAsync(recipe.Id, item.Id, 300);

        result.Should().NotBeNull();
        result!.Quantity.Should().Be(300);
    }

    [Fact]
    public async Task UpdateQuantityAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _ingredientService.UpdateQuantityAsync(999, 999, 100);

        result.Should().BeNull();
    }
}
