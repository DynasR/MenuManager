using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class SupplierService
{
    private readonly HttpClient _http;

    public SupplierService(HttpClient http) => _http = http;

    public async Task<List<SupplierResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<SupplierResponse>>("api/suppliers") ?? [];

    public async Task<SupplierResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<SupplierResponse>($"api/suppliers/{id}");

    public async Task<SupplierResponse?> CreateAsync(CreateSupplierRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/suppliers", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SupplierResponse>()
            : null;
    }

    public async Task<SupplierResponse?> UpdateAsync(int id, UpdateSupplierRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/suppliers/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SupplierResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/suppliers/{id}");
        return response.IsSuccessStatusCode;
    }
}
