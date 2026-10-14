namespace SmartAgenda.Api.Domain;

/// <summary>Categoría de gasto. <see cref="UserId"/> nulo = categoría del sistema; con valor = propia de ese usuario.</summary>
public class Category
{
    public const int NameMaxLength = 40;

    public Guid Id { get; set; }
    public string? UserId { get; set; }

    /// <summary>1 a 40 caracteres, obligatorio.</summary>
    public required string Name { get; set; }

    public static readonly Category[] System =
    [
        new() { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "Comida" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000002"), Name = "Transporte" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000003"), Name = "Salidas" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000004"), Name = "Salud" },
        new() { Id = new Guid("00000000-0000-0000-0000-000000000005"), Name = "Otros" },
    ];

    public static Guid OtrosId => System[4].Id;
}
