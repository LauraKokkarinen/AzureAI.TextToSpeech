using AzureAI.TextToSpeech.Types;
using AzureAI.TextToSpeech.Interfaces;

namespace AzureAI.TextToSpeech.Services
{
    public class SpeechService(IHttpService httpService, string region, string key) : ISpeechService
    {
        private readonly IHttpService _httpService = httpService;
        private readonly string _region = region;
        private readonly string _key = key;

        private Dictionary<string, string> GetHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Ocp-Apim-Subscription-Key", _key }
            };
        }

        private async Task<T?> Get<T>(string url)
        {
            return await _httpService.SendAsync<T>(url, HttpMethod.Get, GetHeaders());
        }

        private async Task<IEnumerable<T>> GetCollection<T>(string url, IEnumerable<T>? items = null)
        {
            items ??= [];

            var response = await Get<CollectionResponse<T>>(url);

            if (response != null)
            {
                if (response?.Value != null)
                    items = items.Concat(response.Value);

                if (response?.NextLink != null)
                    return await GetCollection<T>(response.NextLink, items);
            }

            return items;
        }

        private async Task<T?> Put<T>(string url, object body)
        {
            return await _httpService.SendAsync<T>(url, HttpMethod.Put, GetHeaders(), body);
        }

        private async Task Delete(string url)
        {
            await _httpService.SendAsync<object>(url, HttpMethod.Delete, GetHeaders());
        }

        public async Task<BatchSynthesis?> CreateBatchSynthesis(string content, string? description = null)
        {
            var uniqueId = Guid.NewGuid();
            var body = new
            {
                description,
                inputKind = "SSML",
                inputs = new[]
                {
                    new
                    {
                        content
                    }
                },
                properties = new
                {
                    outputFormat = "riff-48khz-16bit-mono-pcm",
                    wordBoundaryEnabled = false,
                    sentenceBoundaryEnabled = false,
                    concatenateResult = false,
                    decompressOutputFiles = false
                }
            };

            return await Put<BatchSynthesis>($"https://{_region}.api.cognitive.microsoft.com/texttospeech/batchsyntheses/{uniqueId}?api-version=2024-04-01", body);
        }

        public async Task<BatchSynthesis?> GetBatchSynthesis(string id)
        {
            return await Get<BatchSynthesis>($"https://{_region}.api.cognitive.microsoft.com/texttospeech/batchsyntheses/{id}?api-version=2024-04-01");
        }

        public async Task DeleteBatchSynthesis(string id)
        {
            await Delete($"https://{_region}.api.cognitive.microsoft.com/texttospeech/batchsyntheses/{id}?api-version=2024-04-01");
        }
    }
}
