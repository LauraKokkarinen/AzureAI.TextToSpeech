using AzureAI.TextToSpeech.Interfaces;
using System.Text;
using System.Text.Json;

namespace AzureAI.TextToSpeech.Services
{
    public class AnthropicService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly string _systemMessage;

        public AnthropicService(string endpoint, string key, string model, string systemMessage)
        {
            _endpoint = endpoint.TrimEnd('/');
            _model = model;
            _systemMessage = systemMessage;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", key);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        public async Task<string> Chat(string userMessage)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = 4096,
                temperature = 0,
                system = _systemMessage,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_endpoint}/v1/messages", content);

                var responseBody = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 429)
                {
                    await Task.Delay(5000);
                    return await Chat(userMessage);
                }

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"{(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}");
                var json = JsonSerializer.Deserialize<JsonElement>(responseBody);

                return json.GetProperty("content")[0].GetProperty("text").GetString()!;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                await Task.Delay(5000);
                return await Chat(userMessage);
            }
        }
    }
}
