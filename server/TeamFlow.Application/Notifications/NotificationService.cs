using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Notifications.Dtos;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListMineAsync(int take, bool unreadOnly, CancellationToken ct);
    Task<int> UnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(Guid id, CancellationToken ct);
    Task MarkAllReadAsync(CancellationToken ct);
    Task CreateAsync(
        Guid userId,
        NotificationType type,
        string message,
        Guid? projectId,
        Guid? taskItemId,
        Guid? commentId,
        Guid? actorId,
        CancellationToken ct);
}

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public NotificationService(IAppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    private Guid Me => _current.UserId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(int take, bool unreadOnly, CancellationToken ct)
    {
        var userId = Me;
        var query = _db.Notifications
            .Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        var rows = await query
            .Include(n => n.Actor)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public Task<int> UnreadCountAsync(CancellationToken ct)
    {
        var userId = Me;
        return _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        var userId = Me;
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Notification", id);
        if (n.UserId != userId) throw new ForbiddenException();
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var userId = Me;
        var rows = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);
        if (rows.Count == 0) return;
        foreach (var n in rows) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateAsync(
        Guid userId,
        NotificationType type,
        string message,
        Guid? projectId,
        Guid? taskItemId,
        Guid? commentId,
        Guid? actorId,
        CancellationToken ct)
    {
        var n = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message.Length > 500 ? message[..500] : message,
            ProjectId = projectId,
            TaskItemId = taskItemId,
            CommentId = commentId,
            ActorId = actorId,
            IsRead = false
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }

    private static NotificationDto Map(Notification n) => new(
        n.Id, n.Type, n.Message,
        n.ProjectId, n.TaskItemId, n.CommentId,
        n.ActorId, n.Actor?.DisplayName,
        n.IsRead, n.CreatedAt);
}
