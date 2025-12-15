using Baba.Chatbot.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Baba.Chatbot.Application.Conversation.Guardrails;

/// <summary>
/// Implementa validación de guardrails para moderación de contenido, detección de PII,
/// validación de calidad y prevención de promesas no autorizadas
/// </summary>
public class GuardrailsValidator : IGuardrailsValidator
{
    private readonly ILogger<GuardrailsValidator> _logger;
    private static readonly Regex _creditCardPattern = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _phonePattern = new(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex _emailPattern = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex _inePattern = new(@"\b[A-Z]{4}\d{6}[HM][A-Z]{5}\d{2}\b", RegexOptions.Compiled);
    private static readonly Regex _curpPattern = new(@"\b[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d\b", RegexOptions.Compiled);

    private static readonly HashSet<string> _allowedKavakPhones = new(StringComparer.OrdinalIgnoreCase)
    {
        "5512345678",
        "8001234567"
    };

    private static readonly HashSet<string> _allowedKavakEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "contacto@kavak.com",
        "soporte@kavak.com",
        "ventas@kavak.com"
    };

    // Content Moderation Keywords
    private static readonly HashSet<string> _hateSpeechKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "naz", "supremac", "discrimin", "racista", "xenofob",
    };

    private static readonly HashSet<string> _violenceKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "matar", "asesinar", "golpear", "tortura", "violencia", "agredir",
        "atacar", "herir", "lastimar", "daño físico", "sangre",
        "arma", "cuchillo", "pistola", "disparar"
    };

    private static readonly HashSet<string> _sexualContentKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "sexo", "sexual", "erótico", "pornografía", "desnudo", "íntimo",
        "seducir", "excitar", "placer sexual", "orgasmo",
    };

    private static readonly HashSet<string> _businessKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto", "carro", "coche", "vehículo", "camioneta", "suv", "sedán",
        "comprar", "vender", "precio", "financiamiento", "crédito", "pago",
        "kavak", "garantía", "certificación", "prueba de manejo", "entrega",
        "modelo", "marca", "año", "kilómetros", "transmisión", "motor",
        "seguro", "documentos", "factura", "contrato", "cita", "agendar"
    };

    private static readonly HashSet<string> _offTopicPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "receta", "cocinar", "comida", "restaurante", "película", "serie",
        "música", "cantante", "actor", "deporte", "fútbol", "política",
        "elección", "presidente", "partido político", "religión", "dios",
        "iglesia", "rezar", "tarea", "traducir", "definición de",
        "clima", "temperatura", "tiempo libre", "hobby"
    };

    /// <summary>
    /// Inicializa una nueva instancia del validador de guardrails
    /// </summary>
    public GuardrailsValidator(ILogger<GuardrailsValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Valida el mensaje de entrada del usuario aplicando moderación de contenido
    /// Detecta discurso de odio, violencia, contenido sexual, acoso y temas fuera de contexto
    /// </summary>
    public ContentModerationResult ValidateUserInput(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new ContentModerationResult
            {
                IsAppropriate = true,
                Severity = ModerationSeverity.None
            };
        }

        _logger.LogDebug("Validating user input message");

        var moderationResult = ModerateContent(userMessage);

        if (!moderationResult.IsAppropriate)
        {
            _logger.LogWarning("User message failed content moderation. Flags: {Flags}, Severity: {Severity}",
                string.Join(", ", moderationResult.Flags),
                moderationResult.Severity);
        }

        return moderationResult;
    }

    /// <summary>
    /// Valida la respuesta del LLM verificando longitud, PII, promesas no autorizadas,
    /// información inventada y contenido inapropiado
    /// Enmascara automáticamente datos sensibles detectados
    /// </summary>
    public Task<GuardrailsValidationResult> ValidateResponseAsync(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return Task.FromResult(new GuardrailsValidationResult
            {
                IsValid = false,
                Violations = new List<string> { "Empty response" }
            });
        }

        var result = new GuardrailsValidationResult
        {
            IsValid = true,
            CleanedContent = response
        };

        _logger.LogDebug("Validating LLM response");

        if (response.Length > 1000) 
        {
            result.Violations.Add("Response too long (>200 words)");
            _logger.LogWarning("Response exceeds maximum length");
        }

        if (response.Length < 10)
        {
            result.Violations.Add("Response too short");
            _logger.LogWarning("Response is too short");
        }

        var piiDetected = false;

        if (_creditCardPattern.IsMatch(response))
        {
            result.Violations.Add("Credit card number detected");
            result.CleanedContent = _creditCardPattern.Replace(result.CleanedContent, "[TARJETA OCULTA]");
            piiDetected = true;
            _logger.LogWarning("Credit card number detected and masked");
        }

        if (_inePattern.IsMatch(response) || _curpPattern.IsMatch(response))
        {
            result.Violations.Add("INE/CURP detected");
            result.CleanedContent = _inePattern.Replace(result.CleanedContent, "[ID OCULTO]");
            result.CleanedContent = _curpPattern.Replace(result.CleanedContent, "[CURP OCULTO]");
            piiDetected = true;
            _logger.LogWarning("INE/CURP detected and masked");
        }

        var phoneMatches = _phonePattern.Matches(response);
        foreach (Match match in phoneMatches)
        {
            var phone = match.Value.Replace("-", "").Replace(".", "");
            if (!_allowedKavakPhones.Contains(phone))
            {
                result.Violations.Add("Unauthorized phone number detected");
                result.CleanedContent = result.CleanedContent.Replace(match.Value, "[TELÉFONO OCULTO]");
                piiDetected = true;
                _logger.LogWarning("Unauthorized phone number detected and masked");
            }
        }

        var emailMatches = _emailPattern.Matches(response);
        foreach (Match match in emailMatches)
        {
            if (!_allowedKavakEmails.Contains(match.Value))
            {
                result.Violations.Add("Unauthorized email detected");
                result.CleanedContent = result.CleanedContent.Replace(match.Value, "[EMAIL OCULTO]");
                piiDetected = true;
                _logger.LogWarning("Unauthorized email detected and masked");
            }
        }

        var unauthorizedPromises = DetectUnauthorizedPromises(response);
        if (unauthorizedPromises.Any())
        {
            result.Violations.AddRange(unauthorizedPromises);
            result.RequiresHumanEscalation = true;
            _logger.LogWarning("Unauthorized promises detected: {Promises}", string.Join(", ", unauthorizedPromises));
        }

        if (ContainsInventedInformation(response))
        {
            result.Violations.Add("Potentially invented vehicle information");
            _logger.LogWarning("Response may contain invented vehicle information");
        }

        var moderationResult = ModerateContent(response);
        if (!moderationResult.IsAppropriate)
        {
            result.Violations.Add($"Inappropriate content in response: {string.Join(", ", moderationResult.Flags)}");
            result.IsValid = false;
            _logger.LogError("LLM generated inappropriate content");
        }

        if (result.Violations.Any())
        {
            result.IsValid = piiDetected ? true : false;
            
            if (!piiDetected || result.Violations.Count > 3)
            {
                result.IsValid = false;
            }
        }

        _logger.LogInformation("Response validation completed. IsValid: {IsValid}, Violations: {ViolationCount}", result.IsValid, result.Violations.Count);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Detecta promesas o compromisos no autorizados en las respuestas
    /// como garantías, descuentos, aprobaciones de crédito o modificaciones de precio
    /// </summary>
    private List<string> DetectUnauthorizedPromises(string response)
    {
        var violations = new List<string>();

        var unauthorizedPatterns = new Dictionary<string, string>
        {
            { @"te garantizo (?:que|el|la)", "Unauthorized guarantee" },
            { @"te prometo", "Unauthorized promise" },
            { @"descuento (de|del) \d+%", "Unauthorized discount promise" },
            { @"precio especial solo para ti", "Unauthorized special pricing" },
            { @"sin necesidad de (evaluación|crédito|enganche)", "Unauthorized financing terms" },
            { @"garantía extendida gratis", "Unauthorized extended warranty" },
            { @"puedo modificar el precio", "Unauthorized price modification" },
            { @"te apruebo el crédito", "Unauthorized credit approval" }
        };

        foreach (var pattern in unauthorizedPatterns)
        {
            if (Regex.IsMatch(response, pattern.Key, RegexOptions.IgnoreCase))
            {
                violations.Add(pattern.Value);
            }
        }

        return violations;
    }

    /// <summary>
    /// Detecta indicadores de información inventada o especificaciones técnicas inventadas
    /// que no deberían estar en las respuestas
    /// </summary>
    private bool ContainsInventedInformation(string response)
    {
        var inventionIndicators = new[]
        {
            @"este (auto|vehículo|coche) tiene exactamente \d+ hp",
            @"viene (con|equipado con) asientos de (piel|cuero) (italiana|premium)",
            @"consumo de exactamente \d+\.\d+ (km/l|l/100km)",
            @"velocidad máxima de \d+ km/h",
            @"aceleración de 0 a 100 en \d+\.\d+ segundos"
        };

        return inventionIndicators.Any(pattern =>
            Regex.IsMatch(response, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Valida la calidad básica de una respuesta verificando longitud mínima
    /// y uso excesivo de emojis
    /// </summary>
    public bool ValidateResponseQuality(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        if (response.Length < 20)
        {
            return false;
        }

        var emojiCount = response.Count(c => c >= 0x1F600 && c <= 0x1F64F);
        if (emojiCount > response.Length / 2)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Modera el contenido de un mensaje verificando discurso de odio, violencia,
    /// contenido sexual, lenguaje ofensivo y temas fuera del contexto del negocio
    /// </summary>
    private ContentModerationResult ModerateContent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new ContentModerationResult
            {
                IsAppropriate = true,
                Severity = ModerationSeverity.None
            };
        }

        var result = new ContentModerationResult
        {
            IsAppropriate = true,
            Severity = ModerationSeverity.None
        };

        var messageLower = message.ToLower();

        if (ContainsKeywords(messageLower, _hateSpeechKeywords))
        {
            result.IsAppropriate = false;
            result.Flags.Add(ModerationFlag.HateSpeech);
            result.Severity = ModerationSeverity.High;
            result.SuggestedResponse = "No puedo continuar con este tipo de conversación. Si necesitas ayuda con la compra de un vehículo, estaré encantado de asistirte.";
            
            _logger.LogWarning("Hate speech detected in message");
            return result;
        }

        if (ContainsKeywords(messageLower, _violenceKeywords))
        {
            result.IsAppropriate = false;
            result.Flags.Add(ModerationFlag.Violence);
            result.Severity = ModerationSeverity.High;
            result.SuggestedResponse = "Este tipo de contenido no es apropiado para nuestra conversación. Si necesitas ayuda urgente, por favor contacta a las autoridades correspondientes.";
            
            _logger.LogWarning("Violent content detected in message");
            return result;
        }

        if (ContainsKeywords(messageLower, _sexualContentKeywords))
        {
            result.IsAppropriate = false;
            result.Flags.Add(ModerationFlag.SexualContent);
            result.Severity = ModerationSeverity.High;
            result.SuggestedResponse = "Este tipo de conversación no es apropiada. Estoy aquí para ayudarte con la compra de vehículos. ¿Puedo asistirte con eso?";
            
            _logger.LogWarning("Sexual content detected in message");
            return result;
        }

        if (ContainsOffensiveLanguage(messageLower))
        {
            result.IsAppropriate = false;
            result.Flags.Add(ModerationFlag.Harassment);
            result.Severity = ModerationSeverity.Medium;
            result.SuggestedResponse = "Entiendo que puedes estar frustrado, pero necesito que mantengamos una conversación respetuosa para poder ayudarte. ¿Cómo puedo asistirte hoy con la compra de un vehículo?";
            
            _logger.LogWarning("Offensive language detected in message");
            return result;
        }

        if (IsOffTopic(messageLower))
        {
            result.IsAppropriate = false;
            result.Flags.Add(ModerationFlag.OffTopic);
            result.Severity = ModerationSeverity.Low;
            result.SuggestedResponse = "Aprecio tu interés, pero mi especialidad es ayudarte con la compra de vehículos. ¿Puedo asistirte en encontrar el auto ideal para ti o resolver dudas sobre nuestros servicios?";
            
            _logger.LogInformation("Off-topic message detected");
            return result;
        }

        _logger.LogDebug("Message passed content moderation");
        return result;
    }

    /// <summary>
    /// Genera un mensaje de respuesta apropiado basado en el tipo y severidad de la violación
    /// así como el número de violaciones acumuladas del usuario
    /// </summary>
    public string GetResponseForViolation(ContentModerationResult moderationResult, int violationCount = 1)
    {
        if (violationCount == 1 && moderationResult.SuggestedResponse != null)
        {
            return moderationResult.SuggestedResponse;
        }

        if (violationCount >= 2 && moderationResult.Severity >= ModerationSeverity.Medium)
        {
            return "No puedo continuar esta conversación. Si necesitas asistencia para comprar un vehículo en el futuro, estaremos disponibles. Que tengas buen día.";
        }

        return moderationResult.SuggestedResponse ?? "Lo siento, no puedo ayudarte con eso. ¿Hay algo relacionado con vehículos en lo que pueda asistirte?";
    }

    /// <summary>
    /// Verifica si un mensaje contiene alguna de las palabras clave proporcionadas
    /// </summary>
    private bool ContainsKeywords(string message, HashSet<string> keywords)
    {
        return keywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Detecta lenguaje ofensivo o insultos en el mensaje
    /// </summary>
    private bool ContainsOffensiveLanguage(string message)
    {
        var offensivePatterns = new[]
        {
            @"\bidiot[ao]s?\b",
            @"\best[úu]pid[ao]s?\b",
            @"\bimbécil(es)?\b",
            @"\bpend[e]j[ao]s?\b",
            @"\bmierda\b",
            @"\bcarajo\b",
            @"\bchingar",
            @"\bputas?\b",
        };

        foreach (var pattern in offensivePatterns)
        {
            if (Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determina si el mensaje está fuera del contexto del negocio de venta de vehículos
    /// Evalúa temas ajenos, preguntas personales y solicitudes de tareas no relacionadas
    /// </summary>
    private bool IsOffTopic(string message)
    {
        bool hasOffTopicKeywords = ContainsKeywords(message, _offTopicPatterns);
        
        bool hasBusinessKeywords = ContainsKeywords(message, _businessKeywords);

        var personalQuestionPatterns = new[]
        {
            @"cómo estás",
            @"qué haces",
            @"tienes novi[ao]",
            @"cuántos años tienes",
            @"dónde vives",
            @"te gusta",
            @"qué opinas de",
            @"cuéntame de ti",
            @"háblame de"
        };

        bool isPersonalQuestion = personalQuestionPatterns.Any(pattern => Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase));

        if (hasOffTopicKeywords && !hasBusinessKeywords)
        {
            return true;
        }

        if (isPersonalQuestion && !hasBusinessKeywords)
        {
            return true;
        }

        if (message.Length < 50)
        {
            var taskRequestPatterns = new[]
            {
                @"traduc(e|ir|ción)",
                @"resuelve.*problema",
                @"ayúdame con.*tarea",
                @"escribe.*ensayo",
                @"dame.*receta",
                @"dime.*chiste"
            };

            if (taskRequestPatterns.Any(pattern =>
                Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
