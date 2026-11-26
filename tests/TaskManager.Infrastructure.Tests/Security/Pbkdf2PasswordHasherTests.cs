using FluentAssertions;
using TaskManager.Infrastructure.Security;

namespace TaskManager.Infrastructure.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    // Low iteration count keeps the tests fast; the algorithm is the same.
    private readonly Pbkdf2PasswordHasher _sut = new(iterations: 1_000);

    [Fact]
    public void Hash_does_not_contain_the_password()
    {
        var hash = _sut.Hash("Passw0rd!");

        hash.Should().NotContain("Passw0rd!");
        hash.Should().StartWith("PBKDF2-SHA256$1000$");
    }

    [Fact]
    public void Verify_accepts_the_correct_password()
    {
        var hash = _sut.Hash("Passw0rd!");

        _sut.Verify("Passw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_rejects_a_wrong_password()
    {
        var hash = _sut.Hash("Passw0rd!");

        _sut.Verify("passw0rd!", hash).Should().BeFalse();
    }

    [Fact]
    public void Same_password_hashes_differently_each_time_because_of_the_salt()
    {
        _sut.Hash("Passw0rd!").Should().NotBe(_sut.Hash("Passw0rd!"));
    }

    [Fact]
    public void Verify_uses_the_iteration_count_stored_in_the_hash()
    {
        var oldHash = new Pbkdf2PasswordHasher(iterations: 500).Hash("Passw0rd!");

        _sut.Verify("Passw0rd!", oldHash).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("PBKDF2-SHA256$abc$salt$hash")]
    [InlineData("PBKDF2-SHA256$1000$not-base64!$not-base64!")]
    [InlineData("MD5$1000$c2FsdA==$aGFzaA==")]
    public void Verify_returns_false_for_malformed_hashes(string storedHash)
    {
        _sut.Verify("Passw0rd!", storedHash).Should().BeFalse();
    }
}
