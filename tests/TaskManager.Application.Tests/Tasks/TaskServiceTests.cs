using FluentAssertions;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tests.Tasks;

public class TaskServiceTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly FixedTimeProvider _time = new();
    private readonly TaskService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _sut = new TaskService(_repository, _time);
        _repository.UpdateAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private TaskItem ExistingTask(Guid? ownerId = null, string title = "Existing") =>
        TaskItem.Create(ownerId ?? _userId, title, null, TaskItemStatus.Todo, null, _time.Now);

    private void Stored(TaskItem task) =>
        _repository.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

    // ---------- List ----------

    [Fact]
    public async Task List_returns_the_users_tasks_with_the_given_filter()
    {
        var task = ExistingTask();
        _repository.ListByUserAsync(_userId, TaskItemStatus.Todo, Arg.Any<CancellationToken>())
            .Returns([task]);

        var result = await _sut.ListAsync(_userId, TaskItemStatus.Todo);

        result.Should().ContainSingle().Which.Id.Should().Be(task.Id);
    }

    // ---------- Get ----------

    [Fact]
    public async Task Get_returns_an_owned_task()
    {
        var task = ExistingTask();
        Stored(task);

        var result = await _sut.GetAsync(_userId, task.Id);

        result.Should().BeEquivalentTo(TaskDto.FromEntity(task));
    }

    [Fact]
    public async Task Get_unknown_task_throws_not_found()
    {
        var act = () => _sut.GetAsync(_userId, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_another_users_task_throws_not_found()
    {
        var task = ExistingTask(ownerId: Guid.NewGuid());
        Stored(task);

        var act = () => _sut.GetAsync(_userId, task.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_saves_a_task_owned_by_the_caller()
    {
        var request = new CreateTaskRequest("Buy milk", "2 litres", TaskItemStatus.InProgress, _time.Today.AddDays(1));

        var result = await _sut.CreateAsync(_userId, request);

        result.Title.Should().Be("Buy milk");
        result.Status.Should().Be(TaskItemStatus.InProgress);
        result.CreatedAt.Should().Be(_time.Now);
        await _repository.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.UserId == _userId && t.Id == result.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_defaults_status_to_todo()
    {
        var result = await _sut.CreateAsync(_userId, new CreateTaskRequest("Title", null, null, null));

        result.Status.Should().Be(TaskItemStatus.Todo);
    }

    [Fact]
    public async Task Create_with_invalid_data_throws_and_saves_nothing()
    {
        var act = () => _sut.CreateAsync(_userId, new CreateTaskRequest("", null, null, _time.Today.AddDays(-1)));

        await act.Should().ThrowAsync<DomainValidationException>();
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

        [Fact]
    public async Task Create_passes_the_priority_to_the_task()
    {
        var request = new CreateTaskRequest("Title", null, null, null, TaskPriority.High);

        var result = await _sut.CreateAsync(_userId, request);

        result.Priority.Should().Be(TaskPriority.High);
        await _repository.Received(1).AddAsync(
            Arg.Is<TaskItem>(t => t.Priority == TaskPriority.High),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_without_priority_defaults_to_medium()
    {
        var result = await _sut.CreateAsync(_userId, new CreateTaskRequest("Title", null, null, null));

        result.Priority.Should().Be(TaskPriority.Medium);
    }
    // ---------- Update ----------

    [Fact]
    public async Task Update_changes_and_saves_an_owned_task()
    {
        var task = ExistingTask();
        Stored(task);
        _time.Now = _time.Now.AddHours(1);

        var result = await _sut.UpdateAsync(
            _userId,
            task.Id,
            new UpdateTaskRequest("Renamed", "Notes", TaskItemStatus.Done, null));

        result.Title.Should().Be("Renamed");
        result.Status.Should().Be(TaskItemStatus.Done);
        result.UpdatedAt.Should().Be(_time.Now);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<TaskItem>(t => t.Id == task.Id && t.Title == "Renamed"),
            Arg.Any<CancellationToken>());
    }
        
    [Fact]
    public async Task Update_changes_the_priority()
    {
        var task = ExistingTask();
        Stored(task);

        var result = await _sut.UpdateAsync(
            _userId,
            task.Id,
            new UpdateTaskRequest("Title", null, TaskItemStatus.Todo, null, TaskPriority.High));

        result.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task Update_another_users_task_throws_not_found_and_saves_nothing()
    {
        var task = ExistingTask(ownerId: Guid.NewGuid());
        Stored(task);

        var act = () => _sut.UpdateAsync(_userId, task.Id, new UpdateTaskRequest("Hijack", null, TaskItemStatus.Done, null));

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Update_with_invalid_data_throws_and_saves_nothing()
    {
        var task = ExistingTask();
        Stored(task);

        var act = () => _sut.UpdateAsync(_userId, task.Id, new UpdateTaskRequest(" ", null, TaskItemStatus.Done, null));

        await act.Should().ThrowAsync<DomainValidationException>();
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Update_when_task_deleted_concurrently_throws_not_found()
    {
        var task = ExistingTask();
        Stored(task);
        _repository.UpdateAsync(task, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.UpdateAsync(_userId, task.Id, new UpdateTaskRequest("Title", null, TaskItemStatus.Done, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_removes_an_owned_task()
    {
        var task = ExistingTask();
        Stored(task);

        await _sut.DeleteAsync(_userId, task.Id);

        await _repository.Received(1).DeleteAsync(task.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_another_users_task_throws_not_found_and_deletes_nothing()
    {
        var task = ExistingTask(ownerId: Guid.NewGuid());
        Stored(task);

        var act = () => _sut.DeleteAsync(_userId, task.Id);

        await act.Should().ThrowAsync<NotFoundException>();
        await _repository.DidNotReceiveWithAnyArgs().DeleteAsync(default, default);
    }

    [Fact]
    public async Task Delete_unknown_task_throws_not_found()
    {
        var act = () => _sut.DeleteAsync(_userId, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
