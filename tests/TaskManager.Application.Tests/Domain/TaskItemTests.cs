using FluentAssertions;
using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tests.Domain;

public class TaskItemTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_with_valid_data_sets_all_fields()
    {
        var task = TaskItem.Create(UserId, "  Write tests  ", "  First red, then green ", TaskItemStatus.InProgress, Today.AddDays(3), Now);

        task.Id.Should().NotBeEmpty();
        task.UserId.Should().Be(UserId);
        task.Title.Should().Be("Write tests");
        task.Description.Should().Be("First red, then green");
        task.Status.Should().Be(TaskItemStatus.InProgress);
        task.DueDate.Should().Be(Today.AddDays(3));
        task.CreatedAt.Should().Be(Now);
        task.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_title_fails(string? title)
    {
        var act = () => TaskItem.Create(UserId, title, null, TaskItemStatus.Todo, null, Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public void Create_with_title_too_long_fails()
    {
        var act = () => TaskItem.Create(UserId, new string('a', TaskItem.TitleMaxLength + 1), null, TaskItemStatus.Todo, null, Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Title");
    }

    [Fact]
    public void Create_with_description_too_long_fails()
    {
        var act = () => TaskItem.Create(UserId, "Title", new string('a', TaskItem.DescriptionMaxLength + 1), TaskItemStatus.Todo, null, Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Description");
    }

    [Fact]
    public void Create_turns_blank_description_into_null()
    {
        var task = TaskItem.Create(UserId, "Title", "   ", TaskItemStatus.Todo, null, Now);

        task.Description.Should().BeNull();
    }

    [Fact]
    public void Create_with_past_due_date_fails()
    {
        var act = () => TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, Today.AddDays(-1), Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("DueDate");
    }

    [Fact]
    public void Create_with_due_date_today_is_allowed()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, Today, Now);

        task.DueDate.Should().Be(Today);
    }

    [Fact]
    public void Create_with_undefined_status_fails()
    {
        var act = () => TaskItem.Create(UserId, "Title", null, (TaskItemStatus)42, null, Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Status");
    }

    [Fact]
    public void Create_without_user_fails()
    {
        var act = () => TaskItem.Create(Guid.Empty, "Title", null, TaskItemStatus.Todo, null, Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("UserId");
    }

    [Fact]
    public void Create_reports_every_broken_rule_at_once()
    {
        var act = () => TaskItem.Create(UserId, "", null, (TaskItemStatus)42, Today.AddDays(-1), Now);

        act.Should().Throw<DomainValidationException>()
            .Which.Errors.Keys.Should().BeEquivalentTo("Title", "Status", "DueDate");
    }

    [Fact]
    public void Update_changes_fields_and_timestamp()
    {
        var task = TaskItem.Create(UserId, "Old", null, TaskItemStatus.Todo, null, Now);
        var later = Now.AddHours(2);

        task.Update("New", "Details", TaskItemStatus.Done, Today.AddDays(1), later);

        task.Title.Should().Be("New");
        task.Description.Should().Be("Details");
        task.Status.Should().Be(TaskItemStatus.Done);
        task.DueDate.Should().Be(Today.AddDays(1));
        task.UpdatedAt.Should().Be(later);
        task.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Update_keeps_an_existing_overdue_date()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, Today, Now);
        var nextWeek = Now.AddDays(7);

        task.Update("Title", null, TaskItemStatus.Done, Today, nextWeek);

        task.Status.Should().Be(TaskItemStatus.Done);
        task.DueDate.Should().Be(Today);
    }

    [Fact]
    public void Update_to_a_new_past_due_date_fails()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, null, Now);

        var act = () => task.Update("Title", null, TaskItemStatus.Todo, Today.AddDays(-2), Now);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("DueDate");
    }

    [Fact]
    public void Failed_update_leaves_task_unchanged()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, null, Now);

        var act = () => task.Update("", null, TaskItemStatus.Done, null, Now.AddHours(1));

        act.Should().Throw<DomainValidationException>();
        task.Title.Should().Be("Title");
        task.Status.Should().Be(TaskItemStatus.Todo);
        task.UpdatedAt.Should().Be(Now);
    }
        [Fact]
    public void Create_sets_the_given_priority()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, null, Now, TaskPriority.High);

        task.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void Create_without_priority_defaults_to_medium()
    {
        var task = TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, null, Now);

        task.Priority.Should().Be(TaskPriority.Medium);
    }

    [Fact]
    public void Create_with_undefined_priority_fails()
    {
        var act = () => TaskItem.Create(UserId, "Title", null, TaskItemStatus.Todo, null, Now, (TaskPriority)42);

        act.Should().Throw<DomainValidationException>().Which.Errors.Should().ContainKey("Priority");
    }
}
