using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartAgenda.Api.Domain;
using SmartAgenda.Tests.Integration;

namespace SmartAgenda.Tests.Contract;

/// <summary>GET y POST /api/categories (FR-004b, RNF-03): categorías del sistema más las propias, privadas por usuario.</summary>
public class CategoriesContractTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CategoriesContractTests(ApiFactory factory) => _factory = factory;

    private static async Task<List<JsonElement>> NamesOf(HttpClient client) =>
        (await client.GetFromJsonAsync<List<JsonElement>>("/api/categories"))!;

    [Fact]
    public async Task Lista_las_cinco_categorias_del_sistema()
    {
        // Un usuario sin categorías propias: solo ve las del sistema.
        var categories = await NamesOf(_factory.CreateClientFor("usuario-sin-categorias"));

        Assert.Equal(["Comida", "Otros", "Salidas", "Salud", "Transporte"],
            categories.Select(c => c.GetProperty("name").GetString()).Order());
        Assert.All(categories, c => Assert.True(c.GetProperty("isSystem").GetBoolean()));
    }

    [Fact]
    public async Task Crea_una_categoria_propia_y_la_lista()
    {
        var fede = _factory.CreateClientFor("fede");

        var response = await fede.PostAsJsonAsync("/api/categories", new { name = "  Mascotas  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mascotas", created.GetProperty("name").GetString());
        Assert.False(created.GetProperty("isSystem").GetBoolean());
        Assert.Contains(await NamesOf(fede), c => c.GetProperty("name").GetString() == "Mascotas");
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/categories", new { name = "X" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_nombre_vacio_devuelve_400(string name)
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/categories", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Un_nombre_de_mas_de_40_caracteres_devuelve_400()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/categories", new { name = new string('a', 41) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Un_nombre_de_40_caracteres_se_acepta()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/categories", new { name = new string('b', 40) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Libros", "libros")]
    [InlineData("Plantas", "PLANTAS")]
    public async Task Un_nombre_repetido_sin_distinguir_mayusculas_devuelve_409(string first, string repeated)
    {
        var fede = _factory.CreateClientFor("fede");
        await fede.PostAsJsonAsync("/api/categories", new { name = first });

        var response = await fede.PostAsJsonAsync("/api/categories", new { name = repeated });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("comida")]
    [InlineData("OTROS")]
    public async Task No_se_puede_llamar_como_una_del_sistema(string name)
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/categories", new { name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Las_categorias_propias_son_privadas_y_otro_usuario_puede_usar_el_mismo_nombre()
    {
        var fede = _factory.CreateClientFor("fede");
        var sofi = _factory.CreateClientFor("sofi");
        await fede.PostAsJsonAsync("/api/categories", new { name = "Jardín" });

        Assert.DoesNotContain(await NamesOf(sofi), c => c.GetProperty("name").GetString() == "Jardín");
        Assert.Equal(HttpStatusCode.Created, (await sofi.PostAsJsonAsync("/api/categories", new { name = "Jardín" })).StatusCode);
    }

    [Fact]
    public async Task Un_gasto_con_categoria_propia_solo_se_acepta_para_su_dueno()
    {
        var fede = _factory.CreateClientFor("fede");
        var created = await (await fede.PostAsJsonAsync("/api/categories", new { name = "Regalos" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var categoryId = created.GetProperty("id").GetGuid();
        var body = new { expense = new { amount = 1200, categoryId, date = "2026-10-14" } };

        var deFede = await fede.PostAsJsonAsync("/api/entries", body);
        var deSofi = await _factory.CreateClientFor("sofi").PostAsJsonAsync("/api/entries", body);

        Assert.Equal(HttpStatusCode.Created, deFede.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, deSofi.StatusCode);
    }
}
