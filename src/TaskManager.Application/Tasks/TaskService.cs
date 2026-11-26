using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Domain.Common;
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

    /// <summary>Upper bound on one bulk request, so a single call can't lock the table for long.</summary>
    public const int MaxTasksPerStatusUpdate = 100;

    public async Task<IReadOnlyList<TaskDto>> UpdateStatusAsync(
        Guid userId,
        UpdateTasksStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ids = request.Ids?.Distinct().ToList() ?? [];
        var errors = new ValidationErrors();
        if (ids.Count == 0)
        {
            errors.Add(nameof(request.Ids), "Select at least one task.");
        }
        else if (ids.Count > MaxTasksPerStatusUpdate)
        {
            errors.Add(nameof(request.Ids), $"You can update at most {MaxTasksPerStatusUpdate} tasks at once.");
        }

        if (!Enum.IsDefined(request.Status))
        {
            errors.Add(nameof(request.Status), "Status is not valid.");
        }

        errors.ThrowIfAny();

        var found = await tasks.GetByIdsAsync(ids, cancellationToken);

        // Same rule as single-task calls: someone else's task looks exactly like a missing one.
        var owned = found.Where(t => t.UserId == userId).ToDictionary(t => t.Id);
        // Nullable on purpose: Guid.Empty is a valid (missing) id here, not a "none found" marker.
        if (ids.Where(id => !owned.ContainsKey(id)).Select(id => (Guid?)id).FirstOrDefault() is { } missing)
        {
            throw new NotFoundException(Resource, missing);
        }

        var now = timeProvider.GetUtcNow();
        var changed = ids.Select(id => owned[id]).ToList();
        foreach (var task in changed)
        {
            task.ChangeStatus(request.Status, now);
        }

        if (!await tasks.UpdateManyAsync(changed, cancellationToken))
        {
            // One of them was deleted between our read and our write; the transaction saved nothing.
            throw new NotFoundException(Resource, string.Join(", ", ids));
        }

        return changed.Select(TaskDto.FromEntity).ToList();
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
