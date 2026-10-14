namespace SmartAgenda.Api.Domain;

/// <summary>Códigos de aviso de una propuesta (FR-014).</summary>
public static class ProposalWarnings
{
    public const string MultipleEvents = "MULTIPLE_EVENTS";
    public const string MultipleExpenses = "MULTIPLE_EXPENSES";
    public const string DefaultCategory = "DEFAULT_CATEGORY";
    public const string AmbiguousDate = "AMBIGUOUS_DATE";

    public static readonly string[] All = [MultipleEvents, MultipleExpenses, DefaultCategory, AmbiguousDate];
}

/// <summary>Evento propuesto o confirmado. <c>Time</c> con formato HH:mm; nulo = todo el día.</summary>
public record EventInput(string Title, DateOnly Date, string? Time);

public record ExpenseInput(decimal Amount, Guid CategoryId, DateOnly Date);

/// <summary>Propuesta de la IA para el modal. No se persiste (Principio I).</summary>
public record Proposal(EventInput? Event, ExpenseInput? Expense, IReadOnlyList<string> Warnings);

public record ConfirmRequest(EventInput? Event, ExpenseInput? Expense);

public record ConfirmResponse(EventDto? Event, ExpenseDto? Expense);

public record EventDto(Guid Id, string Title, DateOnly Date, string? Time);

public record ExpenseDto(Guid Id, decimal Amount, Guid CategoryId, DateOnly Date);

public record CategoryDto(Guid Id, string Name, bool IsSystem);
