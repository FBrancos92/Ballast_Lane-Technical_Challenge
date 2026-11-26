using FluentAssertions;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Auth;
using TaskManager.Application.Common;
using TaskManager.Domain.Common;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Tests.Auth;

public class AuthServiceTests
{
    private const string ValidPassword = "Passw0rd!";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly FixedTimeProvider _time = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, _hasher, _tokens, _time);
        _hasher.Hash(Arg.Any<string>()).Returns(ci => $"hashed:{ci.Arg<string>()}");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => ci.ArgAt<string>(1) == $"hashed:{ci.ArgAt<string>(0)}");
        _tokens.CreateToken(Arg.Any<User>()).Returns(new AccessToken("jwt-token", _time.Now.AddHours(1)));
    }

    private User ExistingUser(string username = "jane") =>
        User.Create(username, $"{username}@example.com", $"hashed:{ValidPassword}", _time.Now);

    // ---------- Register ----------

    [Fact]
    public async Task Register_stores_a_hashed_password_and_returns_a_token()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("jane", "Jane@Example.com", ValidPassword));

        result.Token.Should().Be("jwt-token");
        result.User.Username.Should().Be("jane");
        result.User.Email.Should().Be("jane@example.com");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Username == "jane" && u.PasswordHash == $"hashed:{ValidPassword}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_never_stores_the_raw_password()
    {
        await _sut.RegisterAsync(new RegisterRequest("jane", "jane@example.com", ValidPassword));

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.PasswordHash != ValidPassword),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("1234567890")]
    public async Task Register_with_weak_password_fails(string? password)
    {
        var act = () => _sut.RegisterAsync(new RegisterRequest("jane", "jane@example.com", password));

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Errors.Should().ContainKey("Password");
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Register_with_taken_username_throws_conflict()
    {
        _users.GetByUsernameAsync("jane", Arg.Any<CancellationToken>()).Returns(ExistingUser());

        var act = () => _sut.RegisterAsync(new RegisterRequest("jane", "other@example.com", ValidPassword));

        await act.Should().ThrowAsync<ConflictException>();
        await _users.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Register_with_taken_email_throws_conflict()
    {
        _users.EmailExistsAsync("jane@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _sut.RegisterAsync(new RegisterRequest("newuser", " JANE@example.com ", ValidPassword));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_with_invalid_username_fails_validation()
    {
        var act = () => _sut.RegisterAsync(new RegisterRequest("a b", "jane@example.com", ValidPassword));

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Errors.Should().ContainKey("Username");
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_with_correct_credentials_returns_a_token()
    {
        var user = ExistingUser();
        _users.GetByUsernameAsync("jane", Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.LoginAsync(new LoginRequest(" jane ", ValidPassword));

        result.Token.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
        _tokens.Received(1).CreateToken(user);
    }

    [Fact]
    public async Task Login_with_wrong_password_throws_invalid_credentials()
    {
        _users.GetByUsernameAsync("jane", Arg.Any<CancellationToken>()).Returns(ExistingUser());

        var act = () => _sut.LoginAsync(new LoginRequest("jane", "WrongPass1"));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokens.DidNotReceiveWithAnyArgs().CreateToken(default!);
    }

    [Fact]
    public async Task Login_with_unknown_user_throws_the_same_invalid_credentials_error()
    {
        var act = () => _sut.LoginAsync(new LoginRequest("ghost", ValidPassword));

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Theory]
    [InlineData(null, ValidPassword, "Username")]
    [InlineData(" ", ValidPassword, "Username")]
    [InlineData("jane", "", "Password")]
    public async Task Login_with_missing_fields_fails_validation(string? username, string? password, string field)
    {
        var act = () => _sut.LoginAsync(new LoginRequest(username, password));

        (await act.Should().ThrowAsync<DomainValidationException>())
            .Which.Errors.Should().ContainKey(field);
    }

    // ---------- Current user ----------

    [Fact]
    public async Task GetUser_returns_the_user()
    {
        var user = ExistingUser();
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetUserAsync(user.Id);

        result.Should().Be(UserDto.FromEntity(user));
    }

    [Fact]
    public async Task GetUser_for_unknown_id_throws_not_found()
    {
        var act = () => _sut.GetUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
