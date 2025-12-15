namespace Baba.Chatbot.Application.Conversation.Guardrails;

/// <summary>
/// Resultado de la moderación de contenido de mensajes de usuario
/// </summary>
public class ContentModerationResult
{
    public bool IsAppropriate { get; set; }
    public List<ModerationFlag> Flags { get; set; } = new();
    public string? SuggestedResponse { get; set; }
    public ModerationSeverity Severity { get; set; }
}

/// <summary>
/// Tipos de violaciones detectadas en la moderación de contenido
/// </summary>
public enum ModerationFlag
{
    None,
    OffTopic,
    HateSpeech,
    Violence,
    SexualContent,
    Harassment,
    PII,
    Spam,
    IllegalContent,
    ManipulationAttempt
}

/// <summary>
/// Nivel de severidad de una violación de moderación
/// </summary>
public enum ModerationSeverity
{
    None,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Resultado de la validación de guardrails aplicada a respuestas del LLM
/// </summary>
public class GuardrailsValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Violations { get; set; } = new();
    public string? CleanedContent { get; set; }
    public bool RequiresHumanEscalation { get; set; }
}
