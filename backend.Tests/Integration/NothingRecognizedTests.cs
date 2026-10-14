using System.Net;
using System.Net.Http.Json;

namespace SmartAgenda.Tests.Integration;

/// <summary>Sin evento ni gasto reconocible no se abre el modal ni se guarda nada (FR-011).</summary>
public class NothingRecognizedTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public NothingRecognizedTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Ai.Reset();
    }

    [Fact]
    public async Task Un_texto_sin_evento_ni_gasto_devuelve_422_y_no_guarda_nada()
    {
        var before = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/proposals", new { text = "hola, ¿cómo andás?", today = "2026-10-14" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(before, await _factory.CountRowsAsync());
    }

    [Fact]
    public async Task Confirmar_sin_evento_ni_gasto_devuelve_400_y_no_guarda_nada()
    {
        var before = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new { @event = (object?)null, expense = (object?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await _factory.CountRowsAsync());
    }
}
