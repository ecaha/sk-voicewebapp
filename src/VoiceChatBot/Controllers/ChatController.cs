using Microsoft.AspNetCore.Mvc;
using VoiceChatBot.Services;

namespace VoiceChatBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var response = await _chatService.GetChatResponseAsync(request.Message);
            return Ok(new ChatResponse { Message = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "An error occurred while processing your message" });
        }
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        try
        {
            var history = _chatService.GetChatHistory();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(500, new { error = "An error occurred while retrieving chat history" });
        }
    }

    [HttpPost("clear")]
    public IActionResult ClearHistory()
    {
        try
        {
            _chatService.ClearChatHistory();
            return Ok(new { message = "Chat history cleared" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing chat history");
            return StatusCode(500, new { error = "An error occurred while clearing chat history" });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
}
