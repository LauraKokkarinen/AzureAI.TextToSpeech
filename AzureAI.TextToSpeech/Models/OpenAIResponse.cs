using System.Text.Json.Serialization;

namespace AzureAI.TextToSpeech.Types
{
    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
