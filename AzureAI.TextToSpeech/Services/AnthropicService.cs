using AzureAI.TextToSpeech.Interfaces;
using AzureAI.TextToSpeech.Types;

namespace AzureAI.TextToSpeech.Services
{
    public class AnthropicService : IChatService
    {
        private readonly IHttpService _httpService;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly string _systemMessage;
        private readonly string _key;

        public AnthropicService(IHttpService httpService, string endpoint, string key, string model, string systemMessage)
        {
            _httpService = httpService;
            _endpoint = endpoint.TrimEnd('/');
            _model = model;
            _systemMessage = systemMessage;
            _key = key;
        }

        private Dictionary<string, string> GetHeaders()
        {
            return new Dictionary<string, string>
            {
                { "x-api-key", _key },
                { "anthropic-version", "2023-06-01" }
            };
        }

        public async Task<string> Chat(string userMessage)
        {
            var requestBody = new
            {
                model = _model,
                max_tokens = 8192,
                temperature = 0,
                system = _systemMessage,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var response = await _httpService.SendAsync<AnthropicResponse>($"{_endpoint}/v1/messages", HttpMethod.Post, GetHeaders(), requestBody);

            return response!.Content!.First().Text!;
        }
    }
}
