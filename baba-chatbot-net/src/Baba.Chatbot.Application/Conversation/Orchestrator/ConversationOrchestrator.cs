using Baba.Chatbot.Application.Abstractions;
using Baba.Chatbot.Application.Conversation.Guardrails;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Baba.Chatbot.Application.Conversation.Orchestrator;

public class ConversationOrchestrator
{
    private readonly ILlmClient _llmClient;
    private readonly IGuardrailsValidator _guardrailsValidator;
    private readonly ILogger<ConversationOrchestrator> _logger;
    
    private readonly ConcurrentDictionary<string, int> _userViolationCount = new();

    public ConversationOrchestrator(ILlmClient llmClient, IGuardrailsValidator guardrailsValidator, ILogger<ConversationOrchestrator> logger)
    {
        _llmClient = llmClient;
        _guardrailsValidator = guardrailsValidator;
        _logger = logger;
    }

    public async Task<ConversationResponse> ProcessMessageAsync(string userId, string userMessage, string systemPrompt, List<string>? conversationHistory = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing message for user {UserId}", userId);

        var userValidation = _guardrailsValidator.ValidateUserInput(userMessage);

        if (!userValidation.IsAppropriate)
        {
            _logger.LogWarning("User message failed validation for user {UserId}. Flags: {Flags}",
                userId, string.Join(", ", userValidation.Flags));


            var violationCount = _userViolationCount.AddOrUpdate(userId, 1, (key, count) => count + 1);

     
            if (userValidation.Severity == ModerationSeverity.High || violationCount >= 3)
            {
                _logger.LogWarning("User {UserId} has {ViolationCount} violations. Terminating conversation.", 
                    userId, violationCount);

                return new ConversationResponse
                {
                    Success = false,
                    Message = _guardrailsValidator.GetResponseForViolation(userValidation, violationCount),
                    RequiresEscalation = true,
                    ModerationFlags = userValidation.Flags
                };
            }

            return new ConversationResponse
            {
                Success = false,
                Message = userValidation.SuggestedResponse ?? 
                         "Lo siento, no puedo ayudarte con eso. ¿Hay algo relacionado con vehículos en lo que pueda asistirte?",
                RequiresEscalation = false,
                ModerationFlags = userValidation.Flags
            };
        }

        string llmResponse;
        try
        {
            llmResponse = await _llmClient.GenerateResponseAsync(systemPrompt, userMessage, conversationHistory, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response for user {UserId}", userId);
            
            return new ConversationResponse
            {
                Success = false,
                Message = "Lo siento, tuve un problema técnico. ¿Podrías intentar de nuevo?",
                RequiresEscalation = false
            };
        }

        var responseValidation = await _guardrailsValidator.ValidateResponseAsync(llmResponse);

        if (!responseValidation.IsValid)
        {
            _logger.LogError("LLM response failed guardrails validation for user {UserId}. Violations: {Violations}", userId, string.Join(", ", responseValidation.Violations));

            if (responseValidation.RequiresHumanEscalation)
            {
                return new ConversationResponse
                {
                    Success = false,
                    Message = "Déjame conectarte con un asesor especializado que podrá ayudarte mejor. Dame un momento...",
                    RequiresEscalation = true
                };
            }

            return new ConversationResponse
            {
                Success = false,
                Message = "Disculpa, déjame reformular eso. ¿Podrías ser más específico sobre lo que necesitas?",
                RequiresEscalation = false
            };
        }

        var finalResponse = responseValidation.CleanedContent ?? llmResponse;

        if (!_guardrailsValidator.ValidateResponseQuality(finalResponse))
        {
            _logger.LogWarning("Response failed quality validation for user {UserId}", userId);
            
            return new ConversationResponse
            {
                Success = false,
                Message = "¿Podrías darme más detalles sobre lo que buscas? Así puedo ayudarte mejor.",
                RequiresEscalation = false
            };
        }

        _logger.LogInformation("Successfully processed message for user {UserId}", userId);

        if (_userViolationCount.ContainsKey(userId))
        {
            _userViolationCount.TryRemove(userId, out _);
        }

        return new ConversationResponse
        {
            Success = true,
            Message = finalResponse,
            RequiresEscalation = false,
            HasPiiViolations = responseValidation.Violations.Any(v => v.Contains("detected"))
        };
    }


    public void ResetViolationCount(string userId)
    {
        _userViolationCount.TryRemove(userId, out _);
    }
}

public class ConversationResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public bool RequiresEscalation { get; set; }
    public List<ModerationFlag> ModerationFlags { get; set; } = new();
    public bool HasPiiViolations { get; set; }
}
