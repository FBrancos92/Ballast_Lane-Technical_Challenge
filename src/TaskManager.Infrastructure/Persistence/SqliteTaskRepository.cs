using Microsoft.Data.Sqlite;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Hand-written ADO.NET repository. Every value goes through a parameter, never string
/// concatenation, so SQL injection is not possible.
/// </summary>
public sealed class SqliteTaskRepository(SqliteConnectionFactory connectionFactory) : ITaskRepository
{
    private const string SelectColumns =
        "SELECT Id, UserId, Title, Description, Status, DueDate, CreatedAt, UpdatedAt FROM Tasks";

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.With("$id", SqliteValues.ToDb(id));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<TaskItem>> ListByUserAsync(
        Guid userId,
        TaskItemStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            {SelectColumns}
            WHERE UserId = $userId
              AND ($status IS NULL OR Status = $status)
            ORDER BY DueDate IS NULL, DueDate, CreatedAt;
            """;
        command.With("$userId", SqliteValues.ToDb(userId));
        command.With("$status", status is { } s ? s.ToString() : DBNull.Value);

        var results = new List<TaskItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(Map(reader));
        }

        return results;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Tasks (Id, UserId, Title, Description, Status, DueDate, CreatedAt, UpdatedAt)
            VALUES ($id, $userId, $title, $description, $status, $dueDate, $createdAt, $updatedAt);
            """;
        AddParameters(command, task);
        command.With("$userId", SqliteValues.ToDb(task.UserId));
        command.With("$createdAt", SqliteValues.ToDb(task.CreatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        // Owner and creation date are immutable, so they are not part of the UPDATE.
        command.CommandText = """
            UPDATE Tasks
            SET Title = $title,
                Description = $description,
                Status = $status,
                DueDate = $dueDate,
                UpdatedAt = $updatedAt
            WHERE Id = $id;
            """;
        AddParameters(command, task);

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tasks WHERE Id = $id;";
        command.With("$id", SqliteValues.ToDb(id));

        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static void AddParameters(SqliteCommand command, TaskItem task) =>
        command
            .With("$id", SqliteValues.ToDb(task.Id))
            .With("$title", task.Title)
            .With("$description", SqliteValues.ToDb(task.Description))
            .With("$status", task.Status.ToString())
            .With("$dueDate", SqliteValues.ToDb(task.DueDate))
            .With("$updatedAt", SqliteValues.ToDb(task.UpdatedAt));

    private static TaskItem Map(SqliteDataReader reader) => TaskItem.Rehydrate(
        id: reader.ReadGuid(0),
        userId: reader.ReadGuid(1),
        title: reader.GetString(2),
        description: reader.ReadNullableString(3),
        status: Enum.Parse<TaskItemStatus>(reader.GetString(4)),
        dueDate: reader.ReadNullableDateOnly(5),
        createdAt: reader.ReadDateTimeOffset(6),
        updatedAt: reader.ReadDateTimeOffset(7));
}
