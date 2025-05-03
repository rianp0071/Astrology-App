using System.Net.Http.Json;
using AstrologyApp.Models;
using Microsoft.Extensions.Caching.Memory;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private readonly IMemoryCache _cache;

    public GeminiService(HttpClient httpClient, AppSettings settings, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _settings = settings;
        _cache = cache;
    }

    public async Task<string> AskGeminiAsync(string userInput)
    {
        // Check if the response is in cache
        if (_cache.TryGetValue(userInput, out string? cachedResponse) && cachedResponse != null)
        {
            return cachedResponse;
        }

        try
        {
            var requestPayload = new RequestPayload
            {
                Contents = new[]
                {
                    new Content
                    {
                        Parts = new[]
                        {
                            new Part { Text = $"{userInput} Limit response to 6 sentences max and don't tell me that you are limiting the response" }
                        }
                    }
                }
            };

            var apiUrl = $"{_settings.ApiEndpoint}?key={_settings.GeminiApiKey}";
            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestPayload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                if (result?.Candidates != null && result.Candidates.Length > 0)
                {
                    var output = result.Candidates[0].Content?.Parts?[0]?.Text ?? "No text found.";

                    // Cache the result for 24 hours
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    };
                    _cache.Set(userInput, output, cacheEntryOptions);

                    return output;
                }
                return "No candidates found.";
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                return $"Failed to fetch response from Gemini AI. Error: {errorDetails}";
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
