using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Tasks;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Tests.Integration;

/// <summary>End-to-end through HTTP, auth, controllers, services, SQL and back.</summary>
public class TaskEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateOnly Tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    private Task<HttpClient> DemoClientAsync() =>
        factory.CreateAuthenticatedClientAsync(DataSeeder.DemoUsername, DataSeeder.DemoPassword);

    private Task<HttpClient> AlexClientAsync() =>
        factory.CreateAuthenticatedClientAsync(DataSeeder.SecondUsername, DataSeeder.SecondPassword);

    private static async Task<TaskDto> CreateAsync(HttpClient client, string title = "Integration task")
    {
        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest(title, "Created by a test", TaskItemStatus.Todo, Tomorrow),
            ApiFactory.Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json))!;
    }

    [Fact]
    public async Task Tasks_require_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Seeded_user_sees_their_seeded_tasks()
    {
        var client = await DemoClientAsync();

        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks", ApiFactory.Json);

        tasks.Should().Contain(t => t.Title == "Prepare interview presentation");
    }

    [Fact]
    public async Task Full_crud_lifecycle()
    {
        var client = await DemoClientAsync();

        // Create
        var create = await client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Lifecycle", "desc", null, Tomorrow),
            ApiFactory.Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await create.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json))!;
        create.Headers.Location!.AbsolutePath.Should().Be($"/api/tasks/{created.Id}");
        created.Status.Should().Be(TaskItemStatus.Todo);

        // Read
        var fetched = await client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiFactory.Json);
        fetched.Should().BeEquivalentTo(created);

        // Update
        var update = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Lifecycle (done)", null, TaskItemStatus.Done, Tomorrow),
            ApiFactory.Json);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json))!;
        updated.Title.Should().Be("Lifecycle (done)");
        updated.Status.Should().Be(TaskItemStatus.Done);
        updated.Description.Should().BeNull();

        // Filtered list
        var done = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?status=Done", ApiFactory.Json);
        done.Should().Contain(t => t.Id == created.Id).And.OnlyContain(t => t.Status == TaskItemStatus.Done);

        // Delete
        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/tasks/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_task_returns_400_with_every_field_error()
    {
        var client = await DemoClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("", null, null, new DateOnly(2000, 1, 1)),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFactory.Json);
        problem!.Errors.Keys.Should().BeEquivalentTo("title", "dueDate");
    }

    [Fact]
    public async Task Unknown_status_value_returns_400()
    {
        var client = await DemoClientAsync();
        using var body = new StringContent("""{ "title": "x", "status": "Archived" }""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/tasks", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Users_cannot_see_or_change_each_others_tasks()
    {
        var demo = await DemoClientAsync();
        var alex = await AlexClientAsync();
        var demoTask = await CreateAsync(demo, "Demo's secret");

        (await alex.GetAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alex.PutAsJsonAsync(
                $"/api/tasks/{demoTask.Id}",
                new UpdateTaskRequest("Hijacked", null, TaskItemStatus.Done, null),
                ApiFactory.Json))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alex.DeleteAsync($"/api/tasks/{demoTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var alexTasks = await alex.GetFromJsonAsync<List<TaskDto>>("/api/tasks", ApiFactory.Json);
        alexTasks.Should().NotContain(t => t.Id == demoTask.Id);

        var stillThere = await demo.GetFromJsonAsync<TaskDto>($"/api/tasks/{demoTask.Id}", ApiFactory.Json);
        stillThere!.Title.Should().Be("Demo's secret");
    }

    [Fact]
    public async Task Malformed_id_in_route_returns_404()
    {
        var client = await DemoClientAsync();

        (await client.GetAsync("/api/tasks/not-a-guid")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Priority_round_trips_through_the_api()
    {
        var client = await DemoClientAsync();

        var create = await client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Urgent fix", null, null, null, TaskPriority.High),
            ApiFactory.Json);
        var created = (await create.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json))!;
        created.Priority.Should().Be(TaskPriority.High);

        var update = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new UpdateTaskRequest("Urgent fix", null, TaskItemStatus.InProgress, null, TaskPriority.Low),
            ApiFactory.Json);
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var reloaded = await client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiFactory.Json);
        reloaded!.Priority.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public async Task Priority_is_sent_as_text_in_json()
    {
        var client = await DemoClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Wire format", null, null, null, TaskPriority.High),
            ApiFactory.Json);
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("\"priority\":\"High\"");
    }

    // ---------- Bulk status change ----------

    [Fact]
    public async Task Bulk_status_change_requires_authentication()
    {
        var response = await factory.CreateClient().PatchAsJsonAsync(
            "/api/tasks/status",
            new UpdateTasksStatusRequest([Guid.NewGuid()], TaskItemStatus.Done),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bulk_status_change_updates_every_selected_task()
    {
        var client = await DemoClientAsync();
        var first = await CreateAsync(client, "Bulk one");
        var second = await CreateAsync(client, "Bulk two");

        var response = await client.PatchAsJsonAsync(
            "/api/tasks/status",
            new UpdateTasksStatusRequest([first.Id, second.Id], TaskItemStatus.Done),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var changed = await response.Content.ReadFromJsonAsync<List<TaskDto>>(ApiFactory.Json);
        changed.Should().HaveCount(2).And.OnlyContain(t => t.Status == TaskItemStatus.Done);

        var reloaded = await client.GetFromJsonAsync<TaskDto>($"/api/tasks/{second.Id}", ApiFactory.Json);
        reloaded!.Status.Should().Be(TaskItemStatus.Done);
        reloaded.Title.Should().Be("Bulk two");
    }

    [Fact]
    public async Task Bulk_status_change_including_another_users_task_returns_404_and_changes_nothing()
    {
        var demo = await DemoClientAsync();
        var alex = await AlexClientAsync();
        var alexTask = await CreateAsync(alex, "Alex's own");
        var demoTask = await CreateAsync(demo, "Demo's secret");

        var response = await alex.PatchAsJsonAsync(
            "/api/tasks/status",
            new UpdateTasksStatusRequest([alexTask.Id, demoTask.Id], TaskItemStatus.Done),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alex.GetFromJsonAsync<TaskDto>($"/api/tasks/{alexTask.Id}", ApiFactory.Json))!
            .Status.Should().Be(TaskItemStatus.Todo);
        (await demo.GetFromJsonAsync<TaskDto>($"/api/tasks/{demoTask.Id}", ApiFactory.Json))!
            .Status.Should().Be(TaskItemStatus.Todo);
    }

    [Fact]
    public async Task Bulk_status_change_without_ids_returns_400()
    {
        var client = await DemoClientAsync();

        var response = await client.PatchAsJsonAsync(
            "/api/tasks/status",
            new UpdateTasksStatusRequest([], TaskItemStatus.Done),
            ApiFactory.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFactory.Json);
        problem!.Errors.Keys.Should().BeEquivalentTo("ids");
    }

    [Fact]
    public async Task Unknown_priority_value_returns_400()
    {
        var client = await DemoClientAsync();
        using var body = new StringContent("""{ "title": "x", "priority": "Urgent" }""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/tasks", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
