using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MealSlotItemService
{
    private readonly HttpClient _http;

    public MealSlotItemService(HttpClient http) => _http = http;

    public async Task<List<MealSlotItemResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<MealSlotItemResponse>>("api/mealslotitems") ?? [];

    public async Task<MealSlotItemResponse?> CreateAsync(CreateMealSlotItemRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mealslotitems", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealSlotItemResponse>()
            : null;
    }

    public async Task<MealSlotItemResponse?> UpdateAsync(int id, UpdateMealSlotItemRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mealslotitems/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealSlotItemResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/mealslotitems/{id}");
        return response.IsSuccessStatusCode;
    }
}
