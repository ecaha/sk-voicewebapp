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
    Task<byte[]> ConvertTextToSpeechAsync(string text, string language);
    void SetLanguage(string language);
    string GetCurrentLanguage();
    Dictionary<string, string> GetSupportedLanguages();
}

public class SpeechService : ISpeechService
{
    private readonly SpeechConfig _speechConfig;
    private readonly ILogger<SpeechService> _logger;
    private readonly Dictionary<string, LanguageConfig> _supportedLanguages;
    private string _currentLanguage = "en-US";

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
        
        // Initialize supported languages with their voice configurations
        _supportedLanguages = new Dictionary<string, LanguageConfig>
        {
            ["en-US"] = new("en-US", "English (US)", "en-US-AvaMultilingualNeural"),
            ["en-GB"] = new("en-GB", "English (UK)", "en-GB-SoniaNeural"),
            ["es-ES"] = new("es-ES", "Spanish (Spain)", "es-ES-ElviraNeural"),
            ["es-MX"] = new("es-MX", "Spanish (Mexico)", "es-MX-DaliaNeural"),
            ["fr-FR"] = new("fr-FR", "French (France)", "fr-FR-DeniseNeural"),
            ["de-DE"] = new("de-DE", "German (Germany)", "de-DE-KatjaNeural"),
            ["it-IT"] = new("it-IT", "Italian (Italy)", "it-IT-ElsaNeural"),
            ["pt-BR"] = new("pt-BR", "Portuguese (Brazil)", "pt-BR-FranciscaNeural"),
            ["zh-CN"] = new("zh-CN", "Chinese (Mandarin)", "zh-CN-XiaoxiaoNeural"),
            ["ja-JP"] = new("ja-JP", "Japanese", "ja-JP-NanamiNeural"),
            ["ko-KR"] = new("ko-KR", "Korean", "ko-KR-SunHiNeural"),
            ["ar-SA"] = new("ar-SA", "Arabic (Saudi Arabia)", "ar-SA-ZariyahNeural"),
            ["hi-IN"] = new("hi-IN", "Hindi (India)", "hi-IN-SwaraNeural"),
            ["ru-RU"] = new("ru-RU", "Russian", "ru-RU-SvetlanaNeural"),
            ["sk-SK"] = new("sk-SK", "Slovak", "sk-SK-ViktoriaNeural"),
            ["cs-CZ"] = new("cs-CZ", "Czech", "cs-CZ-VlastaNeural")
        };
        
        SetLanguage(_currentLanguage);
        
        _logger.LogInformation("Speech service initialized with region: {Region}", speechRegion);
    }

    public void SetLanguage(string language)
    {
        if (_supportedLanguages.ContainsKey(language))
        {
            _currentLanguage = language;
            var config = _supportedLanguages[language];
            _speechConfig.SpeechRecognitionLanguage = config.LanguageCode;
            _speechConfig.SpeechSynthesisVoiceName = config.VoiceName;
            _logger.LogInformation("Language changed to: {Language} ({DisplayName})", language, config.DisplayName);
        }
        else
        {
            _logger.LogWarning("Unsupported language: {Language}. Using default: {Default}", language, _currentLanguage);
        }
    }

    public string GetCurrentLanguage() => _currentLanguage;

    public Dictionary<string, string> GetSupportedLanguages()
    {
        return _supportedLanguages.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.DisplayName
        );
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
        return await ConvertTextToSpeechAsync(text, _currentLanguage);
    }

    public async Task<byte[]> ConvertTextToSpeechAsync(string text, string language)
    {
        try
        {
            _logger.LogInformation("Converting text to speech: {Text} in language: {Language}", text, language);

            // Create a temporary speech config for this specific language if different from current
            SpeechConfig speechConfig = _speechConfig;
            if (language != _currentLanguage && _supportedLanguages.ContainsKey(language))
            {
                speechConfig = SpeechConfig.FromSubscription(_speechConfig.SubscriptionKey, _speechConfig.Region);
                speechConfig.SpeechSynthesisVoiceName = _supportedLanguages[language].VoiceName;
            }

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig);
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

public record LanguageConfig(string LanguageCode, string DisplayName, string VoiceName);
