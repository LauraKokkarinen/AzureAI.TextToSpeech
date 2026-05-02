using AzureAI.TextToSpeech.Types;

namespace AzureAI.TextToSpeech.Interfaces
{
    public interface ISpeechService
    {
        Task<BatchSynthesis?> CreateBatchSynthesis(string content, string? description = null);
        Task<BatchSynthesis?> GetBatchSynthesis(string id);
        Task DeleteBatchSynthesis(string id);
    }
}
