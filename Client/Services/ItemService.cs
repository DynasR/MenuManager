using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class ItemService
{
    private readonly HttpClient _http;

    public ItemService(HttpClient http) => _http = http;

    public async Task<List<ItemResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<ItemResponse>>("api/items") ?? [];

    public async Task<ItemResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<ItemResponse>($"api/items/{id}");

    public async Task<ItemResponse?> CreateAsync(CreateItemRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/items", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ItemResponse>()
            : null;
    }

    public async Task<ItemResponse?> UpdateAsync(int id, UpdateItemRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/items/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ItemResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/items/{id}");
        return response.IsSuccessStatusCode;
    }
}
