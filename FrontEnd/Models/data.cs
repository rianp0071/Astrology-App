// Define the Gemini request and response models to deserialize the JSON
namespace AstrologyApp.Models
{
    public class GeminiResponse
    {
        public Candidate[]? Candidates { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }
    }

    public class RequestPayload
    {
        public Content[]? Contents { get; set; }
    }

    public class Content
    {
        public Part[]? Parts { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }

    public class ErrorResponse
    {
        public Dictionary<string, string[]>? Errors { get; set; }
    }

}
