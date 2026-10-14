using SmartAgenda.Api.Domain;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Api.Endpoints;

public record ProposalRequest(string? Text, DateOnly Today);

public static class ProposalsEndpoints
{
    public const int MaxTextLength = 500;

    /// <summary>
    /// POST /api/proposals: interpreta la frase y devuelve una propuesta. NO guarda nada y no toca la base de
    /// datos (Principio I): lo único que persiste datos es la confirmación del usuario en /api/entries.
    /// </summary>
    public static void MapProposals(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/proposals", async (ProposalRequest request, IExtractionService extraction, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return ApiErrors.BadRequest("El texto no puede estar vacío.");
            }

            if (request.Text.Length > MaxTextLength)
            {
                return ApiErrors.BadRequest($"El texto no puede superar los {MaxTextLength} caracteres.");
            }

            if (request.Today == default)
            {
                return ApiErrors.BadRequest("Falta la fecha de hoy del usuario.");
            }

            ModelOutput output;
            try
            {
                output = await extraction.ExtractAsync(request.Text.Trim(), request.Today, ct);
            }
            catch (ExtractionUnavailableException)
            {
                return ApiErrors.GatewayTimeout("No se pudo interpretar el texto. Probá de nuevo.");
            }

            var proposal = ProposalMapper.Map(output, request.Today, Category.System);
            return proposal.Event is null && proposal.Expense is null
                ? ApiErrors.Unprocessable("No encontré ningún evento ni gasto en esa frase.")
                : Results.Ok(proposal);
        });
}
