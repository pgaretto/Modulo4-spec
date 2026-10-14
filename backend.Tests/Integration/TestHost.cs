using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Extraction;
using SmartAgenda.Tests.Fakes;

namespace SmartAgenda.Tests.Integration;

/// <summary>API en memoria con base EF en memoria, autenticación de prueba y la IA simulada.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public FakeExtractionService Ai { get; } = new();

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

    /// <summary>Cliente autenticado como el usuario indicado ("fede", "sofi"...).</summary>
    public HttpClient CreateClientFor(string user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.Header, user);
        return client;
    }
}
