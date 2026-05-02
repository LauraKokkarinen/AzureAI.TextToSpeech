using System.Text.Json.Serialization;

namespace AzureAI.TextToSpeech.Types
{
    public class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; set; }
    }

    public class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
