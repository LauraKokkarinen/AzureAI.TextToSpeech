using System.Text.Json.Serialization;

namespace AzureAI.TextToSpeech.Types
{
    public class SynthesisDebugFile
    {
        [JsonPropertyName("debugInfo")]
        public List<SynthesisDebugResult>? DebugInfo { get; set; }
    }

    public class SynthesisDebugResult
    {
        [JsonPropertyName("resultID")]
        public string? ResultId { get; set; }
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
        [JsonPropertyName("ssml")]
        public string? Ssml { get; set; }
        [JsonPropertyName("billableCharacters")]
        public string? BillableCharacters { get; set; }
        [JsonPropertyName("retryCount")]
        public string? RetryCount { get; set; }
        [JsonPropertyName("firstByteLatency")]
        public string? FirstByteLatency { get; set; }
        [JsonPropertyName("finishTime")]
        public string? FinishTime { get; set; }
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }
        [JsonPropertyName("errorDetails")]
        public string? ErrorDetails { get; set; }

        public bool HasError => !string.IsNullOrEmpty(ErrorCode);
    }
}
