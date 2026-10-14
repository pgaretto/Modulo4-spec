using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Api.Endpoints;

/// <summary>
/// Gastos guardados del usuario: listar, editar y borrar (FR-007b, FR-007c, FR-007d, FR-007e). Un ítem ajeno
/// responde igual que uno inexistente (404); la categoría debe ser del sistema o propia (FR-004b).
/// </summary>
public static class ExpensesEndpoints
{
    private static ExpenseDto ToDto(Expense e) => new(e.Id, e.Amount, e.CategoryId, e.Date);

    public static void MapExpenses(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses", async (DateOnly? from, DateOnly? to, AppDbContext db, CancellationToken ct) =>
        {
            if (from is null || to is null || to < from)
            {
                return ApiErrors.BadRequest("Indicá el rango de fechas (from y to).");
            }

            var expenses = await db.Expenses
                .Where(e => e.Date >= from && e.Date <= to)
                .OrderBy(e => e.Date).ThenBy(e => e.CreatedAt)
                .ToListAsync(ct);
            return Results.Ok(expenses.Select(ToDto));
        });

        app.MapPut("/api/expenses/{id:guid}", async (Guid id, ExpenseInput input, AppDbContext db, CancellationToken ct) =>
        {
            var categoryVisible = await db.Categories.AnyAsync(c => c.Id == input.CategoryId, ct);
            var errors = ExpenseValidator.Validate(input, _ => categoryVisible);
            if (errors.Count > 0)
            {
                return ApiErrors.BadRequest(string.Join(" ", errors));
            }

            var saved = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (saved is null)
            {
                return ApiErrors.NotFound();
            }

            saved.Amount = input.Amount;
            saved.CategoryId = input.CategoryId;
            saved.Date = input.Date;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(saved));
        });

        app.MapDelete("/api/expenses/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var saved = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (saved is null)
            {
                return ApiErrors.NotFound();
            }

            db.Expenses.Remove(saved);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
