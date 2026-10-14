using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Domain;

namespace SmartAgenda.Tests.Integration;

/// <summary>Aislamiento entre usuarios a nivel del contexto de datos (RNF-03, Principio II).</summary>
public class DataIsolationTests
{
    private sealed class StubUser(string? id) : ICurrentUser
    {
        public string? UserId { get; set; } = id;
    }

    private static (AppDbContext Db, StubUser User) NewContext()
    {
        var user = new StubUser("fede");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new AppDbContext(options, user), user);
    }

    [Fact]
    public async Task Un_usuario_no_ve_los_eventos_ni_gastos_de_otro()
    {
        var (db, user) = NewContext();
        db.Events.Add(new Event { UserId = "fede", Title = "Dentista", Date = new DateOnly(2026, 10, 20) });
        db.Expenses.Add(new Expense { UserId = "fede", Amount = 2500, CategoryId = Category.System[0].Id, Date = new DateOnly(2026, 10, 20) });
        await db.SaveChangesAsync();

        user.UserId = "sofi";

        Assert.Empty(await db.Events.ToListAsync());
        Assert.Empty(await db.Expenses.ToListAsync());

        user.UserId = "fede";
        Assert.Single(await db.Events.ToListAsync());
        Assert.Single(await db.Expenses.ToListAsync());
    }

    [Fact]
    public async Task Las_categorias_del_sistema_se_ven_y_las_propias_son_privadas()
    {
        var (db, user) = NewContext();
        await db.Database.EnsureCreatedAsync();
        db.Categories.Add(new Category { UserId = "fede", Name = "Mascotas" });
        await db.SaveChangesAsync();

        user.UserId = "sofi";
        var delOtro = await db.Categories.Select(c => c.Name).ToListAsync();
        Assert.Equal(5, delOtro.Count);
        Assert.DoesNotContain("Mascotas", delOtro);

        user.UserId = "fede";
        Assert.Contains("Mascotas", await db.Categories.Select(c => c.Name).ToListAsync());
    }

    [Fact]
    public async Task No_se_puede_guardar_a_nombre_de_otro_usuario()
    {
        var (db, _) = NewContext();
        db.Events.Add(new Event { UserId = "sofi", Title = "Ajeno", Date = new DateOnly(2026, 10, 20) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task No_se_puede_guardar_sin_sesion()
    {
        var (db, user) = NewContext();
        user.UserId = null;
        db.Events.Add(new Event { UserId = "fede", Title = "Sin sesión", Date = new DateOnly(2026, 10, 20) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
