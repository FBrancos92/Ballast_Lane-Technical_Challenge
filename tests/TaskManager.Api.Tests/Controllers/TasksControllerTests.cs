using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TaskManager.Api.Controllers;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;

namespace TaskManager.Api.Tests.Controllers;

/// <summary>
/// Unit tests: the controller in isolation with a mocked service. They check that the controller
/// passes the caller's id through and picks the right HTTP result. Routing, auth and JSON
/// are covered by the integration tests.
/// </summary>
public class TasksControllerTests
{
    private readonly ITaskService _service = Substitute.For<ITaskService>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly TasksController _sut;

    public TasksControllerTests()
    {
        _sut = new TasksController(_service).WithUser(_userId);
    }

    private static TaskDto SampleDto(Guid? id = null) => new(
        id ?? Guid.NewGuid(), "Title", null, TaskItemStatus.Todo, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TaskPriority.Medium);

    [Fact]
    public async Task List_passes_the_callers_id_and_filter_and_returns_200()
    {
        IReadOnlyList<TaskDto> tasks = [SampleDto()];
        _service.ListAsync(_userId, TaskItemStatus.Done, Arg.Any<CancellationToken>()).Returns(tasks);

        var result = await _sut.List(TaskItemStatus.Done, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(tasks);
    }

    [Fact]
    public async Task Get_returns_200_with_the_task()
    {
        var dto = SampleDto();
        _service.GetAsync(_userId, dto.Id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Get(dto.Id, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Create_returns_201_pointing_at_the_new_task()
    {
        var request = new CreateTaskRequest("Title", null, null, null);
        var dto = SampleDto();
        _service.CreateAsync(_userId, request, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(TasksController.Get));
        created.RouteValues!["id"].Should().Be(dto.Id);
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Update_returns_200_with_the_updated_task()
    {
        var dto = SampleDto();
        var request = new UpdateTaskRequest("New", null, TaskItemStatus.Done, null);
        _service.UpdateAsync(_userId, dto.Id, request, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Update(dto.Id, request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Delete_returns_204_after_deleting_as_the_caller()
    {
        var id = Guid.NewGuid();

        var result = await _sut.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await _service.Received(1).DeleteAsync(_userId, id, Arg.Any<CancellationToken>());
    }
}
