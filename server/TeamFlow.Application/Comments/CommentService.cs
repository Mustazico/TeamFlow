using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Comments.Dtos;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Notifications;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Comments;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> ListForTaskAsync(Guid taskId, CancellationToken ct);
    Task<CommentDto> CreateAsync(CreateCommentRequest req, CancellationToken ct);
    Task<CommentDto> UpdateAsync(Guid id, UpdateCommentRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public class CommentService : ICommentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IActivityLogger _activity;
    private readonly INotificationService _notifications;

    public CommentService(IAppDbContext db, ICurrentUserService current, IActivityLogger activity, INotificationService notifications)
    {
        _db = db;
        _current = current;
        _activity = activity;
        _notifications = notifications;
    }

    private Guid Me => _current.UserId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<CommentDto>> ListForTaskAsync(Guid taskId, CancellationToken ct)
    {
        var task = await EnsureTaskAccessAsync(taskId, ct);
        var comments = await _db.Comments
            .Where(c => c.TaskItemId == taskId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
        return comments.Select(Map).ToList();
    }

    public async Task<CommentDto> CreateAsync(CreateCommentRequest req, CancellationToken ct)
    {
        var task = await EnsureTaskAccessAsync(req.TaskItemId, ct);
        var comment = new Comment
        {
            TaskItemId = req.TaskItemId,
            AuthorId = Me,
            Content = req.Content
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, task.ProjectId, "Comment", comment.Id, ActivityAction.Commented, $"Commented on '{task.Title}'", ct: ct);

        if (req.MentionedUserIds is { Count: > 0 })
        {
            var unique = req.MentionedUserIds.Where(id => id != Me).Distinct().ToList();
            if (unique.Count > 0)
            {
                var project = await _db.Projects.Include(p => p.Members)
                    .FirstAsync(p => p.Id == task.ProjectId, ct);
                var allowed = unique
                    .Where(id => project.OwnerId == id || project.Members.Any(m => m.UserId == id))
                    .ToList();
                var actorName = await _db.Users.Where(u => u.Id == Me).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) ?? "Someone";
                foreach (var uid in allowed)
                {
                    await _notifications.CreateAsync(
                        uid,
                        NotificationType.Mentioned,
                        $"{actorName} mentioned you in '{task.Title}'",
                        task.ProjectId, task.Id, comment.Id, Me, ct);
                }
            }
        }

        var saved = await _db.Comments.Include(c => c.Author).FirstAsync(c => c.Id == comment.Id, ct);
        return Map(saved);
    }

    public async Task<CommentDto> UpdateAsync(Guid id, UpdateCommentRequest req, CancellationToken ct)
    {
        var comment = await _db.Comments.Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Comment", id);

        if (comment.AuthorId != Me)
            throw new ForbiddenException("You can only edit your own comments.");

        comment.Content = req.Content;
        await _db.SaveChangesAsync(ct);
        return Map(comment);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("Comment", id);

        if (comment.AuthorId != Me)
            throw new ForbiddenException("You can only delete your own comments.");

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> EnsureTaskAccessAsync(Guid taskId, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        var userId = Me;
        var project = await _db.Projects.Include(p => p.Members)
            .FirstAsync(p => p.Id == task.ProjectId, ct);
        if (project.OwnerId != userId && project.Members.All(m => m.UserId != userId))
            throw new ForbiddenException("You are not a member of this project.");
        return task;
    }

    private static CommentDto Map(Comment c) => new(
        c.Id, c.TaskItemId, c.AuthorId, c.Author?.DisplayName ?? "", c.Author?.AvatarUrl,
        c.Content, c.CreatedAt, c.UpdatedAt);
}
