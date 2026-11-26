using TaskManager.Domain.Common;

namespace TaskManager.Application.Auth;

/// <summary>
/// Rules for raw passwords. These live in the application layer because the domain
/// only ever sees the hash.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static void Validate(string? password)
    {
        var errors = new ValidationErrors();
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
        {
            errors.Add("Password", $"Password must be at least {MinLength} characters.");
        }
        else if (password.Length > MaxLength)
        {
            errors.Add("Password", $"Password must be at most {MaxLength} characters.");
        }
        else if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            errors.Add("Password", "Password must contain at least one letter and one digit.");
        }

        errors.ThrowIfAny();
    }
}
