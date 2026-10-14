using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

public static class EntriesEndpoints
{
    /// <summary>
    /// POST /api/entries: guarda lo que el usuario confirmó en el modal. Es la única vía de alta de eventos y
    /// gastos (Principio I). El dueño sale siempre del token, nunca del cuerpo (RNF-03).
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

            if (request.Event is not null)
            {
                return ApiErrors.BadRequest("Los eventos todavía no se pueden confirmar.");
            }

            // El filtro de datos solo deja ver las categorías del sistema y las propias del usuario.
            var categoryVisible = request.Expense is not null
                && await db.Categories.AnyAsync(c => c.Id == request.Expense.CategoryId, ct);

            var errors = ExpenseValidator.Validate(request.Expense!, _ => categoryVisible);
            if (errors.Count > 0)
            {
                return ApiErrors.BadRequest(string.Join(" ", errors));
            }

            var expense = new Expense
            {
                UserId = userId,
                Amount = request.Expense!.Amount,
                CategoryId = request.Expense.CategoryId,
                Date = request.Expense.Date,
            };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync(ct);

            var dto = new ExpenseDto(expense.Id, expense.Amount, expense.CategoryId, expense.Date);
            return Results.Json(new ConfirmResponse(null, dto), statusCode: StatusCodes.Status201Created);
        });
}
