using MenuManager.Server.Services;
using MenuManager.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MenuManager.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemSuppliersController : ControllerBase
{
    private readonly IItemSupplierService _service;

    public ItemSuppliersController(IItemSupplierService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ItemSupplierResponse>>> GetAll([FromQuery] int? itemId)
    {
        if (itemId.HasValue)
            return Ok(await _service.GetByItemAsync(itemId.Value));

        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{itemId:int}/{supplierId:int}")]
    public async Task<ActionResult<ItemSupplierResponse>> GetById(int itemId, int supplierId)
    {
        var result = await _service.GetByIdAsync(itemId, supplierId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("best-by-item")]
    public async Task<ActionResult<Dictionary<int, BestSupplierInfo>>> GetBestByItem()
    {
        return Ok(await _service.GetBestByItemAsync());
    }

    [HttpPost("by-items")]
    public async Task<ActionResult<List<ItemPricingResponse>>> GetByItems(ByItemsRequest request)
    {
        return Ok(await _service.GetByItemsAsync(request.ItemIds));
    }

    [HttpPost]
    public async Task<ActionResult<ItemSupplierResponse>> Create(CreateItemSupplierRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                CreateItemSupplierError.AlreadyExists => Conflict(),
                _ => NotFound()
            };
        }

        return CreatedAtAction(nameof(GetById),
            new { itemId = result.Response!.ItemId, supplierId = result.Response.SupplierId },
            result.Response);
    }

    [HttpPut("{itemId:int}/{supplierId:int}")]
    public async Task<ActionResult<ItemSupplierResponse>> Update(
        int itemId, int supplierId, UpdateItemSupplierRequest request)
    {
        var result = await _service.UpdateAsync(itemId, supplierId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{itemId:int}/{supplierId:int}")]
    public async Task<IActionResult> Delete(int itemId, int supplierId)
    {
        var deleted = await _service.DeleteAsync(itemId, supplierId);
        return deleted ? NoContent() : NotFound();
    }
}
