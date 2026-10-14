using System.Security.Claims;

namespace SmartAgenda.Api.Auth;

public interface ICurrentUser
{
    /// <summary>Identificador del usuario autenticado; nulo si no hay sesión.</summary>
    string? UserId { get; }
}

/// <summary>El usuario sale únicamente de los claims del token, nunca del cuerpo (RNF-03).</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        }
    }
}
