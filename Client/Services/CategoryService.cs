using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class CategoryService
{
    private readonly HttpClient _http;

    public CategoryService(HttpClient http) => _http = http;

    public async Task<List<CategoryResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<CategoryResponse>>("api/categories") ?? [];

    public async Task<CategoryResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<CategoryResponse>($"api/categories/{id}");

    public async Task<CategoryResponse?> CreateAsync(CreateCategoryRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/categories", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CategoryResponse>()
            : null;
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/categories/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CategoryResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/categories/{id}");
        return response.IsSuccessStatusCode;
    }
}
