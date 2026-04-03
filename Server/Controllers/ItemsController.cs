using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _service;

    public ItemsController(IItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemResponse>>> GetAll([FromQuery] int? categoryId)
    {
        if (categoryId.HasValue)
            return Ok(await _service.GetByCategoryAsync(categoryId.Value));

        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ItemResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ItemResponse>> Create(CreateItemRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ItemResponse>> Update(int id, UpdateItemRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var exists = await _service.GetByIdAsync(id);
        if (exists is null) return NotFound();

        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : Conflict();
    }
}
