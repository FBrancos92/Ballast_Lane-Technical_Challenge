using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

/// <summary>
/// Task use cases. Field rules live in the TaskItem entity; this class owns the
/// application rules: every operation is scoped to the calling user.
/// </summary>
public sealed class TaskService(ITaskRepository tasks, TimeProvider timeProvider) : ITaskService
{
    private const string Resource = "Task";

    public async Task<IReadOnlyList<TaskDto>> ListAsync(
        Guid userId,
        TaskItemStatus? status,
        CancellationToken cancellationToken = default)
    {
        var items = await tasks.ListByUserAsync(userId, status, cancellationToken);
        return items.Select(TaskDto.FromEntity).ToList();
    }

    public async Task<TaskDto> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedAsync(userId, taskId, cancellationToken);
        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> CreateAsync(
        Guid userId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = TaskItem.Create(
            userId,
            request.Title,
            request.Description,
            request.Status ?? TaskItemStatus.Todo,
            request.DueDate,
            timeProvider.GetUtcNow(),
            request.Priority ?? TaskPriority.Medium);

        await tasks.AddAsync(task, cancellationToken);
        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> UpdateAsync(
        Guid userId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await GetOwnedAsync(userId, taskId, cancellationToken);
        task.Update(request.Title, request.Description, request.Status, request.DueDate, timeProvider.GetUtcNow(), request.Priority);

        if (!await tasks.UpdateAsync(task, cancellationToken))
        {
            // Deleted between our read and our write.
            throw new NotFoundException(Resource, taskId);
        }

        return TaskDto.FromEntity(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        await GetOwnedAsync(userId, taskId, cancellationToken);
        if (!await tasks.DeleteAsync(taskId, cancellationToken))
        {
            throw new NotFoundException(Resource, taskId);
        }
    }

    /// <summary>
    /// Another user's task is reported as "not found", not "forbidden",
    /// so callers cannot probe which task ids exist.
    /// </summary>
    private async Task<TaskItem> GetOwnedAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.UserId != userId)
        {
            throw new NotFoundException(Resource, taskId);
        }

        return task;
    }
}
