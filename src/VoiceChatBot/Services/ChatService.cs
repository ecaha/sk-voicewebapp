using Azure.AI.Inference;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Reflection;

namespace VoiceChatBot.Services;

/// <summary>
/// Service for handling AI chat interactions using AI Foundry project
/// </summary>
public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage);
    void ClearChatHistory();
    IEnumerable<ChatMessage> GetChatHistory();
}

public record ChatMessage(string Role, string Content, DateTime Timestamp);

public class ChatService : IChatService
{
    private readonly ChatCompletionsClient _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly List<ChatMessage> _chatHistory;
    private readonly string _modelDeploymentName;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
    {
        _logger = logger;
        _chatHistory = new List<ChatMessage>();

        // Get AI Foundry project configuration
        var endpoint = configuration["AzureAI:Endpoint"];
        var apiKey = configuration["AzureAI:ApiKey"];
        _modelDeploymentName = configuration["AzureAI:ModelDeploymentName"] ?? "gpt-4o";

        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("AI Foundry endpoint not configured. Please set AzureAI:Endpoint in configuration.");
        }

        // Initialize chat client for AI Foundry
        // For AI Foundry projects, we need to use the API key authentication
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Create the client with the project endpoint and API key
            _chatClient = new ChatCompletionsClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
        }
        else
        {
            throw new InvalidOperationException("AI Foundry API key not configured. Please set AzureAI:ApiKey in configuration.");
        }

        _logger.LogInformation("Chat service initialized with endpoint: {Endpoint}, model: {Model}", endpoint, _modelDeploymentName);
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        try
        {
            _logger.LogInformation("Processing chat message: {Message}", userMessage);
            _logger.LogInformation("Using model deployment: {Model}", _modelDeploymentName);

            // Add user message to history
            _chatHistory.Add(new ChatMessage("User", userMessage, DateTime.UtcNow));

            // Prepare chat messages for the API
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage("You are a helpful AI assistant. Keep responses concise and conversational since they will be spoken aloud."),
            };

            // Add recent chat history (last 10 messages to keep context manageable)
            var recentHistory = _chatHistory.TakeLast(10);
            foreach (var msg in recentHistory)
            {
                if (msg.Role == "User")
                    messages.Add(new ChatRequestUserMessage(msg.Content));
                else if (msg.Role == "Assistant")
                    messages.Add(new ChatRequestAssistantMessage(msg.Content));
            }

            // Create chat completion request
            var chatRequest = new ChatCompletionsOptions
            {
                Model = _modelDeploymentName, // Use configured deployment name
                MaxTokens = 150,  // Keep responses concise for speech
                Temperature = 0.7f
            };

            foreach (var message in messages)
            {
                chatRequest.Messages.Add(message);
            }

            _logger.LogInformation("Sending request to AI Foundry with {MessageCount} messages", messages.Count);

            // Get response from AI Foundry
            var response = await _chatClient.CompleteAsync(chatRequest);
            var chatCompletions = response.Value;
            
            _logger.LogInformation("Response received from AI Foundry");
            
            // Extract the actual content from the response
            string assistantMessage = "I'm sorry, I couldn't generate a response.";
            
            try
            {
                // The response should have choices with messages
                if (chatCompletions != null)
                {
                    // Log the response structure for debugging
                    _logger.LogInformation("Response type: {Type}", chatCompletions.GetType().Name);
                    
                    // Try to access the response content using reflection first to understand the structure
                    var responseType = chatCompletions.GetType();
                    var properties = responseType.GetProperties();
                    
                    foreach (var prop in properties)
                    {
                        _logger.LogInformation("Available property: {PropertyName} of type {PropertyType}", prop.Name, prop.PropertyType.Name);
                    }
                    
                    // Try common property names for the content
                    var contentProperty = responseType.GetProperty("Content");
                    if (contentProperty != null)
                    {
                        var content = contentProperty.GetValue(chatCompletions)?.ToString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            assistantMessage = content;
                        }
                    }
                    else
                    {
                        // Try to get choices property
                        var choicesProperty = responseType.GetProperty("Choices");
                        if (choicesProperty != null)
                        {
                            var choices = choicesProperty.GetValue(chatCompletions);
                            _logger.LogInformation("Choices found: {Choices}", choices);
                        }
                    }
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogWarning(innerEx, "Error parsing response content");
            }

            // Add assistant response to history
            _chatHistory.Add(new ChatMessage("Assistant", assistantMessage, DateTime.UtcNow));

            _logger.LogInformation("Chat response generated successfully");
            return assistantMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chat response");
            var errorMessage = "I'm sorry, I'm having trouble right now. Could you please try again?";
            _chatHistory.Add(new ChatMessage("Assistant", errorMessage, DateTime.UtcNow));
            return errorMessage;
        }
    }

    public void ClearChatHistory()
    {
        _chatHistory.Clear();
        _logger.LogInformation("Chat history cleared");
    }

    public IEnumerable<ChatMessage> GetChatHistory()
    {
        return _chatHistory.AsReadOnly();
    }
}
