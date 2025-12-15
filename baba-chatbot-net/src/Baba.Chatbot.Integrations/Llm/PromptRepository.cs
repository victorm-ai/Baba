using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Baba.Chatbot.Integrations.Llm;

public class PromptRepository
{
    private readonly ILogger<PromptRepository> _logger;
    private readonly string _promptsPath;

    public PromptRepository(IConfiguration configuration, ILogger<PromptRepository> logger)
    {
        _logger = logger;
        _promptsPath = configuration["Rag:PromptsPath"] ?? "./config/prompts";
    }

    public async Task<string> GetSystemPromptAsync()
    {
        return await LoadPromptAsync("system.md");
    }

    public async Task<string> GetGuardrailsPromptAsync()
    {
        return await LoadPromptAsync("guardrails.md");
    }

    public async Task<string> GetValuePropPromptAsync()
    {
        return await LoadPromptAsync("value-prop.md");
    }

    public async Task<string> GetResponseStylePromptAsync()
    {
        return await LoadPromptAsync("response-style.md");
    }

    private async Task<string> LoadPromptAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_promptsPath, fileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Prompt file not found: {FilePath}", filePath);
                return string.Empty;
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prompt file: {FileName}", fileName);
            return string.Empty;
        }
    }
}

