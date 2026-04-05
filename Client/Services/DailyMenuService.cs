using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class DailyMenuService
{
    private readonly HttpClient _http;

    public DailyMenuService(HttpClient http) => _http = http;

    public async Task<List<DailyMenuResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<DailyMenuResponse>>("api/dailymenus") ?? [];

    public async Task<List<DailyMenuResponse>> GetByCustomerAsync(int customerId) =>
        await _http.GetFromJsonAsync<List<DailyMenuResponse>>($"api/dailymenus?customerId={customerId}") ?? [];

    public async Task<DailyMenuResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<DailyMenuResponse>($"api/dailymenus/{id}");

    public async Task<DailyMenuResponse?> CreateAsync(CreateDailyMenuRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/dailymenus", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DailyMenuResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/dailymenus/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<MonthlySummaryResponse>> GetMonthlySummaryAsync(int customerId) =>
        await _http.GetFromJsonAsync<List<MonthlySummaryResponse>>($"api/dailymenus/{customerId}/monthly-summary") ?? [];
}
