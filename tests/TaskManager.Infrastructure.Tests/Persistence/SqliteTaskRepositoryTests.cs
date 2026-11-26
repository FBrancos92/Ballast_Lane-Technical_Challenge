using FluentAssertions;
using Microsoft.Data.Sqlite;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests.Persistence;

public sealed class SqliteTaskRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly SqliteTestDatabase _db = new();
    private readonly SqliteTaskRepository _sut;
    private readonly User _owner = User.Create("owner", "owner@example.com", "hash", Now);
    private readonly User _other = User.Create("other", "other@example.com", "hash", Now);

    public SqliteTaskRepositoryTests()
    {
        _sut = new SqliteTaskRepository(_db.ConnectionFactory);
    }

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        var users = new SqliteUserRepository(_db.ConnectionFactory);
        await users.AddAsync(_owner);
        await users.AddAsync(_other);
    }

    public Task DisposeAsync() => _db.DisposeAsync();

    private TaskItem NewTask(
        string title = "Task",
        TaskItemStatus status = TaskItemStatus.Todo,
        DateOnly? due = null,
        User? owner = null,
        DateTimeOffset? createdAt = null) =>
        TaskItem.Create((owner ?? _owner).Id, title, "Some details", status, due, createdAt ?? Now);

    [Fact]
    public async Task Add_then_GetById_round_trips_every_field()
    {
        var task = NewTask(status: TaskItemStatus.InProgress, due: Today.AddDays(3));

        await _sut.AddAsync(task);
        var loaded = await _sut.GetByIdAsync(task.Id);

        loaded.Should().BeEquivalentTo(task);
    }

    [Fact]
    public async Task Null_description_and_due_date_round_trip_as_null()
    {
        var task = TaskItem.Create(_owner.Id, "Bare", null, TaskItemStatus.Todo, null, Now);

        await _sut.AddAsync(task);
        var loaded = await _sut.GetByIdAsync(task.Id);

        loaded!.Description.Should().BeNull();
        loaded.DueDate.Should().BeNull();
    }

    [Fact]
    public async Task Ids_and_dates_are_stored_as_readable_text()
    {
        var task = NewTask(due: Today.AddDays(1));
        await _sut.AddAsync(task);

        (await _db.ScalarAsync("SELECT typeof(Id) FROM Tasks")).Should().Be("text");
        (await _db.ScalarAsync("SELECT Id FROM Tasks")).Should().Be(task.Id.ToString());
        (await _db.ScalarAsync("SELECT DueDate FROM Tasks")).Should().Be("2026-06-16");
        (await _db.ScalarAsync("SELECT Status FROM Tasks")).Should().Be("Todo");
    }

    [Fact]
    public async Task GetById_returns_null_when_missing()
    {
        (await _sut.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task ListByUser_returns_only_that_users_tasks()
    {
        var mine = NewTask("Mine");
        await _sut.AddAsync(mine);
        await _sut.AddAsync(NewTask("Theirs", owner: _other));

        var result = await _sut.ListByUserAsync(_owner.Id);

        result.Should().ContainSingle().Which.Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task ListByUser_orders_by_due_date_with_undated_tasks_last()
    {
        await _sut.AddAsync(NewTask("No date", createdAt: Now));
        await _sut.AddAsync(NewTask("Later", due: Today.AddDays(9)));
        await _sut.AddAsync(NewTask("Sooner", due: Today.AddDays(1)));
        await _sut.AddAsync(NewTask("No date, newer", createdAt: Now.AddMinutes(5)));

        var result = await _sut.ListByUserAsync(_owner.Id);

        result.Select(t => t.Title).Should().ContainInOrder("Sooner", "Later", "No date", "No date, newer");
    }

    [Fact]
    public async Task ListByUser_filters_by_status()
    {
        await _sut.AddAsync(NewTask("Open"));
        await _sut.AddAsync(NewTask("Finished", status: TaskItemStatus.Done));

        var done = await _sut.ListByUserAsync(_owner.Id, TaskItemStatus.Done);

        done.Should().ContainSingle().Which.Title.Should().Be("Finished");
    }

    [Fact]
    public async Task Update_persists_changes_and_returns_true()
    {
        var task = NewTask();
        await _sut.AddAsync(task);
        task.Update("Renamed", null, TaskItemStatus.Done, Today.AddDays(2), Now.AddHours(1));

        var updated = await _sut.UpdateAsync(task);
        var loaded = await _sut.GetByIdAsync(task.Id);

        updated.Should().BeTrue();
        loaded.Should().BeEquivalentTo(task);
    }

    [Fact]
    public async Task Update_of_missing_task_returns_false()
    {
        (await _sut.UpdateAsync(NewTask())).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_removes_the_task_and_returns_true()
    {
        var task = NewTask();
        await _sut.AddAsync(task);

        (await _sut.DeleteAsync(task.Id)).Should().BeTrue();
        (await _sut.GetByIdAsync(task.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_of_missing_task_returns_false()
    {
        (await _sut.DeleteAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task Add_for_unknown_user_is_rejected_by_the_foreign_key()
    {
        var orphan = TaskItem.Create(Guid.NewGuid(), "Orphan", null, TaskItemStatus.Todo, null, Now);

        var act = () => _sut.AddAsync(orphan);

        await act.Should().ThrowAsync<SqliteException>();
    }

    [Fact]
    public async Task Initializer_is_idempotent()
    {
        await _sut.AddAsync(NewTask());

        await new DatabaseInitializer(_db.ConnectionFactory).InitializeAsync();

        (await _db.CountAsync("Tasks")).Should().Be(1);
    }

    [Fact]
    public async Task Priority_is_saved_and_loaded()
    {
        var task = TaskItem.Create(_owner.Id, "Urgent", null, TaskItemStatus.Todo, null, Now, TaskPriority.High);

        await _sut.AddAsync(task);
        var loaded = await _sut.GetByIdAsync(task.Id);

        loaded!.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task Update_saves_a_changed_priority()
    {
        var task = TaskItem.Create(_owner.Id, "Task", null, TaskItemStatus.Todo, null, Now, TaskPriority.Low);
        await _sut.AddAsync(task);
        task.Update("Task", null, TaskItemStatus.Todo, null, Now, TaskPriority.High);

        await _sut.UpdateAsync(task);
        var loaded = await _sut.GetByIdAsync(task.Id);

        loaded!.Priority.Should().Be(TaskPriority.High);
    }

    // ---------- Bulk ----------

    [Fact]
    public async Task GetByIds_returns_the_existing_tasks_and_skips_unknown_ids()
    {
        var first = NewTask("First");
        var second = NewTask("Second", owner: _other);
        var notAsked = NewTask("Not asked");
        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        await _sut.AddAsync(notAsked);

        var loaded = await _sut.GetByIdsAsync([first.Id, second.Id, Guid.NewGuid()]);

        loaded.Select(t => t.Id).Should().BeEquivalentTo([first.Id, second.Id]);
        loaded.Single(t => t.Id == first.Id).Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task GetByIds_with_no_ids_returns_nothing()
    {
        await _sut.AddAsync(NewTask());

        var loaded = await _sut.GetByIdsAsync([]);

        loaded.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMany_saves_every_task()
    {
        var first = NewTask("First");
        var second = NewTask("Second");
        await _sut.AddAsync(first);
        await _sut.AddAsync(second);
        first.ChangeStatus(TaskItemStatus.Done, Now.AddHours(1));
        second.ChangeStatus(TaskItemStatus.Done, Now.AddHours(1));

        var saved = await _sut.UpdateManyAsync([first, second]);

        saved.Should().BeTrue();
        (await _sut.GetByIdAsync(first.Id)).Should().BeEquivalentTo(first);
        (await _sut.GetByIdAsync(second.Id)).Should().BeEquivalentTo(second);
    }

    [Fact]
    public async Task UpdateMany_with_a_missing_task_rolls_back_and_saves_nothing()
    {
        var stored = NewTask("Stored");
        var neverSaved = NewTask("Never saved");
        await _sut.AddAsync(stored);
        stored.ChangeStatus(TaskItemStatus.Done, Now.AddHours(1));
        neverSaved.ChangeStatus(TaskItemStatus.Done, Now.AddHours(1));

        var saved = await _sut.UpdateManyAsync([stored, neverSaved]);

        saved.Should().BeFalse();
        (await _sut.GetByIdAsync(stored.Id))!.Status.Should().Be(TaskItemStatus.Todo);
    }

    [Fact]
    public async Task Initializer_adds_the_priority_column_to_an_older_database()
    {
        // Rebuild the Tasks table the way it looked before priorities existed, with one task in it.
        await _db.ScalarAsync("DROP TABLE Tasks;");
        await _db.ScalarAsync("""
            CREATE TABLE Tasks (
                Id TEXT NOT NULL PRIMARY KEY, UserId TEXT NOT NULL, Title TEXT NOT NULL, Description TEXT NULL,
                Status TEXT NOT NULL, DueDate TEXT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);
            """);
        var id = Guid.NewGuid();
        await _db.ScalarAsync($"""
            INSERT INTO Tasks VALUES ('{id}', '{_owner.Id}', 'Old task', NULL, 'Todo', NULL,
                '2026-06-15T12:00:00.0000000+00:00', '2026-06-15T12:00:00.0000000+00:00');
            """);

        await new DatabaseInitializer(_db.ConnectionFactory).InitializeAsync();

        (await _db.ScalarAsync("SELECT COUNT(*) FROM pragma_table_info('Tasks') WHERE name = 'Priority';"))
            .Should().Be("1");
        (await _sut.GetByIdAsync(id))!.Priority.Should().Be(TaskPriority.Medium);
    }
}
