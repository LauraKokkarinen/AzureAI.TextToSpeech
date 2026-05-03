# AzureAI.SpeechConcat

The purpose of this C# .NET 8 console application is to convert long plain-text files into speech using the Azure AI Speech service. The solution has been implemented specifically to create audiobook-like audio files based on fiction writing.

The application will first call the Azure AI service to apply SSML elements to the provided plain-text content according to the provided system message. An example system message can be seen at [systemmessage.md](https://github.com/LauraKokkarinen/AzureAI.TextToSpeech/blob/main/systemmessage.md).

The application of the SSML markup and the real-time speech synthesis happen in batches because the AI services have limits:

- There is a token limit of 8192 for each Azure AI service request
- The real-time speech synthesis has a 10-minute time limit

When the SSML content file(s) have been created, the application will offer you a chance to review and fix them if needed before proceeding to the speech synthesis phase if `PauseBetweenSteps` is set to `true`. When set to `false`, the application will continue automatically to speech synthesis without pausing.

You can select which speech synthesis method you wish to use via the `appsettings.json` file described later.

- When using the real-time `SpeechSynthesizer`, the content batches are iterated consecutively, and the synthesized speech is simultaneously played through the computer's speakers. The synthesized audio is written into a separate `.wav` file for each of the completed batches. When all of the batches have been converted into speech, the partial audio files will be merged into a single file, and the partial files will be deleted.
- When using the batch synthesis API, the content batches will be processed in parallel, unless `CombineSsml` is set to true in configuration, in which case the entire SSML is processed as a single job, resulting in a single audio file. The application will wait for all the created synthesis jobs to complete before downloading the synthesis results and cleaning up the jobs. The results are downloaded as `.zip` files, and their contents are then extracted. Finally, the extracted `.wav` files are merged into a single audio file, and both the downloaded zip files and the extracted folders containing the partial audio files are deleted.

## Running the application

1. Create an Azure AI Founry resource on your Azure subscription.
1. Deploy the model you wish to use, e.g. claude-sonnet-4-6.
1. Create a custom content filter for the Azure Open AI model with high thresholds (optional, depends on content).
1. Clone this repository onto your computer and open the solution on Visual Studio.
1. Create an `appsettings.json` file in the project root. The file is not automatically present because it has been added to `.gitignore` to prevent the contents from being checked into version control.
1. Add the following keys to the `appsettings.json` file and populate them with your values.
   ```json
   {
     "AIProvider": "Anthropic", // "OpenAI" or "Anthropic"
     "AIEndpoint": "https://your-resource-name.services.ai.azure.com/anthropic", // Get this from Azure AI Foundry.
     "AIKey": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVQWXYZ", // Get this from Azure AI Foundry.
     "AIModel": "claude-sonnet-4-6",
     "AIApiVersion": "", // Only used for OpenAI provider. Leave empty to use the default value (2024-10-21).
     "SystemMessageFilePath": "C:\\directory\\systemmessage.txt",
     "SpeechRegion": "swedencentral", // Get this from the Speech service Overview blade.
     "SpeechKey": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVQWXYZ", // Get this from Azure AI Foundry.
     "InputTextFilePath": "C:\\directory\\input.txt", // File containing the plain-text you wish to synthesize.
     "OutputAudioFileName": "Output file name without extension", // Created in the same directory with the text file.
     "SaveSsml": true, // Save the SSML files for debugging purposes.
     "CombineSsml":  true, // Combine all SSML files into a single SSML file before sending it to the speech service (the batch synthesis API).
     "UseExistingSsml": true, // Instead of generating new SSML files, use the existing ones if such exist. If none exists, generates the files as usual.
     "UseBatchSynthesis": false, // true = use the batch synthesis API (async), false = use SpeechSynthesizer (real-time)
     "PauseBetweenSteps": true // true = pause after SSML generation to allow reviewing and editing before synthesis, false = continue automatically
   }
   ```
1. Run the console application.
