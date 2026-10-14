using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartAgenda.Api.Extraction;

/// <summary>Lee el JSON que devuelve el modelo. Una salida que no se puede leer cuenta como falla de la IA.</summary>
public static partial class ModelOutputParser
{
    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimePattern();

    public static ModelOutput Parse(string raw)
    {
        // Tolera que el modelo envuelva el JSON en un bloque de código o agregue texto alrededor.
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new ExtractionUnavailableException("La respuesta de la IA no contiene un JSON.");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var root = doc.RootElement;

            var events = Items(root, "events").Select(e => new ModelEvent(
                e.GetProperty("title").GetString()?.Trim() ?? "",
                ParseDate(e.GetProperty("date").GetString())!.Value,
                ValidTime(OptionalString(e, "time")))).Where(e => e.Title.Length > 0).ToList();

            var expenses = Items(root, "expenses").Select(x => new ModelExpense(
                x.GetProperty("amount").GetDecimal(),
                OptionalString(x, "category"),
                ParseDate(OptionalString(x, "date")))).ToList();

            var warnings = Items(root, "warnings").Select(w => w.GetString() ?? "").Where(w => w.Length > 0).ToList();

            return new ModelOutput(events, expenses, warnings);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException or KeyNotFoundException)
        {
            throw new ExtractionUnavailableException("La respuesta de la IA no tiene el formato esperado.", ex);
        }
    }

    private static IEnumerable<JsonElement> Items(JsonElement root, string name) =>
        root.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
            : [];

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static DateOnly? ParseDate(string? value) =>
        value is null ? null : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? ValidTime(string? value) => value is not null && TimePattern().IsMatch(value) ? value : null;
}
