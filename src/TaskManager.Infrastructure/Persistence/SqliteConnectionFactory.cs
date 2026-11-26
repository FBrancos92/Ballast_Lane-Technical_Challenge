using Microsoft.Data.Sqlite;

namespace TaskManager.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = "Data Source=taskmanager.db";
}

/// <summary>
/// Opens a fresh connection per unit of work. ADO.NET pools the underlying handles,
/// so this is cheap and avoids sharing a connection across threads.
/// </summary>
public sealed class SqliteConnectionFactory(string connectionString)
{
    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);

            // SQLite does not enforce foreign keys unless asked to, per connection.
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
