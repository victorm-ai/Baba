using Baba.Chatbot.Application.Conversation.Orchestrator;
using Baba.Chatbot.Integrations.Llm;
using Microsoft.AspNetCore.Mvc;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;

namespace Baba.Chatbot.Api.Controllers;

/// <summary>
/// Controlador para gestionar webhooks entrantes de Twilio WhatsApp
/// Procesa mensajes de usuarios y genera respuestas mediante el orquestador de conversación
/// </summary>
[ApiController]
[Route("v1/webhook/twilio")]
[Produces("application/xml", "application/json")]
public class TwilioWebhookController : TwilioController
{
    private readonly ILogger<TwilioWebhookController> _logger;
    private readonly ConversationOrchestrator _orchestrator;
    private readonly PromptRepository _promptRepository;

    /// <summary>
    /// Inicializa una nueva instancia del controlador de webhook de Twilio
    /// </summary>
    public TwilioWebhookController(ILogger<TwilioWebhookController> logger, ConversationOrchestrator orchestrator, PromptRepository promptRepository)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _promptRepository = promptRepository;
    }

    /// <summary>
    /// Procesa mensajes SMS/WhatsApp entrantes desde Twilio
    /// Valida el contenido, genera una respuesta mediante IA y aplica guardrails de seguridad
    /// </summary>
    [HttpPost("incoming")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IncomingMessage([FromForm] SmsRequest request)
    {
        _logger.LogInformation("Received message from {From}: {Body}", request.From, request.Body);

        try
        {
            var systemPrompt = await _promptRepository.GetSystemPromptAsync();

            if (string.IsNullOrEmpty(systemPrompt))
            {
                _logger.LogWarning("System prompt is empty, using default");
                systemPrompt = "Eres Baba, un asistente virtual de Kavak que ayuda a clientes a encontrar vehículos.";
            }

            var userId = request.From ?? "unknown";
            var userMessage = request.Body ?? "";

            var conversationResult = await _orchestrator.ProcessMessageAsync(userId, userMessage, systemPrompt, conversationHistory: null);

            if (conversationResult.RequiresEscalation)
            {
                _logger.LogWarning("Conversation requires human escalation for user {UserId}. Flags: {Flags}",
                userId, string.Join(", ", conversationResult.ModerationFlags));
            }

            if (conversationResult.HasPiiViolations)
            {
                _logger.LogWarning("PII detected and masked in response for user {UserId}", userId);
            }

            _logger.LogInformation("Generated AI response for {From}: {Success}", request.From, conversationResult.Success);

            var response = new Twilio.TwiML.MessagingResponse();
            response.Message(conversationResult.Message);

            return TwiML(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming message");

            var errorResponse = new Twilio.TwiML.MessagingResponse();
            errorResponse.Message("Lo siento, hubo un error procesando tu mensaje. Por favor intenta de nuevo.");

            return TwiML(errorResponse);
        }
    }
}
