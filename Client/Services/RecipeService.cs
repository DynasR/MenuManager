using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class RecipeService
{
    private readonly HttpClient _http;

    public RecipeService(HttpClient http) => _http = http;

    public async Task<List<RecipeResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<RecipeResponse>>("api/recipes") ?? [];

    public async Task<RecipeResponse?> CreateAsync(CreateRecipeRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/recipes", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecipeResponse>()
            : null;
    }

    public async Task<RecipeResponse?> UpdateAsync(int id, UpdateRecipeRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/recipes/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecipeResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/recipes/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<RecipeIngredientResponse?> AddIngredientAsync(int recipeId, RecipeIngredientRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/recipes/{recipeId}/ingredients", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecipeIngredientResponse>()
            : null;
    }

    public async Task<RecipeIngredientResponse?> UpdateIngredientAsync(int recipeId, int itemId, RecipeIngredientRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/recipes/{recipeId}/ingredients/{itemId}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecipeIngredientResponse>()
            : null;
    }

    public async Task<bool> DeleteIngredientAsync(int recipeId, int itemId)
    {
        var response = await _http.DeleteAsync($"api/recipes/{recipeId}/ingredients/{itemId}");
        return response.IsSuccessStatusCode;
    }
}
