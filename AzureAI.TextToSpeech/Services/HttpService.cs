using AzureAI.TextToSpeech.Interfaces;
using System.Text;
using System.Text.Json;

namespace AzureAI.TextToSpeech.Services
{
    public class HttpService : IHttpService
    {
        public static readonly HttpService Instance = new();
        private static readonly HttpClient _httpClient = new();
        private HttpService() { }

        public async Task<T?> SendAsync<T>(string url, HttpMethod method, Dictionary<string, string>? headers = null, object? body = null)
        {
            using var request = new HttpRequestMessage(method, url);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            request.Content = body != null ? new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") : null;

            using var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseBody ?? response.ReasonPhrase);
            }

            if (string.IsNullOrEmpty(responseBody))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(responseBody);
        }
    }
}
