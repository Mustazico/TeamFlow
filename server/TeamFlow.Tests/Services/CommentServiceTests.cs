using FluentAssertions;
using TeamFlow.Application.Comments;
using TeamFlow.Application.Comments.Dtos;
using TeamFlow.Application.Tasks;
using TeamFlow.Application.Tasks.Dtos;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Tests.Services;

public class CommentServiceTests
{
    [Fact]
    public async Task CreateAsync_Mentions_FanOutToMembersOnly_DedupesAndExcludesSelf()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var m1 = db.AddUser("M1", "m1@x.io");
        var m2 = db.AddUser("M2", "m2@x.io");
        var outsider = db.AddUser("Out", "out@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, m1);
        db.AddMember(project, m2);

        var notifs = new RecordingNotificationService();
        var tasks = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), new RecordingNotificationService());
        var task = await tasks.CreateAsync(
            new CreateTaskRequest(project.Id, "T", null, TaskStatus.Todo, TaskPriority.Medium, null, null),
            CancellationToken.None);

        var comments = new CommentService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), notifs);

        await comments.CreateAsync(new CreateCommentRequest(
            task.Id,
            "Hey @m1 @m2 @out @owner",
            new List<Guid> { m1.Id, m1.Id, m2.Id, outsider.Id, owner.Id }),
            CancellationToken.None);

        notifs.Calls.Should().HaveCount(2);
        notifs.Calls.Select(c => c.UserId).Should().BeEquivalentTo(new[] { m1.Id, m2.Id });
        notifs.Calls.Should().OnlyContain(c => c.Type == NotificationType.Mentioned);
    }

    [Fact]
    public async Task UpdateAsync_OtherAuthor_Forbidden()
    {
        var db = TestDb.Create();
        var owner = db.AddUser("Owner", "owner@x.io");
        var other = db.AddUser("O", "o@x.io");
        var project = db.AddProject(owner);
        db.AddMember(project, other);

        var tasks = new TaskService(db, new StubCurrentUser { UserId = owner.Id }, new NoopActivityLogger(), new RecordingNotificationService());
        var task = await tasks.CreateAsync(
            new CreateTaskRequest(project.Id, "T", null, TaskStatus.Todo, TaskPriority.Medium, null, null),
            CancellationToken.None);

        var ownerCurrent = new StubCurrentUser { UserId = owner.Id };
        var ownerComments = new CommentService(db, ownerCurrent, new NoopActivityLogger(), new RecordingNotificationService());
        var c = await ownerComments.CreateAsync(new CreateCommentRequest(task.Id, "hi"), CancellationToken.None);

        var otherComments = new CommentService(db, new StubCurrentUser { UserId = other.Id }, new NoopActivityLogger(), new RecordingNotificationService());
        var act = () => otherComments.UpdateAsync(c.Id, new UpdateCommentRequest("edited"), CancellationToken.None);

        await act.Should().ThrowAsync<Application.Common.Exceptions.ForbiddenException>();
    }
}
