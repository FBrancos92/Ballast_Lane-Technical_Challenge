using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public sealed record CreateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus? Status,
    DateOnly? DueDate,
    TaskPriority? Priority = null);

/// <summary>PUT semantics: the whole editable state is replaced.</summary>
public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    TaskPriority? Priority = null);

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TaskPriority Priority)
{
    public static TaskDto FromEntity(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt,
        task.Priority);
}

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> ListAsync(Guid userId, TaskItemStatus? status, CancellationToken cancellationToken = default);

    Task<TaskDto> GetAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);

    Task<TaskDto> CreateAsync(Guid userId, CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<TaskDto> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken = default);
}
