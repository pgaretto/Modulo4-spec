namespace SmartAgenda.Api.Domain;

public class Event
{
    public const int TitleMaxLength = 120;

    public Guid Id { get; set; }
    public required string UserId { get; set; }

    /// <summary>Obligatorio, 1 a 120 caracteres.</summary>
    public required string Title { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Nulo = todo el día.</summary>
    public TimeOnly? Time { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
