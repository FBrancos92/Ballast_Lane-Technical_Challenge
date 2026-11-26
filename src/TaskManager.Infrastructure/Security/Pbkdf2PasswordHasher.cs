using System.Security.Cryptography;
using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Security;

/// <summary>
/// Salted PBKDF2-SHA256 using only the .NET base library.
/// Stored format: <c>PBKDF2-SHA256$iterations$salt$hash</c>, so the cost can be raised later
/// without breaking existing hashes.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // OWASP's 2023 recommendation for PBKDF2-HMAC-SHA256.
    public const int DefaultIterations = 600_000;

    private readonly int _iterations;

    public Pbkdf2PasswordHasher()
        : this(DefaultIterations)
    {
    }

    /// <summary>Lets tests use a low iteration count so they stay fast.</summary>
    public Pbkdf2PasswordHasher(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);
        _iterations = iterations;
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, HashSize);
        return string.Join('$', Algorithm, _iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations) || iterations < 1)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Constant-time comparison so response timing does not leak how many bytes matched.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
