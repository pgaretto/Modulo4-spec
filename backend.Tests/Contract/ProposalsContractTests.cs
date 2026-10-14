using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Domain;
using SmartAgenda.Api.Extraction;
using SmartAgenda.Tests.Integration;

namespace SmartAgenda.Tests.Contract;

/// <summary>POST /api/proposals según contracts/openapi.yaml (FR-001, FR-011, FR-012).</summary>
public class ProposalsContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProposalsContractTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Ai.Reset();
    }

    private static object Request(string text = "Me tomé un café 2500") => new { text, today = "2026-10-14" };

    [Fact]
    public async Task Devuelve_200_con_la_propuesta_de_gasto()
    {
        _factory.Ai.Output = new ModelOutput([], [new ModelExpense(2500, "Comida", null)], []);

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("event").ValueKind);
        var expense = body.GetProperty("expense");
        Assert.Equal(2500, expense.GetProperty("amount").GetDecimal());
        Assert.Equal(Category.System[0].Id, expense.GetProperty("categoryId").GetGuid());
        Assert.Equal("2026-10-14", expense.GetProperty("date").GetString());
        Assert.Equal(0, body.GetProperty("warnings").GetArrayLength());
    }

    [Fact]
    public async Task Le_pasa_a_la_IA_el_texto_y_la_fecha_de_hoy_del_usuario()
    {
        _factory.Ai.Output = new ModelOutput([], [new ModelExpense(1, "Comida", null)], []);

        await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request("mañana almuerzo 15000"));

        Assert.Equal(("mañana almuerzo 15000", new DateOnly(2026, 10, 14)), _factory.Ai.Calls.Single());
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/proposals", Request());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_factory.Ai.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Texto_vacio_devuelve_400_y_no_llama_a_la_IA(string text)
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request(text));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Ai.Calls);
    }

    [Fact]
    public async Task Texto_de_mas_de_500_caracteres_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request(new string('a', 501)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sin_fecha_de_hoy_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", new { text = "café 2500" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sin_evento_ni_gasto_devuelve_422()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request("hola, ¿cómo andás?"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NOTHING_RECOGNIZED", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Si_la_IA_falla_devuelve_504()
    {
        _factory.Ai.Failure = new ExtractionUnavailableException("timeout");

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/proposals", Request());

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }
}
