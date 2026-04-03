using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _service;

    public SuppliersController(ISupplierService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierResponse>> Create(CreateSupplierRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SupplierResponse>> Update(int id, UpdateSupplierRequest request)
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
