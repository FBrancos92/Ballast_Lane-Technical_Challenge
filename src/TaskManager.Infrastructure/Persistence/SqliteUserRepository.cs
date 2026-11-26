using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Users;

namespace TaskManager.Infrastructure.Persistence;

public sealed class SqliteUserRepository(SqliteConnectionFactory connectionFactory) : IUserRepository
{
    private const int SqliteConstraintError = 19;
    private const string SelectColumns = "SELECT Id, Username, Email, PasswordHash, CreatedAt FROM Users";

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        QuerySingleAsync($"{SelectColumns} WHERE Id = $value;", SqliteValues.ToDb(id), cancellationToken);

    // The Username column is COLLATE NOCASE, so this match is case-insensitive.
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        QuerySingleAsync($"{SelectColumns} WHERE Username = $value;", username, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM Users WHERE Email = $email);";
        command.With("$email", email.Trim().ToLowerInvariant());

        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Id, Username, Email, PasswordHash, CreatedAt)
            VALUES ($id, $username, $email, $passwordHash, $createdAt);
            """;
        command
            .With("$id", SqliteValues.ToDb(user.Id))
            .With("$username", user.Username)
            .With("$email", user.Email)
            .With("$passwordHash", user.PasswordHash)
            .With("$createdAt", SqliteValues.ToDb(user.CreatedAt));

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteConstraintError)
        {
            // Two registrations raced past the service's uniqueness check; the UNIQUE index is the real guard.
            throw new ConflictException("That username or email is already registered.");
        }
    }

    private async Task<User?> QuerySingleAsync(string sql, string value, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.With("$value", value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private static User Map(SqliteDataReader reader) => User.Rehydrate(
        id: reader.ReadGuid(0),
        username: reader.GetString(1),
        email: reader.GetString(2),
        passwordHash: reader.GetString(3),
        createdAt: reader.ReadDateTimeOffset(4));
}
