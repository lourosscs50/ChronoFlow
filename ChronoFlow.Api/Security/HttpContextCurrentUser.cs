using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChronoFlow.Api.Security;
public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public string? UserId =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? Principal?.FindFirstValue("uid");

    public string? Email =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);
}

    
