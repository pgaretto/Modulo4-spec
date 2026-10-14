using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Extraction;

/// <summary>
/// Convierte la salida cruda del modelo en la propuesta para el modal: máximo un gasto, primero por orden
/// de aparición (FR-005), fecha por defecto (FR-003b), categoría "Otros" cuando no se puede deducir (FR-004).
/// </summary>
public static class ProposalMapper
{
    public static Proposal Map(ModelOutput output, DateOnly today, IEnumerable<Category> systemCategories)
    {
        var warnings = new List<string>();

        var expense = MapExpense(output, today, systemCategories, warnings);

        foreach (var warning in output.Warnings)
        {
            if (ProposalWarnings.All.Contains(warning) && !warnings.Contains(warning))
            {
                warnings.Add(warning);
            }
        }

        return new Proposal(null, expense, warnings);
    }

    private static ExpenseInput? MapExpense(
        ModelOutput output, DateOnly today, IEnumerable<Category> systemCategories, List<string> warnings)
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

        return new ExpenseInput(first.Amount, categoryId, first.Date ?? today);
    }
}
