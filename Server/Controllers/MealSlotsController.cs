using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MealSlotsController : ControllerBase
{
    private readonly IMealSlotService _service;

    public MealSlotsController(IMealSlotService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<MealSlotResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MealSlotResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MealSlotResponse>> Create(CreateMealSlotRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                CreateMealSlotError.AlreadyExists => Conflict(),
                _ => NotFound()
            };
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Response!.Id }, result.Response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MealSlotResponse>> Update(int id, UpdateMealSlotRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
