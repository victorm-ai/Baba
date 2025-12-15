using Baba.Chatbot.Application.Abstractions;
using Baba.Chatbot.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace Baba.Chatbot.Integrations.Llm;

/// <summary>
/// Cliente para interactuar con modelos de lenguaje de OpenAI
/// Implementa generación de respuestas con RAG y function calling para búsqueda de vehículos
/// </summary>
public class LlmClient : ILlmClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<LlmClient> _logger;
    private readonly KnowledgeRepository _knowledgeRepository;
    private readonly ICatalogRepository? _catalogRepository;
    private readonly string _model;
    private readonly float _temperature;

    /// <summary>
    /// Inicializa una nueva instancia del cliente LLM con configuración de OpenAI
    /// </summary>
    public LlmClient(IConfiguration configuration, ILogger<LlmClient> logger, KnowledgeRepository knowledgeRepository, ICatalogRepository? catalogRepository = null)
    {
        _logger = logger;
        _knowledgeRepository = knowledgeRepository;
        _catalogRepository = catalogRepository;

        var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key is not configured");
        
        _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _temperature = (float)configuration.GetValue<double>("OpenAI:Temperature", 0.7);

        _chatClient = new ChatClient(_model, apiKey);
        
        _logger.LogInformation("LlmClient initialized with model: {Model}", _model);
    }

    /// <summary>
    /// Genera una respuesta usando el LLM con contexto RAG y herramientas de búsqueda de vehículos
    /// Enriquece el prompt del sistema con información relevante de la base de conocimiento
    /// </summary>
    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage,List<string>? conversationHistory = null,CancellationToken cancellationToken = default)
    {
        try
        {
            var relevantContext = _knowledgeRepository.SearchRelevantContext(userMessage, maxChunks: 3);
            
            var enhancedSystemPrompt = systemPrompt;
            if (!string.IsNullOrEmpty(relevantContext))
            {
                enhancedSystemPrompt = $"{systemPrompt}\n\n---\n\n## INFORMACIÓN DE CONTEXTO (Base de Conocimiento)\n\nUsa esta información para responder con datos precisos:\n\n{relevantContext}\n\n---\n\nIMPORTANTE: Usa SOLO la información del contexto cuando esté disponible. Si la información no está en el contexto, indica que necesitas verificar con un asesor.";
                _logger.LogInformation("Enhanced prompt with {Length} characters of context", relevantContext.Length);
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(enhancedSystemPrompt)
            };

            if (conversationHistory != null && conversationHistory.Any())
            {
                for (int i = 0; i < conversationHistory.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        messages.Add(new UserChatMessage(conversationHistory[i]));
                    }
                    else
                    {
                        messages.Add(new AssistantChatMessage(conversationHistory[i]));
                    }
                }
            }

            messages.Add(new UserChatMessage(userMessage));

            _logger.LogDebug("Sending chat completion request with {MessageCount} messages", messages.Count);

            var options = new ChatCompletionOptions
            {
                Temperature = _temperature
            };

            if (_catalogRepository != null)
            {
                options.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: "search_vehicles",
                    functionDescription: "Busca vehículos en el inventario según criterios específicos como marca, modelo, precio, año, etc.",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "brand": {
                                "type": "string",
                                "description": "Marca del vehículo (ej: Toyota, Honda, BMW)"
                            },
                            "model": {
                                "type": "string",
                                "description": "Modelo del vehículo (ej: Corolla, Civic, Serie 3)"
                            },
                            "min_price": {
                                "type": "number",
                                "description": "Precio mínimo en pesos"
                            },
                            "max_price": {
                                "type": "number",
                                "description": "Precio máximo en pesos"
                            },
                            "min_year": {
                                "type": "integer",
                                "description": "Año mínimo del vehículo"
                            },
                            "max_year": {
                                "type": "integer",
                                "description": "Año máximo del vehículo"
                            },
                            "max_mileage": {
                                "type": "integer",
                                "description": "Kilometraje máximo permitido"
                            }
                        },
                        "required": []
                    }
                    """
                    )
                ));

                options.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: "get_vehicle_details",
                    functionDescription: "Obtiene información detallada de un vehículo específico usando su ID o stock_id",
                    functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "vehicle_id": {
                                "type": "string",
                                "description": "ID o stock_id del vehículo"
                            }
                        },
                        "required": ["vehicle_id"]
                    }
                    """
                    )
                ));
            }

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                var response = await HandleToolCallsAsync(messages, completion.Value, options, cancellationToken);
                return response;
            }

            var responseText = completion.Value.Content[0].Text;
            
            _logger.LogInformation("Generated response: {ResponseLength} characters", responseText.Length);
            
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating LLM response");
            throw;
        }
    }

    /// <summary>
    /// Maneja las llamadas a herramientas (function calling) del LLM
    /// Ejecuta las funciones solicitadas y envía los resultados de vuelta al modelo
    /// </summary>
    private async Task<string> HandleToolCallsAsync(List<ChatMessage> messages,ChatCompletion completion,ChatCompletionOptions options,CancellationToken cancellationToken)
    {
        if (_catalogRepository == null)
        {
            return "Lo siento, no puedo consultar el catálogo en este momento.";
        }

        messages.Add(new AssistantChatMessage(completion));

        foreach (var toolCall in completion.ToolCalls)
        {
            var functionName = toolCall.FunctionName;
            var functionArgs = toolCall.FunctionArguments.ToString();

            _logger.LogInformation("Processing tool call: {FunctionName} with args: {Args}", functionName, functionArgs);

            string functionResult;
            try
            {
                functionResult = await ExecuteToolAsync(functionName, functionArgs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {FunctionName}", functionName);
                functionResult = JsonSerializer.Serialize(new { error = "Error ejecutando la búsqueda" });
            }

            messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
        }

        var finalCompletion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        
        return finalCompletion.Value.Content[0].Text;
    }

    /// <summary>
    /// Ejecuta una función/herramienta específica según su nombre
    /// Soporta búsqueda de vehículos y obtención de detalles
    /// </summary>
    private async Task<string> ExecuteToolAsync(string functionName, string functionArgs, CancellationToken cancellationToken)
    {
        if (_catalogRepository == null)
        {
            return JsonSerializer.Serialize(new { error = "Catálogo no disponible" });
        }

        try
        {
            switch (functionName)
            {
                case "search_vehicles":
                    var searchParams = JsonSerializer.Deserialize<SearchVehiclesParams>(functionArgs);
                    if (searchParams == null)
                    {
                        return JsonSerializer.Serialize(new { error = "Parámetros inválidos" });
                    }

                    var query = new VehicleQuery
                    {
                        Brand = searchParams.brand,
                        Model = searchParams.model,
                        MinPrice = searchParams.min_price,
                        MaxPrice = searchParams.max_price,
                        MinYear = searchParams.min_year,
                        MaxYear = searchParams.max_year,
                        MaxMileage = searchParams.max_mileage
                    };

                    var vehicles = await _catalogRepository.SearchVehiclesAsync(query, cancellationToken);
                    
                    var limitedVehicles = vehicles.Take(10).Select(v => new
                    {
                        id = v.StockId,
                        marca = v.Brand,
                        modelo = v.Model,
                        año = v.Year,
                        version = v.Version,
                        precio = v.Price,
                        kilometraje = v.Mileage,
                        dimensiones = new
                        {
                            largo = v.Length,
                            ancho = v.Width,
                            alto = v.Height
                        },
                        caracteristicas = new
                        {
                            bluetooth = v.HasBluetooth,
                            carplay = v.HasCarPlay
                        }
                    }).ToList();

                    return JsonSerializer.Serialize(new
                    {
                        total = vehicles.Count,
                        resultados_mostrados = limitedVehicles.Count,
                        vehiculos = limitedVehicles
                    });

                case "get_vehicle_details":
                    var detailParams = JsonSerializer.Deserialize<GetVehicleDetailsParams>(functionArgs);
                    if (detailParams?.vehicle_id == null)
                    {
                        return JsonSerializer.Serialize(new { error = "ID de vehículo requerido" });
                    }

                    var vehicle = await _catalogRepository.GetVehicleByIdAsync(detailParams.vehicle_id, cancellationToken);
                    
                    if (vehicle == null)
                    {
                        return JsonSerializer.Serialize(new { error = "Vehículo no encontrado" });
                    }

                    return JsonSerializer.Serialize(new
                    {
                        id = vehicle.StockId,
                        marca = vehicle.Brand,
                        modelo = vehicle.Model,
                        año = vehicle.Year,
                        version = vehicle.Version,
                        precio = vehicle.Price,
                        kilometraje = vehicle.Mileage,
                        dimensiones = new
                        {
                            largo = vehicle.Length,
                            ancho = vehicle.Width,
                            alto = vehicle.Height
                        },
                        caracteristicas = new
                        {
                            bluetooth = vehicle.HasBluetooth,
                            carplay = vehicle.HasCarPlay
                        }
                    });

                default:
                    return JsonSerializer.Serialize(new { error = $"Función desconocida: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ExecuteToolAsync para {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = "Error interno al procesar la solicitud" });
        }
    }

    /// <summary>
    /// Genera una respuesta estructurada del LLM según un esquema específico
    /// </summary>
    public Task<T> GenerateStructuredResponseAsync<T>(string prompt, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogWarning("Structured response generation not yet implemented");
        throw new NotImplementedException("Structured response generation not yet implemented");
    }

    /// <summary>
    /// DTO para deserializar parámetros de búsqueda de vehículos
    /// </summary>
    private class SearchVehiclesParams
    {
        public string? brand { get; set; }
        public string? model { get; set; }
        public decimal? min_price { get; set; }
        public decimal? max_price { get; set; }
        public int? min_year { get; set; }
        public int? max_year { get; set; }
        public int? max_mileage { get; set; }
    }

    /// <summary>
    /// DTO para deserializar parámetros de obtención de detalles de vehículo
    /// </summary>
    private class GetVehicleDetailsParams
    {
        public string? vehicle_id { get; set; }
    }
}

