using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace VoiceChatBot.Services;

/// <summary>
/// Service for handling speech-to-text and text-to-speech operations
/// Uses Azure Speech Services from AI Foundry project
/// </summary>
public interface ISpeechService
{
    Task<string> ConvertSpeechToTextAsync(byte[] audioData);
    Task<string> ConvertSpeechToTextAsync(); // Overload for microphone input
    Task<byte[]> ConvertTextToSpeechAsync(string text);
}

public class SpeechService : ISpeechService
{
    private readonly SpeechConfig _speechConfig;
    private readonly ILogger<SpeechService> _logger;

    public SpeechService(IConfiguration configuration, ILogger<SpeechService> logger)
    {
        _logger = logger;
        
        // Get Speech Service configuration
        var speechKey = configuration["AzureSpeech:SubscriptionKey"];
        var speechRegion = configuration["AzureSpeech:Region"] ?? "eastus";
        
        if (string.IsNullOrEmpty(speechKey))
        {
            throw new InvalidOperationException("Speech service key not configured. Please set AzureSpeech:SubscriptionKey in configuration.");
        }

        _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        _speechConfig.SpeechRecognitionLanguage = "en-US";
        _speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";
        
        _logger.LogInformation("Speech service initialized with region: {Region}", speechRegion);
    }

    public async Task<string> ConvertSpeechToTextAsync(byte[] audioData)
    {
        try
        {
            _logger.LogInformation("Converting speech to text, audio size: {Size} bytes", audioData.Length);

            using var audioStream = AudioInputStream.CreatePushStream();
            using var audioConfig = AudioConfig.FromStreamInput(audioStream);
            using var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var tcs = new TaskCompletionSource<string>();
            
            speechRecognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    _logger.LogInformation("Speech recognized: {Text}", e.Result.Text);
                    tcs.TrySetResult(e.Result.Text);
                }
                else
                {
                    tcs.TrySetResult("I couldn't understand what you said. Please try again.");
                }
            };

            speechRecognizer.Canceled += (s, e) =>
            {
                _logger.LogError("Speech recognition canceled: {Reason}", e.ErrorDetails);
                tcs.TrySetException(new InvalidOperationException($"Speech recognition failed: {e.ErrorDetails}"));
            };

            // Start recognition and feed audio data
            await speechRecognizer.StartContinuousRecognitionAsync();
            audioStream.Write(audioData);
            audioStream.Close();

            // Wait for result with timeout
            var resultTask = tcs.Task;
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            
            var completedTask = await Task.WhenAny(resultTask, timeoutTask);
            await speechRecognizer.StopContinuousRecognitionAsync();

            if (completedTask == timeoutTask)
            {
                return "Speech recognition timed out. Please try again.";
            }

            return await resultTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting speech to text");
            return "Sorry, I had trouble understanding your speech. Please try again.";
        }
    }

    public async Task<string> ConvertSpeechToTextAsync()
    {
        try
        {
            _logger.LogInformation("Starting speech recognition from microphone");

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig);

            var tcs = new TaskCompletionSource<string>();
            
            speechRecognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    _logger.LogInformation("Speech recognized: {Text}", e.Result.Text);
                    tcs.TrySetResult(e.Result.Text);
                }
                else
                {
                    tcs.TrySetResult("I couldn't understand what you said. Please try again.");
                }
            };

            speechRecognizer.Canceled += (s, e) =>
            {
                _logger.LogError("Speech recognition canceled: {Reason}", e.ErrorDetails);
                tcs.TrySetException(new InvalidOperationException($"Speech recognition failed: {e.ErrorDetails}"));
            };

            await speechRecognizer.StartContinuousRecognitionAsync();

            // Wait for recognition result with timeout
            var timeoutTask = Task.Delay(10000); // 10 second timeout
            var resultTask = tcs.Task;
            var completedTask = await Task.WhenAny(resultTask, timeoutTask);

            await speechRecognizer.StopContinuousRecognitionAsync();

            if (completedTask == timeoutTask)
            {
                return "Speech recognition timed out. Please try again.";
            }

            return await resultTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting speech to text from microphone");
            return "Sorry, I had trouble understanding your speech. Please try again.";
        }
    }

    public async Task<byte[]> ConvertTextToSpeechAsync(string text)
    {
        try
        {
            _logger.LogInformation("Converting text to speech: {Text}", text);

            using var speechSynthesizer = new SpeechSynthesizer(_speechConfig);
            var result = await speechSynthesizer.SpeakTextAsync(text);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Text-to-speech completed, audio size: {Size} bytes", result.AudioData.Length);
                return result.AudioData;
            }
            else
            {
                _logger.LogError("Text-to-speech failed: {Reason}", result.Reason);
                throw new InvalidOperationException($"Text-to-speech failed: {result.Reason}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting text to speech");
            throw;
        }
    }
}
