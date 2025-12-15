using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Baba.Chatbot.Integrations.Llm;

/// <summary>
/// Repositorio de conocimiento (RAG simple basado en archivos markdown)
/// Carga y busca información relevante en documentos de la base de conocimiento
/// </summary>
public class KnowledgeRepository
{
    private readonly ILogger<KnowledgeRepository> _logger;
    private readonly string _knowledgeBasePath;
    private readonly Dictionary<string, string> _documents = new();

    /// <summary>
    /// Inicializa una nueva instancia del repositorio de conocimiento
    /// </summary>
    public KnowledgeRepository(ILogger<KnowledgeRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        _knowledgeBasePath = configuration["KnowledgeBase:Path"] ?? 
                            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "config", "rag", "kb_sources");
        
        LoadDocuments();
    }

    /// <summary>
    /// Carga todos los documentos markdown del directorio de conocimiento
    /// Omite archivos README
    /// </summary>
    private void LoadDocuments()
    {
        try
        {
            var fullPath = _knowledgeBasePath;
            _logger.LogInformation("Loading knowledge base from: {Path}", fullPath);

            if (!Directory.Exists(fullPath))
            {
                _logger.LogWarning("Knowledge base directory not found: {Path}", fullPath);
                return;
            }

            var markdownFiles = Directory.GetFiles(fullPath, "*.md", SearchOption.AllDirectories);
            
            foreach (var file in markdownFiles)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("README", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                _documents[fileName] = content;
                _logger.LogInformation("Loaded document: {FileName} ({Size} characters)", fileName, content.Length);
            }

            _logger.LogInformation("Knowledge base loaded: {Count} documents", _documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading knowledge base");
        }
    }

    /// <summary>
    /// Busca información relevante en la base de conocimiento usando búsqueda por palabras clave
    /// Divide documentos en secciones y puntúa según relevancia
    /// </summary>
    public string SearchRelevantContext(string query, int maxChunks = 3)
    {
        if (_documents.Count == 0)
        {
            _logger.LogWarning("No documents loaded in knowledge base");
            return string.Empty;
        }

        try
        {
            var queryWords = query.ToLower()
                .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .ToHashSet();

            if (!queryWords.Any())
            {
                return string.Empty;
            }

            // Buscar documentos relevantes
            var relevantSections = new List<(string docName, string section, int score)>();

            foreach (var doc in _documents)
            {
                var sections = SplitIntoSections(doc.Value);
                
                foreach (var section in sections)
                {
                    var sectionLower = section.ToLower();
                    var score = queryWords.Count(word => sectionLower.Contains(word));
                    
                    if (score > 0)
                    {
                        relevantSections.Add((doc.Key, section, score));
                    }
                }
            }

            var topSections = relevantSections
                .OrderByDescending(s => s.score)
                .Take(maxChunks)
                .Select(s => s.section)
                .ToList();

            if (!topSections.Any())
            {
                _logger.LogDebug("No relevant context found for query: {Query}", query);
                return string.Empty;
            }

            var context = string.Join("\n\n---\n\n", topSections);
            _logger.LogInformation("Found {Count} relevant sections for query", topSections.Count);
            
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching knowledge base");
            return string.Empty;
        }
    }

    /// <summary>
    /// Divide un documento en secciones basándose en headers markdown
    /// </summary>
    private List<string> SplitIntoSections(string content)
    {
        var sections = new List<string>();
        var lines = content.Split('\n');
        var currentSection = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("## ") || line.StartsWith("### "))
            {
                if (currentSection.Any())
                {
                    var sectionText = string.Join('\n', currentSection).Trim();
                    if (sectionText.Length > 50)
                    {
                        sections.Add(sectionText);
                    }
                    currentSection.Clear();
                }
            }

            currentSection.Add(line);
        }

        if (currentSection.Any())
        {
            var sectionText = string.Join('\n', currentSection).Trim();
            if (sectionText.Length > 50)
            {
                sections.Add(sectionText);
            }
        }

        return sections;
    }

    /// <summary>
    /// Recarga los documentos de la base de conocimiento
    /// </summary>
    public void Reload()
    {
        _documents.Clear();
        LoadDocuments();
    }

    /// <summary>
    /// Obtiene estadísticas de la base de conocimiento
    /// </summary>
    public (int documentCount, int totalCharacters) GetStats()
    {
        var totalChars = _documents.Values.Sum(d => d.Length);
        return (_documents.Count, totalChars);
    }
}
