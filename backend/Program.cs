using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Auth;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Endpoints;
using SmartAgenda.Api.Extraction;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IExtractionService, AnthropicExtractionService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Autenticación: el token (Bearer) lo emite la feature de login; en desarrollo, `dotnet user-jwts create`.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization(options =>
{
    // Seguro por defecto: todo endpoint exige sesión válida (FR-012) salvo que se marque lo contrario.
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseApiErrorHandling();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapProposals();
app.MapEntries();
app.MapCategories();
app.MapEvents();
app.MapExpenses();

app.Run();

// Necesario para WebApplicationFactory<Program> en los tests de integración.
public partial class Program;
