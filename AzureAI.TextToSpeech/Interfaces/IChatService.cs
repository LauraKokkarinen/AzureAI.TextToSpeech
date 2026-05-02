namespace AzureAI.TextToSpeech.Interfaces
{
    public interface IChatService
    {
        Task<string> Chat(string userMessage);
    }
}
