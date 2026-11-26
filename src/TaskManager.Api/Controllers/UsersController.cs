using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Infrastructure;
using TaskManager.Application.Auth;

namespace TaskManager.Api.Controllers;

/// <summary>Protected user endpoints: need a valid JWT.</summary>
[ApiController]
[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IAuthService authService) : ControllerBase
{
    /// <summary>Returns the profile of the signed-in user.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetUserAsync(User.GetUserId(), cancellationToken));
}
