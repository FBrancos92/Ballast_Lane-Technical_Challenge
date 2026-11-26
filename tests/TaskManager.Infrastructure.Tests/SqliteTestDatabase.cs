using Microsoft.Data.Sqlite;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests;

/// <summary>
/// A real SQLite database in a temp file, created fresh for each test class instance.
/// Repository tests run real SQL: mocking the database would not prove the queries work.
/// </summary>
public sealed class SqliteTestDatabase : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"taskmanager-test-{Guid.NewGuid():N}.db");

    public SqliteTestDatabase()
    {
        // Pooling off so the file can be deleted as soon as the test finishes.
        ConnectionFactory = new SqliteConnectionFactory($"Data Source={_path};Pooling=False");
    }

    public SqliteConnectionFactory ConnectionFactory { get; }

    public Task InitializeAsync() => new DatabaseInitializer(ConnectionFactory).InitializeAsync();

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return Task.CompletedTask;
    }

    public async Task<long> CountAsync(string table)
    {
        await using var connection = await ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table};";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    public async Task<string?> ScalarAsync(string sql)
    {
        await using var connection = await ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
