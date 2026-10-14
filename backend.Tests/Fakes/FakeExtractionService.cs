using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Fakes;

/// <summary>IA simulada y configurable por prueba: respuesta fija, falla o demora.</summary>
public class FakeExtractionService : IExtractionService
{
    public ModelOutput Output { get; set; } = new([], [], []);
    public Exception? Failure { get; set; }
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public List<(string Text, DateOnly Today)> Calls { get; } = [];

    public async Task<ModelOutput> ExtractAsync(string text, DateOnly today, CancellationToken cancellationToken)
    {
        Calls.Add((text, today));
        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        return Failure is not null ? throw Failure : Output;
    }

    public void Reset()
    {
        Output = new([], [], []);
        Failure = null;
        Delay = TimeSpan.Zero;
        Calls.Clear();
    }
}
