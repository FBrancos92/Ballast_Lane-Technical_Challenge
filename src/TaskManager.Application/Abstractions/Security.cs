using TaskManager.Domain.Users;

namespace TaskManager.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
