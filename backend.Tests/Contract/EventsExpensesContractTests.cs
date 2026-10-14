using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Domain;
using SmartAgenda.Tests.Integration;

namespace SmartAgenda.Tests.Contract;

/// <summary>GET, PUT y DELETE de /api/events y /api/expenses (FR-007b, FR-007c, FR-007e).</summary>
public class EventsExpensesContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventsExpensesContractTests(ApiFactory factory) => _factory = factory;

    private async Task<Guid> CreateEvent(HttpClient client, string title, string date, string? time = "10:00")
    {
        var body = await (await client.PostAsJsonAsync("/api/entries", new { @event = new { title, date, time } }))
            .Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("event").GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateExpense(HttpClient client, decimal amount, string date)
    {
        var body = await (await client.PostAsJsonAsync("/api/entries",
            new { expense = new { amount, categoryId = Category.System[0].Id, date } })).Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("expense").GetProperty("id").GetGuid();
    }

    // --- Listados ---

    [Fact]
    public async Task Lista_los_eventos_del_rango_ordenados_por_fecha_y_hora()
    {
        var fede = _factory.CreateClientFor("lista-eventos");
        await CreateEvent(fede, "Tarde", "2026-11-05", "18:00");
        await CreateEvent(fede, "Mañana", "2026-11-05", "09:00");
        await CreateEvent(fede, "Antes", "2026-11-01", null);
        await CreateEvent(fede, "Fuera de rango", "2026-12-01");

        var events = (await fede.GetFromJsonAsync<List<JsonElement>>("/api/events?from=2026-11-01&to=2026-11-30"))!;

        Assert.Equal(["Antes", "Mañana", "Tarde"], events.Select(e => e.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task Lista_los_gastos_del_rango()
    {
        var fede = _factory.CreateClientFor("lista-gastos");
        await CreateExpense(fede, 111, "2026-11-10");
        await CreateExpense(fede, 222, "2026-12-10");

        var expenses = (await fede.GetFromJsonAsync<List<JsonElement>>("/api/expenses?from=2026-11-01&to=2026-11-30"))!;

        Assert.Equal([111m], expenses.Select(e => e.GetProperty("amount").GetDecimal()));
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/expenses")]
    [InlineData("/api/events?from=2026-11-01")]
    public async Task Sin_rango_de_fechas_devuelve_400(string url)
    {
        var response = await _factory.CreateClientFor("fede").GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Editar ---

    [Fact]
    public async Task Edita_un_evento()
    {
        var fede = _factory.CreateClientFor("edita-evento");
        var id = await CreateEvent(fede, "Dentista", "2026-11-20");

        var response = await fede.PutAsJsonAsync($"/api/events/{id}", new { title = "Ortodoncia", date = "2026-11-22", time = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ortodoncia", body.GetProperty("title").GetString());
        Assert.Equal("2026-11-22", body.GetProperty("date").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("time").ValueKind);
    }

    [Fact]
    public async Task Edita_un_gasto()
    {
        var fede = _factory.CreateClientFor("edita-gasto");
        var id = await CreateExpense(fede, 2500, "2026-11-20");

        var response = await fede.PutAsJsonAsync($"/api/expenses/{id}",
            new { amount = 3000, categoryId = Category.System[1].Id, date = "2026-11-21" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3000, body.GetProperty("amount").GetDecimal());
        Assert.Equal(Category.System[1].Id, body.GetProperty("categoryId").GetGuid());
    }

    [Fact]
    public async Task Editar_con_datos_invalidos_devuelve_400_y_no_cambia_nada()
    {
        var fede = _factory.CreateClientFor("edita-invalido");
        var eventId = await CreateEvent(fede, "Original", "2026-11-20");
        var expenseId = await CreateExpense(fede, 500, "2026-11-20");

        var badEvent = await fede.PutAsJsonAsync($"/api/events/{eventId}", new { title = "", date = "2026-11-20", time = (string?)null });
        var badExpense = await fede.PutAsJsonAsync($"/api/expenses/{expenseId}",
            new { amount = 0, categoryId = Category.System[0].Id, date = "2026-11-20" });

        Assert.Equal(HttpStatusCode.BadRequest, badEvent.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badExpense.StatusCode);
        var events = (await fede.GetFromJsonAsync<List<JsonElement>>("/api/events?from=2026-11-01&to=2026-11-30"))!;
        Assert.Equal("Original", events.Single().GetProperty("title").GetString());
    }

    [Fact]
    public async Task Editar_un_gasto_con_una_categoria_inexistente_devuelve_400()
    {
        var fede = _factory.CreateClientFor("edita-categoria");
        var id = await CreateExpense(fede, 500, "2026-11-20");

        var response = await fede.PutAsJsonAsync($"/api/expenses/{id}", new { amount = 500, categoryId = Guid.NewGuid(), date = "2026-11-20" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Borrar ---

    [Fact]
    public async Task Borra_un_evento_y_un_gasto_con_204_y_dejan_de_aparecer()
    {
        var fede = _factory.CreateClientFor("borra");
        var eventId = await CreateEvent(fede, "Borrame", "2026-11-25");
        var expenseId = await CreateExpense(fede, 999, "2026-11-25");

        Assert.Equal(HttpStatusCode.NoContent, (await fede.DeleteAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await fede.DeleteAsync($"/api/expenses/{expenseId}")).StatusCode);

        Assert.Empty((await fede.GetFromJsonAsync<List<JsonElement>>("/api/events?from=2026-11-01&to=2026-11-30"))!);
        Assert.Empty((await fede.GetFromJsonAsync<List<JsonElement>>("/api/expenses?from=2026-11-01&to=2026-11-30"))!);
    }

    [Fact]
    public async Task Un_id_inexistente_devuelve_404_en_editar_y_borrar()
    {
        var fede = _factory.CreateClientFor("fede");
        var missing = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await fede.PutAsJsonAsync($"/api/events/{missing}", new { title = "x", date = "2026-11-20", time = (string?)null })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fede.PutAsJsonAsync($"/api/expenses/{missing}", new { amount = 1, categoryId = Category.System[0].Id, date = "2026-11-20" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fede.DeleteAsync($"/api/events/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fede.DeleteAsync($"/api/expenses/{missing}")).StatusCode);
    }

    // --- Sin sesión ---

    [Theory]
    [InlineData("GET", "/api/events?from=2026-11-01&to=2026-11-30")]
    [InlineData("GET", "/api/expenses?from=2026-11-01&to=2026-11-30")]
    [InlineData("PUT", "/api/events/00000000-0000-0000-0000-000000000000")]
    [InlineData("PUT", "/api/expenses/00000000-0000-0000-0000-000000000000")]
    [InlineData("DELETE", "/api/events/00000000-0000-0000-0000-000000000000")]
    [InlineData("DELETE", "/api/expenses/00000000-0000-0000-0000-000000000000")]
    public async Task Sin_token_devuelve_401(string method, string url)
    {
        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "PUT" ? JsonContent.Create(new { }) : null,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
