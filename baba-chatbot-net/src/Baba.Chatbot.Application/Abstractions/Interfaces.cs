using Baba.Chatbot.Domain.Entities;
using Baba.Chatbot.Application.Conversation.Guardrails;

namespace Baba.Chatbot.Application.Abstractions;

/// <summary>
/// Cliente para comunicación con modelos de lenguaje (LLM)
/// Gestiona generación de respuestas y llamadas a funciones
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Genera una respuesta de texto usando el LLM con contexto RAG y función calling
    /// </summary>
    public Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, List<string>? conversationHistory = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Genera una respuesta estructurada con un esquema específico
    /// </summary>
    public Task<T> GenerateStructuredResponseAsync<T>(string prompt, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Repositorio para búsqueda y consulta del catálogo de vehículos
/// </summary>
public interface ICatalogRepository
{
    /// <summary>
    /// Busca vehículos en el catálogo según criterios especificados
    /// </summary>
    public Task<List<Vehicle>> SearchVehiclesAsync(Domain.ValueObjects.VehicleQuery query,CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene un vehículo específico por su ID o stock_id
    /// </summary>
    public Task<Vehicle?> GetVehicleByIdAsync(string id,CancellationToken cancellationToken = default);
}

/// <summary>
/// Validador de guardrails para moderación de contenido y seguridad de respuestas
/// </summary>
public interface IGuardrailsValidator
{
    /// <summary>
    /// Valida y modera el contenido del mensaje de entrada del usuario
    /// </summary>
    public ContentModerationResult ValidateUserInput(string userMessage);
    
    /// <summary>
    /// Valida la respuesta generada por el LLM (PII, promesas no autorizadas, calidad)
    /// </summary>
    public Task<GuardrailsValidationResult> ValidateResponseAsync(string response);
    
    /// <summary>
    /// Valida la calidad básica de la respuesta (longitud, emojis excesivos)
    /// </summary>
    public bool ValidateResponseQuality(string response);
    
    /// <summary>
    /// Obtiene un mensaje de respuesta apropiado según el tipo de violación detectada
    /// </summary>
    public string  GetResponseForViolation(ContentModerationResult moderationResult, int violationCount = 1);
}

