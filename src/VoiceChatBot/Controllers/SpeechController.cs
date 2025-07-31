using Microsoft.AspNetCore.Mvc;
using VoiceChatBot.Services;

namespace VoiceChatBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpeechController : ControllerBase
{
    private readonly ISpeechService _speechService;
    private readonly IChatService _chatService;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(ISpeechService speechService, IChatService chatService, ILogger<SpeechController> logger)
    {
        _speechService = speechService;
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("voice-chat")]
    public async Task<IActionResult> VoiceChat()
    {
        try
        {
            _logger.LogInformation("Starting voice chat session");

            // Convert speech to text
            var speechText = await _speechService.ConvertSpeechToTextAsync();
            
            if (string.IsNullOrWhiteSpace(speechText))
            {
                return BadRequest(new { error = "No speech detected or speech recognition failed" });
            }

            _logger.LogInformation("Speech recognized: {Text}", speechText);

            // Get AI response
            var chatResponse = await _chatService.GetChatResponseAsync(speechText);

            // Convert response to speech
            var audioBytes = await _speechService.ConvertTextToSpeechAsync(chatResponse);

            return Ok(new VoiceChatResponse
            {
                RecognizedText = speechText,
                ChatResponse = chatResponse,
                AudioBase64 = Convert.ToBase64String(audioBytes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voice chat");
            return StatusCode(500, new { error = "An error occurred during voice chat processing" });
        }
    }

    [HttpPost("text-to-speech")]
    public async Task<IActionResult> TextToSpeech([FromBody] TextToSpeechRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "Text cannot be empty" });
            }

            var audioBytes = await _speechService.ConvertTextToSpeechAsync(request.Text);
            return File(audioBytes, "audio/wav", "speech.wav");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting text to speech");
            return StatusCode(500, new { error = "An error occurred during text-to-speech conversion" });
        }
    }

    [HttpPost("speech-to-text")]
    public async Task<IActionResult> SpeechToText()
    {
        try
        {
            var speechText = await _speechService.ConvertSpeechToTextAsync();
            
            if (string.IsNullOrWhiteSpace(speechText))
            {
                return BadRequest(new { error = "No speech detected or speech recognition failed" });
            }

            return Ok(new { text = speechText });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting speech to text");
            return StatusCode(500, new { error = "An error occurred during speech-to-text conversion" });
        }
    }
}

public class TextToSpeechRequest
{
    public string Text { get; set; } = string.Empty;
}

public class VoiceChatResponse
{
    public string RecognizedText { get; set; } = string.Empty;
    public string ChatResponse { get; set; } = string.Empty;
    public string AudioBase64 { get; set; } = string.Empty;
}
