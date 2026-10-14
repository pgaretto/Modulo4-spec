using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Eval;

/// <summary>
/// RNF-01 / AC-12: en 100 solicitudes consecutivas, el percentil 95 de la interpretación de la IA debe ser menor
/// a 3 s. Mide el servicio de extracción con la configuración real (timeout de 4 s, sin reintentos); el endpoint
/// solo suma validaciones locales. Una solicitud que falla cuenta como más lenta que el límite. Correr con
/// <c>ANTHROPIC_EVAL_KEY=... dotnet test --filter LatencyEval</c>.
/// </summary>
public class LatencyEval
{
    private const int Requests = 100;
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(3);

    [EvalFact]
    public async Task El_percentil_95_de_100_solicitudes_consecutivas_es_menor_a_3_segundos()
    {
        var phrases = ExtractionAccuracyEval.LoadPhrases();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Anthropic:ApiKey"] = Environment.GetEnvironmentVariable(EvalFactAttribute.KeyVariable),
        }).Build();
        var service = new AnthropicExtractionService(configuration, NullLogger<AnthropicExtractionService>.Instance);

        var times = new List<TimeSpan>();
        for (var i = 0; i < Requests; i++)
        {
            var phrase = phrases[i % phrases.Count];
            var watch = Stopwatch.StartNew();
            try
            {
                await service.ExtractAsync(phrase.Text, phrase.Today, CancellationToken.None);
            }
            catch (ExtractionUnavailableException)
            {
                // Falló o venció el timeout: cuenta como más lenta que el límite.
                watch.Restart();
                times.Add(Limit + TimeSpan.FromSeconds(1));
                continue;
            }

            times.Add(watch.Elapsed);
        }

        var ordered = times.Order().ToList();
        var p95 = ordered[(int)Math.Ceiling(Requests * 0.95) - 1];
        Assert.True(p95 < Limit, $"p95 = {p95.TotalSeconds:F2} s (límite {Limit.TotalSeconds} s); mediana = {ordered[Requests / 2].TotalSeconds:F2} s.");
    }
}
