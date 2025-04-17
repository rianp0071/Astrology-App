using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AstrologyApp.Models;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    public GeminiService(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<string> AskGeminiAsync(string userInput)
    {
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
                            new Part { Text = $"{userInput} Limit response to 10 sentences max and don't tell me that you are limiting the response" }
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
                    var firstCandidate = result.Candidates[0];
                    return firstCandidate.Content?.Parts?[0]?.Text ?? "No text found.";
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

