using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TaskManager.Api.Controllers;
using TaskManager.Application.Auth;

namespace TaskManager.Api.Tests.Controllers;

public class AuthAndUsersControllerTests
{
    private readonly IAuthService _service = Substitute.For<IAuthService>();
    private static readonly UserDto User = new(Guid.NewGuid(), "jane", "jane@example.com", DateTimeOffset.UtcNow);
    private static readonly AuthResponse Response = new("token", DateTimeOffset.UtcNow.AddHours(1), User);

    [Fact]
    public async Task Register_returns_201_with_the_token()
    {
        var request = new RegisterRequest("jane", "jane@example.com", "Passw0rd!");
        _service.RegisterAsync(request, Arg.Any<CancellationToken>()).Returns(Response);

        var result = await new AuthController(_service).Register(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.Me));
        created.ControllerName.Should().Be("Users");
        created.Value.Should().Be(Response);
    }

    [Fact]
    public async Task Login_returns_200_with_the_token()
    {
        var request = new LoginRequest("jane", "Passw0rd!");
        _service.LoginAsync(request, Arg.Any<CancellationToken>()).Returns(Response);

        var result = await new AuthController(_service).Login(request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Response);
    }

    [Fact]
    public async Task Me_returns_the_user_from_the_token()
    {
        _service.GetUserAsync(User.Id, Arg.Any<CancellationToken>()).Returns(User);

        var result = await new UsersController(_service).WithUser(User.Id).Me(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(User);
    }
}
