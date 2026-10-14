using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

/// <summary>
/// Eventos guardados del usuario: listar, editar y borrar (FR-007b, FR-007c, FR-007d, FR-007e). Las consultas
/// pasan por el filtro de datos del usuario; un ítem ajeno responde igual que uno inexistente (404).
/// </summary>
public static class EventsEndpoints
{
    public static void MapEvents(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/events", async (DateOnly? from, DateOnly? to, AppDbContext db, CancellationToken ct) =>
        {
            if (from is null || to is null || to < from)
            {
                return ApiErrors.BadRequest("Indicá el rango de fechas (from y to).");
            }

            var events = await db.Events
                .Where(e => e.Date >= from && e.Date <= to)
                .OrderBy(e => e.Date).ThenBy(e => e.Time)
                .ToListAsync(ct);
            return Results.Ok(events.Select(EntriesEndpoints.ToDto));
        });

        // La edición la hace el propio usuario en el modal de confirmación; no pasa de nuevo por la IA.
        app.MapPut("/api/events/{id:guid}", async (Guid id, EventInput input, AppDbContext db, CancellationToken ct) =>
        {
            var errors = EventValidator.Validate(input);
            if (errors.Count > 0)
            {
                return ApiErrors.BadRequest(string.Join(" ", errors));
            }

            var saved = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (saved is null)
            {
                return ApiErrors.NotFound();
            }

            saved.Title = input.Title.Trim();
            saved.Date = input.Date;
            saved.Time = EventValidator.ParseTime(input.Time);
            await db.SaveChangesAsync(ct);
            return Results.Ok(EntriesEndpoints.ToDto(saved));
        });

        // Borrado definitivo: no hay forma de deshacerlo (FR-007c).
        app.MapDelete("/api/events/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var saved = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (saved is null)
            {
                return ApiErrors.NotFound();
            }

            db.Events.Remove(saved);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
