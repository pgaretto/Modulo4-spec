using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Extraction;
using SmartAgenda.Tests.Fakes;

namespace SmartAgenda.Tests.Integration;

/// <summary>API en memoria con base EF en memoria, autenticación de prueba y la IA simulada.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public FakeExtractionService Ai { get; } = new();

    /// <summary>Crea la base en memoria con las categorías del sistema (en producción las siembra la migración).</summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Sacar el proveedor de SQL Server y registrar el de memoria.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") == true
                        && d.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(AppDbContext)))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IExtractionService>();
            services.AddSingleton<IExtractionService>(Ai);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Filas de la base sin filtros por usuario: para comprobar lo que realmente quedó guardado.</summary>
    public async Task<T> WithDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task<(int Events, int Expenses)> CountRowsAsync() => WithDbAsync(async db =>
        (await db.Events.IgnoreQueryFilters().CountAsync(), await db.Expenses.IgnoreQueryFilters().CountAsync()));

    /// <summary>Cliente autenticado como el usuario indicado ("fede", "sofi"...).</summary>
    public HttpClient CreateClientFor(string user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.Header, user);
        return client;
    }
}
