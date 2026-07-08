using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Fuel.Orders.Service.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    }
}