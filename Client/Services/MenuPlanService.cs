using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class MenuPlanService
{
    private readonly HttpClient _http;

    public MenuPlanService(HttpClient http) => _http = http;

    public async Task<List<MenuPlanResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<MenuPlanResponse>>("api/menuplans") ?? [];

    public async Task<MenuPlanResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<MenuPlanResponse>($"api/menuplans/{id}");

    public async Task<MenuPlanResponse?> CreateAsync(MenuPlanRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/menuplans", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuPlanResponse>()
            : null;
    }

    public async Task<MenuPlanResponse?> UpdateAsync(int id, MenuPlanRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/menuplans/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MenuPlanResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/menuplans/{id}");
        return response.IsSuccessStatusCode;
    }
}
