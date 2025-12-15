using Baba.Chatbot.Application.Abstractions;
using Baba.Chatbot.Application.Conversation.Guardrails;
using Baba.Chatbot.Application.Conversation.Orchestrator;
using Baba.Chatbot.Integrations.Catalog;
using Baba.Chatbot.Integrations.Llm;

namespace Baba.Chatbot.Api.Extensions;

/// <summary>
/// Extensiones para configurar la inyección de dependencias del proyecto
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios de la capa de aplicación (orquestador y validadores)
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IGuardrailsValidator, GuardrailsValidator>();
        services.AddScoped<ConversationOrchestrator>();
        
        return services;
    }

    /// <summary>
    /// Registra los servicios de integración (LLM, catálogo, prompts y base de conocimiento)
    /// </summary>
    public static IServiceCollection AddIntegrationServices(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddSingleton<KnowledgeRepository>();        
        services.AddSingleton<ILlmClient, LlmClient>();        
        services.AddSingleton<PromptRepository>();        
        services.AddSingleton<ICatalogRepository, CatalogRepository>();
        
        return services;
    }
}

