using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class ItemSupplierService
{
    private readonly HttpClient _http;

    public ItemSupplierService(HttpClient http) => _http = http;

    public async Task<List<ItemSupplierResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<ItemSupplierResponse>>("api/itemsuppliers") ?? [];

    public async Task<ItemSupplierResponse?> GetByIdsAsync(int itemId, int supplierId) =>
        await _http.GetFromJsonAsync<ItemSupplierResponse>($"api/itemsuppliers/{itemId}/{supplierId}");

    public async Task<ItemSupplierResponse?> CreateAsync(CreateItemSupplierRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/itemsuppliers", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ItemSupplierResponse>()
            : null;
    }

    public async Task<ItemSupplierResponse?> UpdateAsync(int itemId, int supplierId, UpdateItemSupplierRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/itemsuppliers/{itemId}/{supplierId}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ItemSupplierResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int itemId, int supplierId)
    {
        var response = await _http.DeleteAsync($"api/itemsuppliers/{itemId}/{supplierId}");
        return response.IsSuccessStatusCode;
    }
}
