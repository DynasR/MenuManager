using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuPlansController : ControllerBase
{
    private readonly IMenuPlanService _service;

    public MenuPlansController(IMenuPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<MenuPlanResponse>>> GetAll([FromQuery] int? customerId)
    {
        if (customerId.HasValue)
            return Ok(await _service.GetByCustomerAsync(customerId.Value));

        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuPlanResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MenuPlanResponse>> Create(MenuPlanRequest request)
    {
        var result = await _service.CreateAsync(request);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuPlanResponse>> Update(int id, MenuPlanRequest request)
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
