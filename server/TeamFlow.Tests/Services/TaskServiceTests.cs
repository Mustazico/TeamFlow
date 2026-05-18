using FluentAssertions;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Tasks;
using TeamFlow.Application.Tasks.Dtos;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Tests.Services;

public class TaskServiceTests
{
    private static (TaskService svc, StubCurrentUser current, RecordingNotificationService notifs, Infrastructure.Persistence.AppDbContext db)
        Build(Guid actingUserId)
    {
        var db = TestDb.Create();
        var current = new StubCurrentUser { UserId = actingUserId };
        var notifs = new RecordingNotificationService();
        var svc = new TaskService(db, current, new NoopActivityLogger(), notifs);
        return (svc, current, notifs, db);
    }

    [Fact]
    public async Task CreateAsync_AssigningAnotherMember_NotifiesAssignee()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var member = db.AddUser("Member", "member@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, member);

        var notifs = new RecordingNotificationService();
        var current = new StubCurrentUser { UserId = owner.Id };
        var svc = new TaskService(db, current, new NoopActivityLogger(), notifs);

        var dto = await svc.CreateAsync(
            new CreateTaskRequest(project.Id, "Ship it", null, TaskStatus.Todo, TaskPriority.Medium, member.Id, null),
            CancellationToken.None);

        dto.Title.Should().Be("Ship it");
        notifs.Calls.Should().ContainSingle(c =>
            c.UserId == member.Id &&
            c.Type == NotificationType.TaskAssigned &&
            c.TaskItemId == dto.Id);
    }

    [Fact]
    public async Task CreateAsync_AssigningSelf_DoesNotNotify()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var project = db.AddProject(owner);

        var notifs = new RecordingNotificationService();
        var svc = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), notifs);

        await svc.CreateAsync(
            new CreateTaskRequest(project.Id, "Self", null, TaskStatus.Todo, TaskPriority.Low, owner.Id, null),
            CancellationToken.None);

        notifs.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_AssigneeNotMember_Throws()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var outsider = db.AddUser("Out", "out@x.io");
        var project = db.AddProject(owner);

        var svc = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), new RecordingNotificationService());

        var act = () => svc.CreateAsync(
            new CreateTaskRequest(project.Id, "x", null, TaskStatus.Todo, TaskPriority.Low, outsider.Id, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task UpdateAsync_ReassigningToOtherMember_NotifiesNewAssignee()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var a = db.AddUser("A", "a@x.io");
        var b = db.AddUser("B", "b@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, a);
        db.AddMember(project, b);

        var notifs = new RecordingNotificationService();
        var svc = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), notifs);

        var created = await svc.CreateAsync(
            new CreateTaskRequest(project.Id, "T", null, TaskStatus.Todo, TaskPriority.Medium, a.Id, null),
            CancellationToken.None);
        notifs.Calls.Clear();

        await svc.UpdateAsync(created.Id,
            new UpdateTaskRequest("T", null, TaskStatus.Todo, TaskPriority.Medium, b.Id, null),
            CancellationToken.None);

        notifs.Calls.Should().ContainSingle(c => c.UserId == b.Id && c.Type == NotificationType.TaskAssigned);
    }

    [Fact]
    public async Task OverdueAsync_ReturnsOnlyOpenPastDueTasksForUser()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var project = db.AddProject(owner);
        var svc = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), new RecordingNotificationService());

        await svc.CreateAsync(new CreateTaskRequest(project.Id, "Past Open", null, TaskStatus.Todo, TaskPriority.Medium, null, DateTime.UtcNow.AddDays(-2)), CancellationToken.None);
        await svc.CreateAsync(new CreateTaskRequest(project.Id, "Past Done", null, TaskStatus.Done, TaskPriority.Medium, null, DateTime.UtcNow.AddDays(-2)), CancellationToken.None);
        await svc.CreateAsync(new CreateTaskRequest(project.Id, "Future Open", null, TaskStatus.Todo, TaskPriority.Medium, null, DateTime.UtcNow.AddDays(2)), CancellationToken.None);

        var overdue = await svc.OverdueAsync(CancellationToken.None);

        overdue.Should().ContainSingle(t => t.Title == "Past Open");
    }
}
