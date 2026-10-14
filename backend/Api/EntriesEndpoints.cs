using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

public static class EntriesEndpoints
{
    /// <summary>
    /// POST /api/entries: guarda lo que el usuario confirmó en el modal. Es la única vía de alta de eventos y
    /// gastos (Principio I). El dueño sale siempre del token, nunca del cuerpo (RNF-03). Todo o nada.
    /// </summary>
    public static void MapEntries(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/entries", async (ConfirmRequest request, AppDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            if (user.UserId is not { } userId)
            {
                return Results.Unauthorized();
            }

            if (request.Event is null && request.Expense is null)
            {
                return ApiErrors.BadRequest("Hay que confirmar al menos un evento o un gasto.");
            }

            var errors = new List<string>();
            if (request.Event is not null)
            {
                errors.AddRange(EventValidator.Validate(request.Event));
            }

            if (request.Expense is not null)
            {
                // El filtro de datos solo deja ver las categorías del sistema y las propias del usuario.
                var categoryVisible = await db.Categories.AnyAsync(c => c.Id == request.Expense.CategoryId, ct);
                errors.AddRange(ExpenseValidator.Validate(request.Expense, _ => categoryVisible));
            }

            if (errors.Count > 0)
            {
                return ApiErrors.BadRequest(string.Join(" ", errors));
            }

            Event? savedEvent = null;
            if (request.Event is { } ev)
            {
                savedEvent = new Event
                {
                    UserId = userId,
                    Title = ev.Title.Trim(),
                    Date = ev.Date,
                    Time = EventValidator.ParseTime(ev.Time),
                };
                db.Events.Add(savedEvent);
            }

            Expense? savedExpense = null;
            if (request.Expense is { } ex)
            {
                savedExpense = new Expense { UserId = userId, Amount = ex.Amount, CategoryId = ex.CategoryId, Date = ex.Date };
                db.Expenses.Add(savedExpense);
            }

            await db.SaveChangesAsync(ct); // una sola transacción: se guardan ambos o ninguno

            var response = new ConfirmResponse(
                savedEvent is null ? null : ToDto(savedEvent),
                savedExpense is null ? null : new ExpenseDto(savedExpense.Id, savedExpense.Amount, savedExpense.CategoryId, savedExpense.Date));
            return Results.Json(response, statusCode: StatusCodes.Status201Created);
        });

    public static EventDto ToDto(Event e) => new(e.Id, e.Title, e.Date, e.Time?.ToString("HH:mm"));
}
