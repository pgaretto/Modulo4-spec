namespace SmartAgenda.Api.Extraction;

/// <summary>Evento tal como lo devuelve el modelo (fecha y hora ya resueltas; hora nula = sin hora en la frase).</summary>
public record ModelEvent(string Title, DateOnly Date, string? Time);

/// <summary>Gasto tal como lo devuelve el modelo; la categoría viene por nombre.</summary>
public record ModelExpense(decimal Amount, string? Category, DateOnly? Date);

/// <summary>Salida cruda del modelo, antes de mapearla a una <c>Proposal</c>.</summary>
public record ModelOutput(
    IReadOnlyList<ModelEvent> Events,
    IReadOnlyList<ModelExpense> Expenses,
    IReadOnlyList<string> Warnings);

/// <summary>La IA que interpreta una frase. Falla con <see cref="ExtractionUnavailableException"/> si no responde.</summary>
public interface IExtractionService
{
    Task<ModelOutput> ExtractAsync(string text, DateOnly today, CancellationToken cancellationToken);
}

public class ExtractionUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
