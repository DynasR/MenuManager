using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MealSlotService
{
    private readonly HttpClient _http;

    public MealSlotService(HttpClient http) => _http = http;

    public async Task<List<MealSlotResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<MealSlotResponse>>("api/mealslots") ?? [];

    public async Task<CreateMealSlotResult> CreateAsync(CreateMealSlotRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/mealslots", request);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<MealSlotResponse>();
            return new CreateMealSlotResult(dto, null);
        }

        var error = response.StatusCode == System.Net.HttpStatusCode.Conflict
            ? CreateMealSlotError.AlreadyExists
            : CreateMealSlotError.DayPlanNotFound;

        return new CreateMealSlotResult(null, error);
    }

    public async Task<MealSlotResponse?> UpdateAsync(int id, UpdateMealSlotRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/mealslots/{id}", request);
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
