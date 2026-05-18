using FluentAssertions;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Notifications;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;

namespace TeamFlow.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsRow()
    {
        var db = TestDb.Create();
        var user = db.AddUser("U", "u@x.io");
        var svc = new NotificationService(db, new StubCurrentUser { UserId = user.Id });

        await svc.CreateAsync(user.Id, NotificationType.TaskAssigned, "hello", null, null, null, null, CancellationToken.None);

        db.Notifications.Should().ContainSingle(n => n.UserId == user.Id && n.Message == "hello" && !n.IsRead);
    }

    [Fact]
    public async Task UnreadCount_OnlyCountsCurrentUserUnread()
    {
        var db = TestDb.Create();
        var me = db.AddUser("Me", "me@x.io");
        var other = db.AddUser("O", "o@x.io");
        db.Notifications.Add(new Notification { UserId = me.Id, Type = NotificationType.TaskAssigned, Message = "a", IsRead = false });
        db.Notifications.Add(new Notification { UserId = me.Id, Type = NotificationType.TaskAssigned, Message = "b", IsRead = true });
        db.Notifications.Add(new Notification { UserId = other.Id, Type = NotificationType.TaskAssigned, Message = "c", IsRead = false });
        await db.SaveChangesAsync();

        var svc = new NotificationService(db, new StubCurrentUser { UserId = me.Id });

        (await svc.UnreadCountAsync(CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task MarkReadAsync_OtherUsersRow_Forbidden()
    {
        var db = TestDb.Create();
        var me = db.AddUser("Me", "me@x.io");
        var other = db.AddUser("O", "o@x.io");
        var n = new Notification { UserId = other.Id, Type = NotificationType.TaskAssigned, Message = "x", IsRead = false };
        db.Notifications.Add(n);
        await db.SaveChangesAsync();

        var svc = new NotificationService(db, new StubCurrentUser { UserId = me.Id });

        var act = () => svc.MarkReadAsync(n.Id, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksOnlyCurrentUser()
    {
        var db = TestDb.Create();
        var me = db.AddUser("Me", "me@x.io");
        var other = db.AddUser("O", "o@x.io");
        db.Notifications.Add(new Notification { UserId = me.Id, Type = NotificationType.TaskAssigned, Message = "a", IsRead = false });
        db.Notifications.Add(new Notification { UserId = me.Id, Type = NotificationType.TaskAssigned, Message = "b", IsRead = false });
        db.Notifications.Add(new Notification { UserId = other.Id, Type = NotificationType.TaskAssigned, Message = "c", IsRead = false });
        await db.SaveChangesAsync();

        var svc = new NotificationService(db, new StubCurrentUser { UserId = me.Id });
        await svc.MarkAllReadAsync(CancellationToken.None);

        db.Notifications.Where(n => n.UserId == me.Id).All(n => n.IsRead).Should().BeTrue();
        db.Notifications.Single(n => n.UserId == other.Id).IsRead.Should().BeFalse();
    }
}
