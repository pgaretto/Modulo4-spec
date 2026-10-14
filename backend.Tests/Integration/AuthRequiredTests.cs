using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SmartAgenda.Tests.Integration;

/// <summary>Sin sesión válida ningún endpoint procesa ni guarda nada (FR-012, RNF-03).</summary>
public class AuthRequiredTests : IClassFixture<ApiFactory>
{
    private static readonly string[] HttpMethods = ["get", "post", "put", "delete"];

    private readonly ApiFactory _factory;

    public AuthRequiredTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Ai.Reset();
    }

    /// <summary>Lee (método, ruta) del contrato, para que un endpoint nuevo del contrato quede cubierto solo.</summary>
    public static IEnumerable<object[]> ContractEndpoints()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "specs", "001-texto-ia-confirmacion", "contracts", "openapi.yaml")))
        {
            dir = dir.Parent;
        }

        var lines = File.ReadAllLines(Path.Combine(dir!.FullName, "specs", "001-texto-ia-confirmacion", "contracts", "openapi.yaml"));
        string? path = null;
        foreach (var line in lines)
        {
            if (line.StartsWith("components:")) break;
            var pathMatch = Regex.Match(line, @"^  (/\S+):\s*$");
            if (pathMatch.Success) { path = pathMatch.Groups[1].Value; continue; }

            var methodMatch = Regex.Match(line, @"^    (get|post|put|delete):\s*$");
            if (methodMatch.Success && path is not null && HttpMethods.Contains(methodMatch.Groups[1].Value))
            {
                yield return [methodMatch.Groups[1].Value.ToUpperInvariant(), path];
            }
        }
    }

    [Fact]
    public void El_contrato_lista_todos_los_endpoints_esperados()
    {
        Assert.True(ContractEndpoints().Count() >= 10, "El contrato debería declarar al menos 10 operaciones.");
    }

    [Theory]
    [MemberData(nameof(ContractEndpoints))]
    public async Task Sin_token_devuelve_401_y_no_procesa_nada(string method, string path)
    {
        // Las rutas del contrato son relativas al servidor "/api"; los {id} se reemplazan por un id cualquiera.
        var url = "/api" + path.Replace("{id}", Guid.Empty.ToString()) + (method == "GET" ? "?from=2026-11-01&to=2026-11-30" : "");
        var before = await _factory.CountRowsAsync();
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { text = "café 2500", today = "2026-10-14" }) : null,
        };

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_factory.Ai.Calls);
        Assert.Equal(before, await _factory.CountRowsAsync());
    }

    [Fact]
    public void Ningun_endpoint_de_la_API_permite_acceso_anonimo()
    {
        var endpoints = _factory.Services.GetServices<EndpointDataSource>().SelectMany(s => s.Endpoints).ToList();

        Assert.NotEmpty(endpoints);
        Assert.DoesNotContain(endpoints, e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null);
    }
}
