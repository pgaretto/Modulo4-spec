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
}
