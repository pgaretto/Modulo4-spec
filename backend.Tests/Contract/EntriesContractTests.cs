using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Domain;
using SmartAgenda.Tests.Integration;

namespace SmartAgenda.Tests.Contract;

/// <summary>POST /api/entries según contracts/openapi.yaml (FR-007, FR-009).</summary>
public class EntriesContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EntriesContractTests(ApiFactory factory) => _factory = factory;

    private static object Expense(decimal amount = 2500, Guid? categoryId = null, string date = "2026-10-14") =>
        new { amount, categoryId = categoryId ?? Category.System[0].Id, date };

    [Fact]
    public async Task Guarda_un_gasto_y_devuelve_201_con_su_id()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new { expense = Expense() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var expense = body.GetProperty("expense");
        Assert.NotEqual(Guid.Empty, expense.GetProperty("id").GetGuid());
        Assert.Equal(2500, expense.GetProperty("amount").GetDecimal());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("event").ValueKind);
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/entries", new { expense = Expense() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sin_evento_ni_gasto_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Un_monto_no_positivo_devuelve_400_y_no_guarda(decimal amount)
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new { expense = Expense(amount) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("VALIDATION_ERROR", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Una_categoria_inexistente_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/entries", new { expense = Expense(categoryId: Guid.NewGuid()) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Evento (US2: FR-002, FR-009) ---

    private static object Event(string title = "Dentista", string date = "2026-10-20", string? time = "16:00") =>
        new { title, date, time };

    [Fact]
    public async Task Guarda_evento_y_gasto_y_devuelve_201_con_ambos()
    {
        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/entries", new { @event = Event(), expense = Expense(25000, Category.System[3].Id, "2026-10-20") });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ev = body.GetProperty("event");
        Assert.Equal("Dentista", ev.GetProperty("title").GetString());
        Assert.Equal("2026-10-20", ev.GetProperty("date").GetString());
        Assert.Equal("16:00", ev.GetProperty("time").GetString());
        Assert.NotEqual(Guid.Empty, ev.GetProperty("id").GetGuid());
        Assert.Equal(25000, body.GetProperty("expense").GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task Guarda_solo_un_evento_sin_hora_como_todo_el_dia()
    {
        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/entries", new { @event = Event("Cumple de Juan", time: null) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("expense").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("event").GetProperty("time").ValueKind);
    }

    [Theory]
    [InlineData("", "2026-10-20", "16:00")]
    [InlineData("   ", "2026-10-20", "16:00")]
    [InlineData("x", "0001-01-01", "16:00")]
    [InlineData("x", "2026-10-20", "4 pm")]
    [InlineData("x", "2026-10-20", "25:00")]
    public async Task Un_evento_invalido_devuelve_400(string title, string date, string? time)
    {
        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/entries", new { @event = Event(title, date, time) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Un_titulo_de_mas_de_120_caracteres_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede")
            .PostAsJsonAsync("/api/entries", new { @event = Event(new string('a', 121)) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
