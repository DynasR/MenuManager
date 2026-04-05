using MenuManager.Shared.DTOs;
using System.Net.Http.Json;

namespace MenuManager.Client.Services;

public class ItemSupplierCache
{
    private readonly HttpClient _http;
    private Dictionary<int, BestSupplierInfo>? _cache;

    public ItemSupplierCache(HttpClient http) => _http = http;

    public async Task EnsureLoadedAsync()
    {
        if (_cache is not null) return;
        _cache = await _http.GetFromJsonAsync<Dictionary<int, BestSupplierInfo>>("api/itemsuppliers/best-by-item") ?? [];
    }

    public BestSupplierInfo? GetBestSupplier(int itemId) =>
        _cache?.GetValueOrDefault(itemId);
}
