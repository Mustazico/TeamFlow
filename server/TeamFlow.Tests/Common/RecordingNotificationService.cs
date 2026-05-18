using TeamFlow.Application.Notifications;
using TeamFlow.Application.Notifications.Dtos;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common;

internal sealed class RecordingNotificationService : INotificationService
{
    public record Call(Guid UserId, NotificationType Type, string Message, Guid? ProjectId, Guid? TaskItemId, Guid? CommentId, Guid? ActorId);

    public List<Call> Calls { get; } = new();

    public Task<IReadOnlyList<NotificationDto>> ListMineAsync(int take, bool unreadOnly, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<NotificationDto>>(Array.Empty<NotificationDto>());

    public Task<int> UnreadCountAsync(CancellationToken ct) => Task.FromResult(0);
    public Task MarkReadAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
    public Task MarkAllReadAsync(CancellationToken ct) => Task.CompletedTask;

    public Task CreateAsync(Guid userId, NotificationType type, string message, Guid? projectId, Guid? taskItemId, Guid? commentId, Guid? actorId, CancellationToken ct)
    {
        Calls.Add(new Call(userId, type, message, projectId, taskItemId, commentId, actorId));
        return Task.CompletedTask;
    }
}
