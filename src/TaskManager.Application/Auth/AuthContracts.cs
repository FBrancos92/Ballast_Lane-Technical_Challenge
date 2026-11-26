using TaskManager.Domain.Users;

namespace TaskManager.Application.Auth;

public sealed record RegisterRequest(string? Username, string? Email, string? Password);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record UserDto(Guid Id, string Username, string Email, DateTimeOffset CreatedAt)
{
    public static UserDto FromEntity(User user) => new(user.Id, user.Username, user.Email, user.CreatedAt);
}

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
