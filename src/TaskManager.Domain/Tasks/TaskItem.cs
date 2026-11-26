using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

/// <summary>
/// A task owned by a single user. Named TaskItem to avoid clashing with System.Threading.Tasks.Task.
/// The entity protects its own invariants: it can only be created or changed through methods that validate.
/// </summary>
public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly? dueDate,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(
        Guid userId,
        string? title,
        string? description,
        TaskItemStatus status,
        DateOnly? dueDate,
        DateTimeOffset now)
    {
        var errors = new ValidationErrors();
        if (userId == Guid.Empty)
        {
            errors.Add(nameof(UserId), "A task must belong to a user.");
        }

        var (cleanTitle, cleanDescription) = ValidateFields(errors, title, description, status);
        if (dueDate is { } due && due < DateOnly.FromDateTime(now.UtcDateTime))
        {
            errors.Add(nameof(DueDate), "Due date cannot be in the past.");
        }

        errors.ThrowIfAny();
        return new TaskItem(Guid.NewGuid(), userId, cleanTitle, cleanDescription, status, dueDate, now, now);
    }

    public void Update(
        string? title,
        string? description,
        TaskItemStatus status,
        DateOnly? dueDate,
        DateTimeOffset now)
    {
        var errors = new ValidationErrors();
        var (cleanTitle, cleanDescription) = ValidateFields(errors, title, description, status);

        // An existing overdue date may be kept, but a *new* date cannot be set in the past.
        if (dueDate is { } due && dueDate != DueDate && due < DateOnly.FromDateTime(now.UtcDateTime))
        {
            errors.Add(nameof(DueDate), "Due date cannot be in the past.");
        }

        errors.ThrowIfAny();
        Title = cleanTitle;
        Description = cleanDescription;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = now;
    }

    /// <summary>Rebuilds an entity from storage. No validation: data was valid when it was saved.</summary>
    public static TaskItem Rehydrate(
        Guid id,
        Guid userId,
        string title,
        string? description,
        TaskItemStatus status,
        DateOnly? dueDate,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new(id, userId, title, description, status, dueDate, createdAt, updatedAt);

    private static (string Title, string? Description) ValidateFields(
        ValidationErrors errors,
        string? title,
        string? description,
        TaskItemStatus status)
    {
        var cleanTitle = title?.Trim() ?? string.Empty;
        if (cleanTitle.Length == 0)
        {
            errors.Add(nameof(Title), "Title is required.");
        }
        else if (cleanTitle.Length > TitleMaxLength)
        {
            errors.Add(nameof(Title), $"Title must be at most {TitleMaxLength} characters.");
        }

        var cleanDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (cleanDescription is { Length: > DescriptionMaxLength })
        {
            errors.Add(nameof(Description), $"Description must be at most {DescriptionMaxLength} characters.");
        }

        if (!Enum.IsDefined(status))
        {
            errors.Add(nameof(Status), "Status is not valid.");
        }

        return (cleanTitle, cleanDescription);
    }
}
