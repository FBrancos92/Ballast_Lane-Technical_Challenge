using System.Text.RegularExpressions;
using TaskManager.Domain.Common;

namespace TaskManager.Domain.Users;

public sealed partial class User
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 50;
    public const int EmailMaxLength = 254;

    private User(Guid id, string username, string email, string passwordHash, DateTimeOffset createdAt)
    {
        Id = id;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string Username { get; }
    public string Email { get; }

    /// <summary>Only ever a hash produced by an IPasswordHasher. The raw password never reaches the domain.</summary>
    public string PasswordHash { get; }

    public DateTimeOffset CreatedAt { get; }

    public static User Create(string? username, string? email, string passwordHash, DateTimeOffset now)
    {
        var errors = new ValidationErrors();

        var cleanUsername = username?.Trim() ?? string.Empty;
        if (cleanUsername.Length is < UsernameMinLength or > UsernameMaxLength)
        {
            errors.Add(nameof(Username), $"Username must be between {UsernameMinLength} and {UsernameMaxLength} characters.");
        }
        else if (!UsernamePattern().IsMatch(cleanUsername))
        {
            errors.Add(nameof(Username), "Username may only contain letters, digits, '.', '_' and '-'.");
        }

        var cleanEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (cleanEmail.Length == 0)
        {
            errors.Add(nameof(Email), "Email is required.");
        }
        else if (cleanEmail.Length > EmailMaxLength || !EmailPattern().IsMatch(cleanEmail))
        {
            errors.Add(nameof(Email), "Email is not valid.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            errors.Add(nameof(PasswordHash), "Password hash is required.");
        }

        errors.ThrowIfAny();
        return new User(Guid.NewGuid(), cleanUsername, cleanEmail, passwordHash, now);
    }

    public static User Rehydrate(Guid id, string username, string email, string passwordHash, DateTimeOffset createdAt) =>
        new(id, username, email, passwordHash, createdAt);

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex UsernamePattern();

    // Deliberately simple: real verification is "send a confirmation email", not a perfect regex.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
