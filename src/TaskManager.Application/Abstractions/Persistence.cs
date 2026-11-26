using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Abstractions;

/// <summary>
/// Ports the business layer needs from storage. Defined here, implemented in Infrastructure,
/// so the dependency points inward (Dependency Inversion).
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The tasks with these ids that exist, whoever owns them. Missing ids are left out.</summary>
    Task<IReadOnlyList<TaskItem>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Tasks for one user, soonest due first (tasks without a due date last).</summary>
    Task<IReadOnlyList<TaskItem>> ListByUserAsync(
        Guid userId,
        TaskItemStatus? status = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <returns>False if the task no longer exists.</returns>
    Task<bool> UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);

    /// <summary>Saves several tasks in one transaction.</summary>
    /// <returns>False, with nothing saved, if any of the tasks no longer exists.</returns>
    Task<bool> UpdateManyAsync(IReadOnlyCollection<TaskItem> tasks, CancellationToken cancellationToken = default);

    /// <returns>False if the task did not exist.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive lookup.</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
