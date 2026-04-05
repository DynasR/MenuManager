using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MealItemService
{
    private readonly HttpClient _http;

    public MealItemService(HttpClient http) => _http = http;

    public async Task<MealItemResponse?> CreateAsync(CreateMealItemRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mealitems", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealItemResponse>()
            : null;
    }

    public async Task<MealItemResponse?> MoveAsync(int id, MoveMealItemRequest request)
    {
        var response = await _http.PatchAsJsonAsync($"api/mealitems/{id}/move", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealItemResponse>()
            : null;
    }

    public async Task<bool> ReorderAsync(ReorderMealItemsRequest request)
    {
        var response = await _http.PatchAsJsonAsync("api/mealitems/reorder", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/mealitems/{id}");
        return response.IsSuccessStatusCode;
    }
}
