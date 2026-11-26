namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Creates the schema if it does not exist. Without an ORM there are no migrations,
/// so the script is idempotent and safe to run on every start-up.
/// </summary>
public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    // Ids are stored as TEXT (canonical Guid strings) and dates as ISO-8601 TEXT,
    // so the data stays readable in any SQLite browser and sorts correctly as text.
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Users (
            Id           TEXT NOT NULL PRIMARY KEY,
            Username     TEXT NOT NULL COLLATE NOCASE UNIQUE,
            Email        TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            CreatedAt    TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Tasks (
            Id          TEXT NOT NULL PRIMARY KEY,
            UserId      TEXT NOT NULL REFERENCES Users (Id) ON DELETE CASCADE,
            Title       TEXT NOT NULL,
            Description TEXT NULL,
            Status      TEXT NOT NULL CHECK (Status IN ('Todo', 'InProgress', 'Done')),
            DueDate     TEXT NULL,
            CreatedAt   TEXT NOT NULL,
            UpdatedAt   TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Tasks_UserId_DueDate ON Tasks (UserId, DueDate);
        """;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = Schema;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
