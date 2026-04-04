using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class DayPlanService
{
    private readonly HttpClient _http;

    public DayPlanService(HttpClient http) => _http = http;

    public async Task<List<DayPlanResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<DayPlanResponse>>("api/dayplans") ?? [];

    public async Task<DayPlanResponse?> CreateAsync(CreateDayPlanRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/dayplans", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DayPlanResponse>()
            : null;
    }

    public async Task<DayPlanResponse?> UpdateAsync(int id, UpdateDayPlanRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/dayplans/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DayPlanResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/dayplans/{id}");
        return response.IsSuccessStatusCode;
    }
}
