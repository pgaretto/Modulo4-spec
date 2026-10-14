using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Tests.Integration;

/// <summary>Confirmar evento y gasto: se guardan juntos, a nombre del token, o no se guarda nada (FR-007, FR-010).</summary>
public class EntriesEventIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EntriesEventIsolationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Guarda_evento_y_gasto_a_nombre_de_Fede_y_Sofi_no_los_ve()
    {
        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new
        {
            userId = "sofi",
            @event = new { title = "Dentista Fede", date = "2026-10-20", time = "16:00" },
            expense = new { amount = 25001, categoryId = Category.System[3].Id, date = "2026-10-20" },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var (eventOwners, expenseOwners) = await _factory.WithDbAsync(async db => (
            await db.Events.IgnoreQueryFilters().Where(e => e.Title == "Dentista Fede").Select(e => e.UserId).ToListAsync(),
            await db.Expenses.IgnoreQueryFilters().Where(e => e.Amount == 25001).Select(e => e.UserId).ToListAsync()));
        Assert.Equal(["fede"], eventOwners);
        Assert.Equal(["fede"], expenseOwners);
    }

    [Fact]
    public async Task Si_el_gasto_es_invalido_no_se_guarda_ni_el_evento()
    {
        var before = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "Evento huérfano", date = "2026-10-20", time = (string?)null },
            expense = new { amount = -5, categoryId = Category.System[0].Id, date = "2026-10-20" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await _factory.CountRowsAsync());
    }

    [Fact]
    public async Task Si_el_evento_es_invalido_no_se_guarda_ni_el_gasto()
    {
        var before = await _factory.CountRowsAsync();

        var response = await _factory.CreateClientFor("fede").PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "", date = "2026-10-20", time = (string?)null },
            expense = new { amount = 100, categoryId = Category.System[0].Id, date = "2026-10-20" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await _factory.CountRowsAsync());
    }
}
