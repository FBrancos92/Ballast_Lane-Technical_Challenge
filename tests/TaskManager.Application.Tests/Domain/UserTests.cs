using FluentAssertions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Tests.Domain;

public class UserTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    [Fact]
    public void Create_normalizes_username_and_email()
    {
        var user = User.Create("  Jane.Doe ", "  Jane@Example.COM ", "hash", Now);

        user.Id.Should().NotBeEmpty();
        user.Username.Should().Be("Jane.Doe");
        user.Email.Should().Be("jane@example.com");
        user.PasswordHash.Should().Be("hash");
        user.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    public void Create_with_invalid_username_fails(string? username)
    {
        var act = () => User.Create(username, "a@b.com", "hash", Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Username");
    }

    [Fact]
    public void Create_with_username_too_long_fails()
    {
        var act = () => User.Create(new string('a', User.UsernameMaxLength + 1), "a@b.com", "hash", Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Username");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    public void Create_with_invalid_email_fails(string? email)
    {
        var act = () => User.Create("jane", email, "hash", Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public void Create_without_password_hash_fails()
    {
        var act = () => User.Create("jane", "a@b.com", " ", Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("PasswordHash");
    }
}
