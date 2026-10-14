using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Tests.Integration;

/// <summary>El dueño de lo guardado es siempre el usuario del token (FR-010, RNF-03, AC-10).</summary>
public class EntriesIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EntriesIsolationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Guarda_el_gasto_a_nombre_del_usuario_del_token_e_ignora_un_userId_del_cuerpo()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new
        {
            userId = "sofi",
            expense = new { userId = "sofi", amount = 2500, categoryId = Category.System[0].Id, date = "2026-10-14" },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var owners = await _factory.WithDbAsync(db =>
            db.Expenses.IgnoreQueryFilters().Where(e => e.Amount == 2500).Select(e => e.UserId).ToListAsync());
        Assert.Equal(["fede"], owners);
    }

    [Fact]
    public async Task Lo_que_guarda_Fede_no_es_visible_para_Sofi()
    {
        await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new
        {
            expense = new { amount = 777, categoryId = Category.System[1].Id, date = "2026-10-14" },
        });

        var visibleParaSofi = await _factory.WithDbAsync(async db =>
        {
            var all = await db.Expenses.IgnoreQueryFilters().Where(e => e.Amount == 777).ToListAsync();
            return all.Where(e => e.UserId == "sofi").ToList();
        });

        Assert.Empty(visibleParaSofi);
    }
}
