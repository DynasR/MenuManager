using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MealSlotService
{
    private readonly HttpClient _http;

    public MealSlotService(HttpClient http) => _http = http;

    public async Task<MealSlotResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<MealSlotResponse>($"api/mealslots/{id}");

    public async Task<MealSlotResponse?> CreateAsync(CreateMealSlotRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mealslots", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealSlotResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/mealslots/{id}");
        return response.IsSuccessStatusCode;
    }
}
