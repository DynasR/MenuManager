using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MealService
{
    private readonly HttpClient _http;

    public MealService(HttpClient http) => _http = http;

    public async Task<MealResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<MealResponse>($"api/meals/{id}");

    public async Task<MealResponse?> CreateAsync(CreateMealRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/meals", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MealResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/meals/{id}");
        return response.IsSuccessStatusCode;
    }
}
