using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FrontEnd;
using System.Net.Http.Json;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load appsettings.json
using var client = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var settings = await client.GetFromJsonAsync<AppSettings>("appsettings.json");

// Register settings as a service
if (settings is not null)
{
    builder.Services.AddSingleton(settings);
}
else
{
    throw new InvalidOperationException("AppSettings could not be loaded.");
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddMemoryCache(); // Ensure IMemoryCache is registered

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://gemini-api-url/")
});

await builder.Build().RunAsync();

// Define AppSettings class
public class AppSettings
{
    public required string GeminiApiKey { get; set; }
    public required string ApiEndpoint { get; set; }
}
