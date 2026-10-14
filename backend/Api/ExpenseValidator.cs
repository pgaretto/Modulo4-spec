using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

/// <summary>Reglas de un gasto confirmado o editado (FR-009, FR-004b). Devuelve los errores; vacío = válido.</summary>
public static class ExpenseValidator
{
    public const decimal MaxAmount = 999_999_999_999m;

    public static IReadOnlyList<string> Validate(ExpenseInput expense, Func<Guid, bool> categoryIsVisible)
    {
        var errors = new List<string>();

        if (expense.Amount <= 0)
        {
            errors.Add("El monto debe ser mayor a 0.");
        }
        else if (expense.Amount > MaxAmount)
        {
            errors.Add("El monto es demasiado grande.");
        }
        else if (decimal.Round(expense.Amount, 2) != expense.Amount)
        {
            errors.Add("El monto admite como máximo 2 decimales.");
        }

        if (expense.Date == default)
        {
            errors.Add("La fecha del gasto es obligatoria.");
        }

        // Solo categorías del sistema o propias del usuario (el filtro de datos oculta las ajenas).
        if (!categoryIsVisible(expense.CategoryId))
        {
            errors.Add("La categoría no existe.");
        }

        return errors;
    }
}
