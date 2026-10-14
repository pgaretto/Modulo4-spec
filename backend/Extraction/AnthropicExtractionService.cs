using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace SmartAgenda.Api.Extraction;

/// <summary>
/// Extracción con Claude Haiku (RF-05, RF-06). Salida estructurada con esquema JSON, esfuerzo bajo, timeout de
/// 4 s y sin reintentos para cumplir RNF-01 y RNF-05; cualquier falla se informa como
/// <see cref="ExtractionUnavailableException"/>.
/// </summary>
public class AnthropicExtractionService : IExtractionService
{
    private const string DefaultModel = "claude-haiku-5-5";

    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicExtractionService> _logger;
    private readonly Lazy<AnthropicClient> _client;

    public AnthropicExtractionService(IConfiguration configuration, ILogger<AnthropicExtractionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _client = new Lazy<AnthropicClient>(CreateClient);
    }

    public async Task<ModelOutput> ExtractAsync(string text, DateOnly today, CancellationToken cancellationToken)
    {
        var section = _configuration.GetSection("Anthropic");
        Message response;
        try
        {
            response = await _client.Value.Messages.Create(
                new MessageCreateParams
                {
                    Model = section["Model"] ?? DefaultModel,
                    MaxTokens = section.GetValue("MaxTokens", 1024),
                    System = ExtractionPrompt.BuildSystemPrompt(today),
                    OutputConfig = new OutputConfig
                    {
                        Effort = Effort.Low,
                        Format = new JsonOutputFormat
                        {
                            Schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ExtractionPrompt.OutputSchema)!,
                        },
                    },
                    Messages = [new() { Role = Role.User, Content = text }],
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout, red, límite de uso o error de la API: para el usuario es "la IA no respondió".
            _logger.LogWarning(ex, "Falló la llamada a la IA");
            throw new ExtractionUnavailableException("La IA no respondió.", ex);
        }

        if (response.StopReason == "refusal")
        {
            throw new ExtractionUnavailableException("La IA rechazó la solicitud.");
        }

        var json = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        return ModelOutputParser.Parse(json);
    }

    private AnthropicClient CreateClient()
    {
        var section = _configuration.GetSection("Anthropic");
        var apiKey = section["ApiKey"]
            ?? throw new InvalidOperationException("Falta configurar Anthropic:ApiKey (user-secrets o variable de entorno).");

        return new AnthropicClient
        {
            ApiKey = apiKey,
            Timeout = TimeSpan.FromSeconds(section.GetValue("TimeoutSeconds", 4)),
            MaxRetries = section.GetValue("MaxRetries", 0),
        };
    }
}
