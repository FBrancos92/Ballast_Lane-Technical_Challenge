using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Common;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PasswordPolicy.Validate(request.Password);

        // Check uniqueness before hashing: hashing is deliberately slow.
        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length > 0 && await users.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new ConflictException("That username is already taken.");
        }

        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length > 0 && await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("An account with that email already exists.");
        }

        var user = User.Create(username, email, passwordHasher.Hash(request.Password!), timeProvider.GetUtcNow());
        await users.AddAsync(user, cancellationToken);

        return IssueToken(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            var errors = new ValidationErrors();
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                errors.Add("Username", "Username is required.");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                errors.Add("Password", "Password is required.");
            }

            errors.ThrowIfAny();
        }

        var user = await users.GetByUsernameAsync(request.Username!.Trim(), cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password!, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return IssueToken(user);
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        return UserDto.FromEntity(user);
    }

    private AuthResponse IssueToken(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, UserDto.FromEntity(user));
    }
}
