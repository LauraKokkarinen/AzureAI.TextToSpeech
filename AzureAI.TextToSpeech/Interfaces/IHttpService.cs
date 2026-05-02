namespace AzureAI.TextToSpeech.Interfaces
{
    public interface IHttpService
    {
        Task<T?> SendAsync<T>(string url, HttpMethod method, Dictionary<string, string>? headers = null, object? body = null);
    }
}
