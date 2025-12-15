using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Baba.Chatbot.Integrations.Twilio;

/// <summary>
/// Cliente para enviar mensajes SMS/WhatsApp mediante la API de Twilio
/// </summary>
public class TwilioClient
{
    private readonly ILogger<TwilioClient> _logger;
    private readonly string _phoneNumber;

    /// <summary>
    /// Inicializa una nueva instancia del cliente de Twilio
    /// Configura las credenciales de autenticación
    /// </summary>
    public TwilioClient(IConfiguration configuration, ILogger<TwilioClient> logger)
    {
        _logger = logger;

        var accountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio AccountSid not configured");
        
        var authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio AuthToken not configured");

        _phoneNumber = configuration["Twilio:PhoneNumber"] ?? throw new InvalidOperationException("Twilio PhoneNumber not configured");

        global::Twilio.TwilioClient.Init(accountSid, authToken);
    }

    /// <summary>
    /// Envía un mensaje SMS/WhatsApp a un número de teléfono específico
    /// </summary>
    public async Task<string> SendMessageAsync(string to, string body)
    {
        try
        {
            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber(to),
                from: new PhoneNumber(_phoneNumber),
                body: body
            );

            _logger.LogInformation("Message sent: {MessageSid} to {To}", message.Sid, to);
            return message.Sid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to {To}", to);
            throw;
        }
    }
}

