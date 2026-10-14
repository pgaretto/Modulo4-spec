using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

public static class CategoriesEndpoints
{
    /// <summary>GET /api/categories: las del sistema más las propias del usuario (el filtro oculta las ajenas).</summary>
    public static void MapCategories(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/categories", async (AppDbContext db, CancellationToken ct) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.UserId != null)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name, c.UserId == null))
                .ToListAsync(ct);
            return Results.Ok(categories);
        });
}
