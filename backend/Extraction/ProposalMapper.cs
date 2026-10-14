using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Extraction;

/// <summary>
/// Convierte la salida cruda del modelo en la propuesta para el modal: máximo un evento y un gasto, el primero
/// de cada tipo por orden de aparición (FR-005); el gasto toma la fecha del evento de la frase o la de hoy
/// (FR-003b); categoría "Otros" con aviso cuando no se puede deducir (FR-004).
/// </summary>
public static class ProposalMapper
{
    public static Proposal Map(ModelOutput output, DateOnly today, IEnumerable<Category> systemCategories)
    {
        var warnings = new List<string>();

        var eventInput = MapEvent(output, warnings);
        var expense = MapExpense(output, eventInput?.Date ?? today, systemCategories, warnings);

        foreach (var warning in output.Warnings)
        {
            if (ProposalWarnings.All.Contains(warning) && !warnings.Contains(warning))
            {
                warnings.Add(warning);
            }
        }

        return new Proposal(eventInput, expense, warnings);
    }

    private static EventInput? MapEvent(ModelOutput output, List<string> warnings)
    {
        var candidates = output.Events.Where(e => !string.IsNullOrWhiteSpace(e.Title) && e.Date != default).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count > 1)
        {
            warnings.Add(ProposalWarnings.MultipleEvents);
        }

        var first = candidates[0];
        return new EventInput(first.Title.Trim(), first.Date, first.Time);
    }

    private static ExpenseInput? MapExpense(
        ModelOutput output, DateOnly defaultDate, IEnumerable<Category> systemCategories, List<string> warnings)
    {
        // Los montos no positivos no son un gasto válido: se descartan antes de elegir "el primero".
        var candidates = output.Expenses.Where(e => e.Amount > 0).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count > 1)
        {
            warnings.Add(ProposalWarnings.MultipleExpenses);
        }

        var first = candidates[0];
        var byName = systemCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        Guid categoryId;
        if (first.Category is { Length: > 0 } name && byName.TryGetValue(name.Trim(), out var id))
        {
            categoryId = id;
        }
        else
        {
            categoryId = Category.OtrosId;
            warnings.Add(ProposalWarnings.DefaultCategory);
        }

        return new ExpenseInput(first.Amount, categoryId, first.Date ?? defaultDate);
    }
}
