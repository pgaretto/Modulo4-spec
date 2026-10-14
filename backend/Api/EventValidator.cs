using System.Text.RegularExpressions;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

/// <summary>Reglas de un evento confirmado o editado (FR-002, FR-009). Devuelve los errores; vacío = válido.</summary>
public static partial class EventValidator
{
    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimePattern();

    public static IReadOnlyList<string> Validate(EventInput input)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors.Add("El título del evento es obligatorio.");
        }
        else if (input.Title.Trim().Length > Event.TitleMaxLength)
        {
            errors.Add($"El título no puede superar los {Event.TitleMaxLength} caracteres.");
        }

        if (input.Date == default)
        {
            errors.Add("La fecha del evento es obligatoria.");
        }

        if (input.Time is not null && !TimePattern().IsMatch(input.Time))
        {
            errors.Add("La hora debe tener el formato HH:mm.");
        }

        return errors;
    }

    /// <summary>Hora ya validada; nulo = todo el día.</summary>
    public static TimeOnly? ParseTime(string? time) => time is null ? null : TimeOnly.ParseExact(time, "HH:mm");
}
