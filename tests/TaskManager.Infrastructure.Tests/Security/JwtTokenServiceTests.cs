using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests.Security;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Settings = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "a-test-signing-key-that-is-at-least-32-bytes",
        ExpiryMinutes = 30,
    };

    private readonly User _user = User.Create("jane", "jane@example.com", "hash", DateTimeOffset.UtcNow);
    private readonly JwtTokenService _sut = new(Options.Create(Settings), TimeProvider.System);

    private static TokenValidationParameters ValidationParameters(string signingKey) => new()
    {
        ValidIssuer = Settings.Issuer,
        ValidAudience = Settings.Audience,
        IssuerSigningKey = new JwtOptions { SigningKey = signingKey }.GetSecurityKey(),
    };

    [Fact]
    public async Task Token_is_valid_and_carries_the_user_claims()
    {
        var token = _sut.CreateToken(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, ValidationParameters(Settings.SigningKey));

        result.IsValid.Should().BeTrue();
        result.Claims[JwtRegisteredClaimNames.Sub].Should().Be(_user.Id.ToString());
        result.Claims[JwtRegisteredClaimNames.UniqueName].Should().Be("jane");
        result.Claims[JwtRegisteredClaimNames.Email].Should().Be("jane@example.com");
    }

    [Fact]
    public void Token_expires_after_the_configured_minutes()
    {
        var before = DateTimeOffset.UtcNow;

        var token = _sut.CreateToken(_user);

        token.ExpiresAt.Should().BeCloseTo(before.AddMinutes(Settings.ExpiryMinutes), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Token_signed_with_another_key_is_rejected()
    {
        var token = _sut.CreateToken(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            token.Token,
            ValidationParameters("a-completely-different-key-of-32-bytes!!"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Token_does_not_contain_the_password_hash()
    {
        var token = new JsonWebToken(_sut.CreateToken(_user).Token);

        token.Claims.Select(c => c.Value).Should().NotContain(_user.PasswordHash);
    }
}
