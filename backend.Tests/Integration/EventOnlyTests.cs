using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Extraction;

namespace SmartAgenda.Tests.Integration;

/// <summary>Una frase solo con evento propone solo el evento y no crea ningún gasto (US3, AC-04, FR-005).</summary>
public class EventOnlyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventOnlyTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Ai.Reset();
    }

    [Fact]
    public async Task Una_frase_sin_monto_propone_solo_el_evento()
    {
        _factory.Ai.Output = new ModelOutput([new ModelEvent("Reunión con mamá", new DateOnly(2026, 10, 15), "10:00")], [], []);

        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/proposals", new { text = "Mañana tengo reunión con mamá a las 10", today = "2026-10-14" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ev = body.GetProperty("event");
        Assert.Equal("Reunión con mamá", ev.GetProperty("title").GetString());
        Assert.Equal("2026-10-15", ev.GetProperty("date").GetString());
        Assert.Equal("10:00", ev.GetProperty("time").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("expense").ValueKind);
    }

    [Fact]
    public async Task Confirmar_solo_el_evento_no_crea_ningun_gasto()
    {
        var (_, expensesBefore) = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries",
            new { @event = new { title = "Reunión con mamá", date = "2026-10-15", time = "10:00" } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var (events, expenses) = await _factory.CountRowsAsync();
        Assert.Equal(1, events);
        Assert.Equal(expensesBefore, expenses);
    }
}
