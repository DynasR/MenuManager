using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class CustomerService
{
    private readonly HttpClient _http;

    public CustomerService(HttpClient http) => _http = http;

    public async Task<List<CustomerResponse>> GetAllAsync() =>
        await _http.GetFromJsonAsync<List<CustomerResponse>>("api/customers") ?? [];

    public async Task<CustomerResponse?> GetByIdAsync(int id) =>
        await _http.GetFromJsonAsync<CustomerResponse>($"api/customers/{id}");

    public async Task<CustomerResponse?> CreateAsync(CreateCustomerRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/customers", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CustomerResponse>()
            : null;
    }

    public async Task<CustomerResponse?> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/customers/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CustomerResponse>()
            : null;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/customers/{id}");
        return response.IsSuccessStatusCode;
    }
}
