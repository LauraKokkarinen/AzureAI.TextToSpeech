using AzureAI.TextToSpeech.Enums;
using AzureAI.TextToSpeech.Services;
using AzureAI.TextToSpeech.Types;
using AzureAI.TextToSpeech.Interfaces;
using System.IO.Compression;
using System.Text.Json;

namespace AzureAI.Speech
{
    internal static class BatchSynthesizer
    {
        internal static async Task<List<string>> Run(string speechKey, string speechRegion, string directoryPath, List<string> batches)
        {
            var speechService = new SpeechService(HttpService.Instance, speechRegion, speechKey);

            var synthesisIds = await CreateBatchSyntheses(speechService, batches);
            await WaitForSynthesesToComplete(speechService, synthesisIds);
            var zipFilePaths = await DownloadSynthesisResults(speechService, synthesisIds, directoryPath);
            var audioFilePaths = ExtractAudioFiles(zipFilePaths);

            return audioFilePaths;
        }

        private static async Task<IEnumerable<string>> CreateBatchSyntheses(ISpeechService speechService, List<string> batches)
        {
            var synthesisIds = new List<string>();
            foreach (var batch in batches)
            {
                var synthesis = await speechService.CreateBatchSynthesis(batch);
                if (synthesis?.Id != null)
                    synthesisIds.Add(synthesis.Id);
            }

            return synthesisIds;
        }

        private static async Task WaitForSynthesesToComplete(ISpeechService speechService, IEnumerable<string> synthesisIds)
        {
            var inProgress = true;
            while (inProgress)
            {
                await Task.Delay(5000);

                var batchSyntheses = new List<BatchSynthesis>();
                foreach (var synthesisId in synthesisIds)
                {
                    var synthesis = await speechService.GetBatchSynthesis(synthesisId);
                    if (synthesis != null)
                        batchSyntheses.Add(synthesis);
                }

                inProgress = batchSyntheses.Any(s => s.Status == SynthesisStatus.Running.ToString() || s.Status == SynthesisStatus.NotStarted.ToString());
            }
        }

        private static async Task<IEnumerable<string>> DownloadSynthesisResults(ISpeechService speechService, IEnumerable<string> synthesisIds, string? directoryPath)
        {
            var zipFilePaths = new List<string>();

            using var httpClient = new HttpClient();
            foreach (var synthesisId in synthesisIds)
            {
                var zipFilePath = $"{directoryPath}\\{synthesisId}.zip";
                var synthesis = await speechService.GetBatchSynthesis(synthesisId);

                using var stream = await httpClient.GetStreamAsync(synthesis?.Outputs?.Result);
                using var fileStream = new FileStream(zipFilePath, FileMode.OpenOrCreate);
                await stream.CopyToAsync(fileStream);

                zipFilePaths.Add(zipFilePath);
                await speechService.DeleteBatchSynthesis(synthesisId);
            }

            return zipFilePaths;
        }

        private static List<string> ExtractAudioFiles(IEnumerable<string> zipFilePaths)
        {
            var audioFilePaths = new List<string>();

            foreach (var zipFilePath in zipFilePaths)
            {
                var extractDirName = Path.GetFileNameWithoutExtension(zipFilePath).Replace(".zip", "");
                var extractDirPath = $"{Path.GetDirectoryName(zipFilePath)}\\{extractDirName}";

                ZipFile.ExtractToDirectory(zipFilePath, extractDirPath);
                var extractDirInfo = new DirectoryInfo(extractDirPath);

                var audioFiles = extractDirInfo.GetFiles("*.wav");
                foreach (var audioFile in audioFiles)
                {
                    audioFilePaths.Add(audioFile.FullName);
                }

                DisplayDebugErrors(extractDirInfo);

                File.Delete(zipFilePath);
            }

            return audioFilePaths;
        }

        private static void DisplayDebugErrors(DirectoryInfo extractDirInfo)
        {
            var debugFiles = extractDirInfo.GetFiles("*.debug.json");

            Console.ForegroundColor = ConsoleColor.Red;

            foreach (var debugFile in debugFiles)
            {
                try
                {
                    var json = File.ReadAllText(debugFile.FullName);
                    var debugData = JsonSerializer.Deserialize<SynthesisDebugFile>(json);
                    var errors = debugData?.DebugInfo?.Where(r => r.HasError).ToList();

                    if (errors == null || errors.Count == 0) continue;

                    foreach (var error in errors)
                    {                        
                        Console.WriteLine($"Speech synthesis error ({error.ErrorCode}): {error.ErrorDetails}");
                        if (!string.IsNullOrWhiteSpace(error.Ssml))
                        {
                            Console.WriteLine($"Problematic SSML:");
                            Console.WriteLine(error.Ssml);
                        }
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse error details from {debugFile.Name}: {ex.Message}");
                }
            }

            Console.ResetColor();
        }
    }
}
