using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Tests.Integration;

/// <summary>
/// Fede no puede listar, editar ni borrar lo de Sofi, y la respuesta es idéntica a la de un ítem inexistente,
/// sin revelar que existe (FR-007d, RNF-03, AC-10).
/// </summary>
public class SavedItemsIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SavedItemsIsolationTests(ApiFactory factory) => _factory = factory;

    private static readonly object EventBody = new { title = "Hackeado", date = "2026-11-20", time = (string?)null };
    private static readonly object ExpenseBody = new { amount = 1, categoryId = Category.System[0].Id, date = "2026-11-20" };

    private async Task<(Guid EventId, Guid ExpenseId)> SofiSaves()
    {
        var body = await (await _factory.CreateClientFor("sofi").PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "Examen de Sofi", date = "2026-11-20", time = "08:00" },
            expense = new { amount = 4321, categoryId = Category.System[4].Id, date = "2026-11-20" },
        })).Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("event").GetProperty("id").GetGuid(), body.GetProperty("expense").GetProperty("id").GetGuid());
    }

    private static async Task<(HttpStatusCode Status, string Body)> Send(HttpClient client, HttpMethod method, string url, object? body = null)
    {
        var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) });
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Fede_no_ve_los_eventos_ni_gastos_de_Sofi_en_los_listados()
    {
        await SofiSaves();
        var fede = _factory.CreateClientFor("fede");

        var events = (await fede.GetFromJsonAsync<List<JsonElement>>("/api/events?from=2026-11-01&to=2026-11-30"))!;
        var expenses = (await fede.GetFromJsonAsync<List<JsonElement>>("/api/expenses?from=2026-11-01&to=2026-11-30"))!;

        Assert.DoesNotContain(events, e => e.GetProperty("title").GetString() == "Examen de Sofi");
        Assert.DoesNotContain(expenses, e => e.GetProperty("amount").GetDecimal() == 4321);
    }

    [Fact]
    public async Task Editar_o_borrar_lo_de_Sofi_responde_igual_que_un_id_inexistente()
    {
        var (eventId, expenseId) = await SofiSaves();
        var fede = _factory.CreateClientFor("fede");
        var missing = Guid.NewGuid();

        var ajenoPutEvento = await Send(fede, HttpMethod.Put, $"/api/events/{eventId}", EventBody);
        var vacioPutEvento = await Send(fede, HttpMethod.Put, $"/api/events/{missing}", EventBody);
        var ajenoPutGasto = await Send(fede, HttpMethod.Put, $"/api/expenses/{expenseId}", ExpenseBody);
        var vacioPutGasto = await Send(fede, HttpMethod.Put, $"/api/expenses/{missing}", ExpenseBody);
        var ajenoDelEvento = await Send(fede, HttpMethod.Delete, $"/api/events/{eventId}");
        var vacioDelEvento = await Send(fede, HttpMethod.Delete, $"/api/events/{missing}");
        var ajenoDelGasto = await Send(fede, HttpMethod.Delete, $"/api/expenses/{expenseId}");
        var vacioDelGasto = await Send(fede, HttpMethod.Delete, $"/api/expenses/{missing}");

        Assert.Equal(vacioPutEvento, ajenoPutEvento);
        Assert.Equal(vacioPutGasto, ajenoPutGasto);
        Assert.Equal(vacioDelEvento, ajenoDelEvento);
        Assert.Equal(vacioDelGasto, ajenoDelGasto);
        Assert.Equal(HttpStatusCode.NotFound, ajenoPutEvento.Status);
    }

    [Fact]
    public async Task Despues_de_los_intentos_de_Fede_lo_de_Sofi_sigue_igual()
    {
        var (eventId, expenseId) = await SofiSaves();
        var fede = _factory.CreateClientFor("fede");

        await fede.PutAsJsonAsync($"/api/events/{eventId}", EventBody);
        await fede.PutAsJsonAsync($"/api/expenses/{expenseId}", ExpenseBody);
        await fede.DeleteAsync($"/api/events/{eventId}");
        await fede.DeleteAsync($"/api/expenses/{expenseId}");

        var sofi = _factory.CreateClientFor("sofi");
        var events = (await sofi.GetFromJsonAsync<List<JsonElement>>("/api/events?from=2026-11-01&to=2026-11-30"))!;
        var expenses = (await sofi.GetFromJsonAsync<List<JsonElement>>("/api/expenses?from=2026-11-01&to=2026-11-30"))!;
        Assert.Contains(events, e => e.GetProperty("id").GetGuid() == eventId && e.GetProperty("title").GetString() == "Examen de Sofi");
        Assert.Contains(expenses, e => e.GetProperty("id").GetGuid() == expenseId && e.GetProperty("amount").GetDecimal() == 4321);
    }
}
