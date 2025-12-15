using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Twilio.Security;

namespace Baba.Chatbot.Integrations.Twilio;

/// <summary>
/// Validador de firmas de webhooks de Twilio para seguridad
/// Verifica que las solicitudes entrantes provengan de Twilio
/// </summary>
public class TwilioValidator
{
    private readonly RequestValidator _validator;
    private readonly bool _validateSignature;

    /// <summary>
    /// Inicializa una nueva instancia del validador de Twilio
    /// </summary>
    public TwilioValidator(IConfiguration configuration)
    {
        var authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio AuthToken not configured");
        
        _validator = new RequestValidator(authToken);
        _validateSignature = configuration.GetValue<bool>("Twilio:ValidateSignature", true);
    }

    /// <summary>
    /// Valida si una solicitud HTTP proviene de Twilio verificando la firma
    /// </summary>
    public bool IsValidRequest(HttpRequest request, string twilioSignature)
    {
        if (!_validateSignature)
            return true;

        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        
        var parameters = request.Form.ToDictionary(kvp => kvp.Key,kvp => kvp.Value.ToString());

        return _validator.Validate(url, parameters, twilioSignature);
    }
}

