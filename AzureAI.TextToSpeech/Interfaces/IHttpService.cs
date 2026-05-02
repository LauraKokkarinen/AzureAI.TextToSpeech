using AzureAI.TextToSpeech.Enums;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AzureAI.TextToSpeech.Interfaces
{
    public interface IHttpService
    {
        Task<JsonElement?> GetResponseAsync(string url, Method method, HttpRequestHeaders? headers = null, string? body = null, string? contentType = null);
    }
}
