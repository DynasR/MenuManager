using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IRecipeIngredientService _ingredientService;

    public RecipesController(IRecipeService recipeService, IRecipeIngredientService ingredientService)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecipeResponse>>> GetAll()
        => Ok(await _recipeService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecipeResponse>> GetById(int id)
    {
        var result = await _recipeService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<RecipeResponse>> Create(CreateRecipeRequest request)
    {
        var result = await _recipeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RecipeResponse>> Update(int id, UpdateRecipeRequest request)
    {
        var result = await _recipeService.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _recipeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── Ingredients ──────────────────────────────────────────────────────

    [HttpPost("{recipeId:int}/ingredients")]
    public async Task<ActionResult<RecipeIngredientResponse>> AddIngredient(
        int recipeId, RecipeIngredientRequest request)
    {
        request.RecipeId = recipeId;
        var result = await _ingredientService.AddIngredientAsync(request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{recipeId:int}/ingredients/{itemId:int}")]
    public async Task<ActionResult<RecipeIngredientResponse>> UpdateIngredient(
        int recipeId, int itemId, RecipeIngredientRequest request)
    {
        request.RecipeId = recipeId;
        request.ItemId = itemId;
        var result = await _ingredientService.UpdateAsync(recipeId, itemId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{recipeId:int}/ingredients/{itemId:int}")]
    public async Task<IActionResult> RemoveIngredient(int recipeId, int itemId)
    {
        var removed = await _ingredientService.RemoveIngredientAsync(recipeId, itemId);
        return removed ? NoContent() : NotFound();
    }
}
