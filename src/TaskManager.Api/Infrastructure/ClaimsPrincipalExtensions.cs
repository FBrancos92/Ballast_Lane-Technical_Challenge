using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManager.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The caller's user id, taken from the token's "sub" claim. The id is never accepted
    /// from the URL or the request body, so a user cannot act as someone else.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated principal has no valid 'sub' claim.");
    }
}
