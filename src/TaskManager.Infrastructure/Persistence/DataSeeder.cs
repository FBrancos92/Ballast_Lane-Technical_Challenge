using Microsoft.Extensions.Logging;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Seeds demo accounts and tasks the first time the app runs against an empty database.
/// Goes through the domain factories and repositories, so seeded data obeys the same rules as real data.
/// </summary>
public sealed class DataSeeder(
    IUserRepository users,
    ITaskRepository tasks,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<DataSeeder> logger)
{
    public const string DemoUsername = "demo";
    public const string DemoPassword = "Demo1234!";
    public const string SecondUsername = "alex";
    public const string SecondPassword = "Alex1234!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await users.GetByUsernameAsync(DemoUsername, cancellationToken) is not null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var demo = User.Create(DemoUsername, "demo@example.com", passwordHasher.Hash(DemoPassword), now);
        await users.AddAsync(demo, cancellationToken);

        // A mix of statuses and priorities, so every board column and every priority badge shows up in a demo.
        (string Title, string? Description, TaskItemStatus Status, TaskPriority Priority, DateOnly? Due)[] demoTasks =
        [
            ("Prepare interview presentation", "User story, architecture diagram, live demo.", TaskItemStatus.InProgress, TaskPriority.High, today.AddDays(1)),
            ("Practice the live demo", "Drag a card, change status in bulk, phone view.", TaskItemStatus.InProgress, TaskPriority.Medium, today.AddDays(1)),
            ("Review pull request #42", "Check the repository tests and SQL parameters.", TaskItemStatus.Todo, TaskPriority.High, today.AddDays(2)),
            ("Write README", "Setup steps, demo credentials, architecture notes.", TaskItemStatus.Done, TaskPriority.Medium, today),
            ("Book dentist appointment", null, TaskItemStatus.Todo, TaskPriority.Low, today.AddDays(10)),
            ("Read Clean Architecture, chapter 22", "The Clean Architecture chapter.", TaskItemStatus.Todo, TaskPriority.Low, null),
        ];

        foreach (var (title, description, status, priority, due) in demoTasks)
        {
            await tasks.AddAsync(TaskItem.Create(demo.Id, title, description, status, due, now, priority), cancellationToken);
        }

        // A second user proves that each user only ever sees their own tasks.
        var alex = User.Create(SecondUsername, "alex@example.com", passwordHasher.Hash(SecondPassword), now);
        await users.AddAsync(alex, cancellationToken);
        await tasks.AddAsync(
            TaskItem.Create(alex.Id, "Alex's private task", "Not visible to the demo user.", TaskItemStatus.Todo, today.AddDays(5), now),
            cancellationToken);

        logger.LogInformation("Seeded demo users '{Demo}' and '{Second}'.", DemoUsername, SecondUsername);
    }
}
