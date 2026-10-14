namespace SmartAgenda.Api.Domain;

public class Expense
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }

    /// <summary>decimal(18,2), mayor que 0.</summary>
    public decimal Amount { get; set; }

    /// <summary>Del sistema o propia del mismo usuario.</summary>
    public Guid CategoryId { get; set; }

    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
