using AzureAI.TextToSpeech.Interfaces;
using AzureAI.TextToSpeech.Types;

namespace AzureAI.TextToSpeech.Services
{
    public class OpenAIService : IChatService
    {
        private readonly IHttpService _httpService;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly string _apiVersion;
        private readonly string _systemMessage;
        private readonly string _key;

        public OpenAIService(IHttpService httpService, string endpoint, string key, string model, string apiVersion, string systemMessage)
        {
            _httpService = httpService;
            _endpoint = endpoint.TrimEnd('/');
            _model = model;
            _apiVersion = apiVersion;
            _systemMessage = systemMessage;
            _key = key;
        }

        private Dictionary<string, string> GetHeaders()
        {
            return new Dictionary<string, string>
            {
                { "api-key", _key }
            };
        }

        public async Task<string> Chat(string userMessage)
        {
            var requestBody = new
            {
                model = _model,
                max_completion_tokens = 8192,
                temperature = 0,
                messages = new object[]
                {
                    new { role = "system", content = _systemMessage },
                    new { role = "user", content = userMessage }
                }
            };

            var url = $"{_endpoint}/openai/deployments/{_model}/chat/completions?api-version={_apiVersion}";
            var response = await _httpService.SendAsync<OpenAIResponse>(url, HttpMethod.Post, GetHeaders(), requestBody);

            return response!.Choices!.First().Message!.Content!;
        }
    }
}
