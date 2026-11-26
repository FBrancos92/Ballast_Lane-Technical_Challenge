using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskManager.Api.Tests.Controllers;

internal static class ControllerTestHelpers
{
    /// <summary>Simulates what the JWT middleware does: attaches a principal with a "sub" claim.</summary>
    public static T WithUser<T>(this T controller, Guid userId)
        where T : ControllerBase
    {
        var identity = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }
}
