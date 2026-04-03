using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MenuManager.Client;
using MenuManager.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var serverUrl = builder.Configuration["ServerUrl"] ?? "https://localhost:5075";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(serverUrl) });
builder.Services.AddScoped<CategoryService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
