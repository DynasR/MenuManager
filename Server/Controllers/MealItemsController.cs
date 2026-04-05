using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MealItemsController : ControllerBase
{
    private readonly IMealItemService _service;

    public MealItemsController(IMealItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<MealItemResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MealItemResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MealItemResponse>> Create(CreateMealItemRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MealItemResponse>> Update(int id, UpdateMealItemRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:int}/move")]
    public async Task<ActionResult<MealItemResponse>> Move(int id, MoveMealItemRequest request)
    {
        var result = await _service.MoveAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("reorder")]
    public async Task<IActionResult> Reorder(ReorderMealItemsRequest request)
    {
        var result = await _service.ReorderAsync(request);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
