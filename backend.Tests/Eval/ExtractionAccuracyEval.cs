using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using SmartAgenda.Api.Domain;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Eval;

/// <summary>Se saltea si no hay clave: hace llamadas reales a Claude (cuestan unos centavos).</summary>
public sealed class EvalFactAttribute : FactAttribute
{
    public const string KeyVariable = "ANTHROPIC_EVAL_KEY";

    public EvalFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(KeyVariable)))
        {
            Skip = $"Definí {KeyVariable} para correr la evaluación contra la IA real.";
        }
    }
}

public record EvalPhrase(string Text, DateOnly Today, decimal? ExpectedAmount, string? ExpectedCategory, string? ExpectedEvent);

/// <summary>
/// RNF-06 / AC-16 (Principio III): al menos el 90 % de las frases del dataset (≥ 50) debe tener el monto y la
/// categoría del gasto idénticos a los esperados. Correr con
/// <c>ANTHROPIC_EVAL_KEY=... dotnet test --filter ExtractionAccuracyEval</c>.
/// </summary>
public class ExtractionAccuracyEval(ITestOutputHelper output)
{
    private const double MinAccuracy = 0.90;
    private const int MinPhrases = 50;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static List<EvalPhrase> LoadPhrases() =>
        JsonSerializer.Deserialize<List<EvalPhrase>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Eval", "phrases.json")), Json)!;

    [Fact]
    public void El_dataset_tiene_al_menos_50_frases_con_gastos_y_solo_eventos()
    {
        var phrases = LoadPhrases();

        Assert.True(phrases.Count >= MinPhrases, $"Hay {phrases.Count} frases; se necesitan al menos {MinPhrases} (RNF-06).");
        Assert.Contains(phrases, p => p.ExpectedAmount is null && p.ExpectedEvent is not null);
        Assert.Equal(phrases.Count, phrases.Select(p => p.Text).Distinct().Count());
    }

    [EvalFact]
    public async Task Al_menos_el_90_por_ciento_acierta_monto_y_categoria()
    {
        var phrases = LoadPhrases();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Anthropic:ApiKey"] = Environment.GetEnvironmentVariable(EvalFactAttribute.KeyVariable),
            ["Anthropic:TimeoutSeconds"] = "30", // la evaluación mide acierto; la latencia se mide aparte
        }).Build();
        var service = new AnthropicExtractionService(configuration, NullLogger<AnthropicExtractionService>.Instance);

        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
        await Parallel.ForEachAsync(phrases, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (phrase, ct) =>
        {
            try
            {
                var output = await service.ExtractAsync(phrase.Text, phrase.Today, ct);
                var proposal = ProposalMapper.Map(output, phrase.Today, Category.System);

                var category = proposal.Expense is null
                    ? null
                    : Category.System.Single(c => c.Id == proposal.Expense.CategoryId).Name;
                var ok = proposal.Expense?.Amount == phrase.ExpectedAmount && category == phrase.ExpectedCategory;
                if (!ok)
                {
                    failures.Add($"\"{phrase.Text}\" -> {proposal.Expense?.Amount} {category} (esperado {phrase.ExpectedAmount} {phrase.ExpectedCategory})");
                }
            }
            catch (ExtractionUnavailableException ex)
            {
                failures.Add($"\"{phrase.Text}\" -> falló la IA: {ex.Message}");
            }
        });

        var accuracy = 1.0 - (double)failures.Count / phrases.Count;
        // Queda en el informe del test (dotnet test --logger "console;verbosity=detailed") para registrar la medición.
        output.WriteLine($"Acierto: {accuracy:P1} ({phrases.Count - failures.Count}/{phrases.Count}); mínimo {MinAccuracy:P0}.");
        foreach (var failure in failures.Order()) output.WriteLine($"  falla: {failure}");
        Assert.True(accuracy >= MinAccuracy,
            $"Acierto {accuracy:P1} ({phrases.Count - failures.Count}/{phrases.Count}); mínimo {MinAccuracy:P0}.\n" +
            string.Join("\n", failures.Order()));
    }
}
