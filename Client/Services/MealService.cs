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

    public async Task<bool> DeleteBatchAsync(List<int> ids)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "api/meals/batch")
        {
            Content = JsonContent.Create(new DeleteMealsBatchRequest { Ids = ids })
        };
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<DailyMenuResponse>?> RandomFillAsync(int customerId, int year, int month)
    {
        var response = await _http.PostAsJsonAsync("api/meals/random-fill",
            new RandomFillRequest { CustomerId = customerId, Year = year, Month = month });
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<DailyMenuResponse>>()
            : null;
    }
}
