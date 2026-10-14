using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

public record CreateCategoryRequest(string? Name);

public static class CategoriesEndpoints
{
    public static void MapCategories(this IEndpointRouteBuilder app)
    {
        // Las del sistema más las propias del usuario (el filtro de datos oculta las ajenas).
        app.MapGet("/api/categories", async (AppDbContext db, CancellationToken ct) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.UserId != null)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto(c.Id, c.Name, c.UserId == null))
                .ToListAsync(ct);
            return Results.Ok(categories);
        });

        // Categoría propia: 1 a 40 caracteres, única por usuario sin distinguir mayúsculas y distinta de las del
        // sistema (FR-004b). Se guarda al crearla, aunque después se cancele el modal.
        app.MapPost("/api/categories", async (CreateCategoryRequest request, AppDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.UserId is not { } userId)
            {
                return Results.Unauthorized();
            }

            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                return ApiErrors.BadRequest("El nombre de la categoría es obligatorio.");
            }

            if (name.Length > Category.NameMaxLength)
            {
                return ApiErrors.BadRequest($"El nombre no puede superar los {Category.NameMaxLength} caracteres.");
            }

            // El filtro de datos deja ver solo las del sistema y las propias: alcanza para detectar repetidas.
            var existing = await db.Categories.Select(c => c.Name).ToListAsync(ct);
            if (existing.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            {
                return ApiErrors.Conflict("Ya existe una categoría con ese nombre.");
            }

            var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = name };
            db.Categories.Add(category);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Otra solicitud la creó justo antes: el índice único la rechaza.
                return ApiErrors.Conflict("Ya existe una categoría con ese nombre.");
            }

            return Results.Json(new CategoryDto(category.Id, category.Name, false), statusCode: StatusCodes.Status201Created);
        });
    }
}
