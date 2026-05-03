using AzureAI.Speech;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using Microsoft.Extensions.Configuration;
using AzureAI.TextToSpeech.Helpers;
using AzureAI.TextToSpeech.Services;
using AzureAI.TextToSpeech.Interfaces;

class Program
{
    async static Task Main(string[] args)
    {
        var appSettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var configuration = new ConfigurationHelper(appSettings);

        string aiProvider = configuration.GetConfigurationValue<string>("AIProvider") ?? "Anthropic";
        string aiEndpoint = configuration.GetConfigurationValue<string>("AIEndpoint", true)!;
        string aiKey = configuration.GetConfigurationValue<string>("AIKey", true)!;
        string aiModel = configuration.GetConfigurationValue<string>("AIModel", true)!;
        string aiApiVersion = configuration.GetConfigurationValue<string>("AIApiVersion") ?? "2024-10-21";
        string systemMessageFilePath = configuration.GetConfigurationValue<string>("SystemMessageFilePath", true)!;

        string speechKey = configuration.GetConfigurationValue<string>("SpeechKey", true)!;
        string speechRegion = configuration.GetConfigurationValue<string>("SpeechRegion", true)!;

        string inputTextFilePath = configuration.GetConfigurationValue<string>("InputTextFilePath", true)!;
        string? outputAudioFileName = configuration.GetConfigurationValue<string?>("OutputAudioFileName");

        bool? saveSsml = configuration.GetConfigurationValue<bool?>("SaveSsml");
        bool? combineSsml = configuration.GetConfigurationValue<bool?>("CombineSsml");
        bool? useExistingSsml = configuration.GetConfigurationValue<bool?>("UseExistingSsml");
        bool? useBatchSynthesis = configuration.GetConfigurationValue<bool?>("UseBatchSynthesis");
        bool? pauseBetweenSteps = configuration.GetConfigurationValue<bool?>("PauseBetweenSteps");

        var directoryPath = Path.GetDirectoryName(inputTextFilePath) ?? throw new Exception("Text file path is invalid.");

        if (useExistingSsml == false || GetBatches(directoryPath).Count == 0)
        {
            await PrepareBatches(aiProvider, aiEndpoint, aiKey, aiModel, aiApiVersion, systemMessageFilePath, inputTextFilePath, saveSsml, combineSsml);
            if (combineSsml == true)
                ConcatSsmlFiles(directoryPath);

            if (pauseBetweenSteps == true)
            {
                Console.WriteLine("SSML files have now been generated and saved on disk. You can now make modifications to them if desired. Press any key to proceed with speech synthesis.");
                Console.Beep();
                Console.ReadKey();
            }
        }

        var batches = GetBatches(directoryPath);

        var audioFilePaths = useBatchSynthesis == true ?
            await BatchSynthesizer.Run(speechKey, speechRegion, directoryPath, batches) :
            await SpeechSynthesizer.Run(speechKey, speechRegion, directoryPath, batches);

        ConcatAudioFiles(audioFilePaths, directoryPath, $"{outputAudioFileName ?? "result"}.wav");
        Console.Beep();
    }

    private static async Task PrepareBatches(string aiProvider, string aiEndpoint, string aiKey, string aiModel, string aiApiVersion, string systemMessageFilePath, string inputTextFilePath, bool? saveSsml, bool? combineSsml)
    {
        var systemMessage = File.ReadAllText(systemMessageFilePath);
        var httpService = HttpService.Instance;

        IChatService chatService = aiProvider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase)
            ? new AnthropicService(httpService, aiEndpoint, aiKey, aiModel, systemMessage)
            : new OpenAIService(httpService, aiEndpoint, aiKey, aiModel, aiApiVersion, systemMessage);

        var textContent = File.ReadAllText(inputTextFilePath);

        var batches = new List<string>();
        var startIndex = 0;
        var batchLength = 5000; // Batch size for SSML generation
        var breakAt = "\r\n";

        while (startIndex < textContent.Length)
        {
            var endIndex = startIndex + batchLength < textContent.Length ? textContent.LastIndexOf(breakAt, startIndex + batchLength) + breakAt.Length : textContent.Length;
            batches.Add(textContent.Substring(startIndex, endIndex - startIndex));
            startIndex = endIndex;
        }

        await Parallel.ForEachAsync(batches, async (batch, _) =>
        {
            var response = await chatService.Chat(batch);
            response = response.Replace("```xml", "").Replace("```", "");

            if (saveSsml == true)
                File.WriteAllText($"{Path.GetDirectoryName(inputTextFilePath)}\\{batches.IndexOf(batch)}.ssml", response);
        });
    }

    private static List<string> GetBatches(string directoryPath)
    {
        var batches = new List<string>();

        var files = Directory.GetFiles(directoryPath, "*.ssml").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();

        foreach (var file in files)
        {
            batches.Add(File.ReadAllText(file));
        }

        return batches;
    }

    private static void ConcatSsmlFiles(string directoryPath)
    {
        var openingTag = "<speak xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" version=\"1.0\" xml:lang=\"en-GB\">";
        var closingTag = "</speak>";

        var concatResult = string.Empty;

        var files = Directory.GetFiles(directoryPath, "*.ssml").OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f))).ToArray();

        foreach (var file in files)
        {
            concatResult += File.ReadAllText(file).Replace(openingTag, string.Empty).Replace(closingTag, string.Empty);
            File.Delete(file);
        }

        concatResult = $"{openingTag}{concatResult}{closingTag}";

        File.WriteAllText($"{directoryPath}\\{files.Length}.ssml", concatResult);
    }

    private static void ConcatAudioFiles(IEnumerable<string> inputFilePaths, string directoryPath, string outputFileName)
    {
        var filePaths = inputFilePaths.ToList();

        if (filePaths.Count == 0)
        {
            Console.WriteLine("No audio files were produced.");
            return;
        }

        var audioFiles = new List<AudioFileReader>();

        foreach (var filePath in filePaths)
        {
            audioFiles.Add(new AudioFileReader(filePath));
        }

        var concatResult = new ConcatenatingSampleProvider(audioFiles);

        WaveFileWriter.CreateWaveFile16($"{directoryPath}\\{outputFileName}", concatResult);

        foreach (var audioFile in audioFiles)
        {
            audioFile.Close();
            audioFile.Dispose();

            var directory = Path.GetDirectoryName(audioFile.FileName);
            if (directory != null && directory != directoryPath)
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                    File.Delete(file);

                Directory.Delete(directory);
            }
            else
                File.Delete(audioFile.FileName);
        }
    }
}