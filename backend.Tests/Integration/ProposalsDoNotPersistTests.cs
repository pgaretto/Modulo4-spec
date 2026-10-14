using System.Net;
using System.Net.Http.Json;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Integration;

/// <summary>Principio I: la propuesta de la IA nunca se guarda por sí sola (FR-007, SC-004).</summary>
public class ProposalsDoNotPersistTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProposalsDoNotPersistTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Ai.Reset();
    }

    [Fact]
    public async Task Pedir_una_propuesta_deja_cero_filas_nuevas()
    {
        _factory.Ai.Output = new ModelOutput([], [new ModelExpense(2500, "Comida", null)], []);
        var before = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/proposals", new { text = "Me tomé un café 2500", today = "2026-10-14" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, await _factory.CountRowsAsync());
        Assert.Equal((0, 0), await _factory.CountRowsAsync());
    }
}
