using System.Globalization;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Extraction;

/// <summary>Prompt de sistema y esquema de salida para extraer eventos y gastos de una frase (FR-002 a FR-005).</summary>
public static class ExtractionPrompt
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-AR");

    /// <summary>Prompt corto y estable: lo único que cambia entre solicitudes es la fecha de hoy.</summary>
    public static string BuildSystemPrompt(DateOnly today) => $$"""
        Extraés eventos y gastos de frases en español rioplatense escritas por un usuario de una agenda.
        Hoy es {{today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}} ({{today.ToString("dddd", Spanish)}}).
        Devolvé SOLO el objeto JSON pedido. Reglas:
        - "events" y "expenses" son listas con TODOS los ítems de la frase, en el orden en que aparecen. Vacía si la frase no menciona ninguno. No inventes datos que la frase no contiene.
        - Evento: "title" es un nombre corto con la primera letra en mayúscula (ej. "Dentista", "Reunión con mamá"). "date" es yyyy-MM-dd. "time" es HH:mm en 24 horas, o null si la frase no menciona la hora ("a las 4 de la tarde" es 16:00; "a las 10" es 10:00).
        - Fechas relativas, a partir de hoy: "mañana" es el día siguiente; "el martes que viene" (o cualquier día de la semana) es la próxima ocurrencia estrictamente posterior a hoy; "el finde" es el próximo sábado; "la semana que viene" es el lunes de la semana siguiente. En los dos últimos casos agregá "AMBIGUOUS_DATE" a "warnings".
        - Gasto: "amount" es un número en pesos sin símbolos ("25 mil" es 25000; "40 lucas" es 40000; "2,5 lucas" es 2500). "date" solo si la frase da una fecha para el gasto mismo; si no, null.
        - "category" es exactamente una de: {{string.Join(", ", Category.System.Select(c => c.Name))}}. Pasajes, SUBE, nafta, combustible y taxi son Transporte; cafés y comidas son Comida; médico, dentista y farmacia son Salud; bares, cine y salidas son Salidas. Si ninguna aplica o el gasto es ambiguo (por ejemplo un kiosco), usá Otros.
        - "warnings" solo puede contener "AMBIGUOUS_DATE".
        """;

    /// <summary>Esquema JSON de la salida (salida estructurada de la API).</summary>
    public const string OutputSchema = """
        {
          "type": "object",
          "properties": {
            "events": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "title": { "type": "string" },
                  "date": { "type": "string" },
                  "time": { "anyOf": [ { "type": "string" }, { "type": "null" } ] }
                },
                "required": ["title", "date", "time"],
                "additionalProperties": false
              }
            },
            "expenses": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "amount": { "type": "number" },
                  "category": { "anyOf": [ { "type": "string" }, { "type": "null" } ] },
                  "date": { "anyOf": [ { "type": "string" }, { "type": "null" } ] }
                },
                "required": ["amount", "category", "date"],
                "additionalProperties": false
              }
            },
            "warnings": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["events", "expenses", "warnings"],
          "additionalProperties": false
        }
        """;
}
