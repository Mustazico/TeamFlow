using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Notifications;
using TeamFlow.Application.Tasks.Dtos;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> ListForProjectAsync(Guid projectId, CancellationToken ct);
    Task<TaskDto> GetAsync(Guid id, CancellationToken ct);
    Task<TaskDto> CreateAsync(CreateTaskRequest req, CancellationToken ct);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest req, CancellationToken ct);
    Task<TaskDto> MoveAsync(Guid id, MoveTaskRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TaskDto>> MyTasksAsync(CancellationToken ct);
    Task<IReadOnlyList<TaskDto>> OverdueAsync(CancellationToken ct);
}

public class TaskService : ITaskService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IActivityLogger _activity;
    private readonly INotificationService _notifications;

    public TaskService(IAppDbContext db, ICurrentUserService current, IActivityLogger activity, INotificationService notifications)
    {
        _db = db;
        _current = current;
        _activity = activity;
        _notifications = notifications;
    }

    private Guid Me => _current.UserId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<TaskDto>> ListForProjectAsync(Guid projectId, CancellationToken ct)
    {
        await EnsureProjectAccessAsync(projectId, ProjectRole.Viewer, ct);
        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .OrderBy(t => t.Status).ThenBy(t => t.OrderIndex)
            .ToListAsync(ct);
        return tasks.Select(Map).ToList();
    }

    public async Task<TaskDto> GetAsync(Guid id, CancellationToken ct)
    {
        var task = await LoadAsync(id, ct);
        await EnsureProjectAccessAsync(task.ProjectId, ProjectRole.Viewer, ct);
        return Map(task);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest req, CancellationToken ct)
    {
        await EnsureProjectAccessAsync(req.ProjectId, ProjectRole.Member, ct);
        await EnsureAssigneeIsProjectMemberAsync(req.ProjectId, req.AssigneeId, ct);

        var maxOrder = await _db.Tasks
            .Where(t => t.ProjectId == req.ProjectId && t.Status == req.Status)
            .Select(t => (int?)t.OrderIndex).MaxAsync(ct) ?? -1;

        var task = new TaskItem
        {
            ProjectId = req.ProjectId,
            Title = req.Title,
            Description = req.Description,
            Status = req.Status,
            Priority = req.Priority,
            AssigneeId = req.AssigneeId,
            DueDate = req.DueDate,
            CreatedById = Me,
            OrderIndex = maxOrder + 1
        };
        if (req.Status == TaskStatus.Done) task.CompletedAt = DateTime.UtcNow;

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, req.ProjectId, "Task", task.Id, ActivityAction.Created, $"Created task '{task.Title}'", ct: ct);

        if (task.AssigneeId.HasValue && task.AssigneeId.Value != Me)
        {
            var actorName = await GetUserDisplayNameAsync(Me, ct);
            await _notifications.CreateAsync(
                task.AssigneeId.Value,
                NotificationType.TaskAssigned,
                $"{actorName} assigned you to '{task.Title}'",
                task.ProjectId, task.Id, null, Me, ct);
        }

        return Map(await LoadAsync(task.Id, ct));
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest req, CancellationToken ct)
    {
        var task = await LoadAsync(id, ct);
        await EnsureProjectAccessAsync(task.ProjectId, ProjectRole.Member, ct);
        await EnsureAssigneeIsProjectMemberAsync(task.ProjectId, req.AssigneeId, ct);

        var statusChanged = task.Status != req.Status;
        var assigneeChanged = task.AssigneeId != req.AssigneeId;

        task.Title = req.Title;
        task.Description = req.Description;
        task.Status = req.Status;
        task.Priority = req.Priority;
        task.AssigneeId = req.AssigneeId;
        task.DueDate = req.DueDate;
        if (req.Status == TaskStatus.Done && task.CompletedAt is null)
            task.CompletedAt = DateTime.UtcNow;
        else if (req.Status != TaskStatus.Done)
            task.CompletedAt = null;

        await _db.SaveChangesAsync(ct);

        if (statusChanged)
            await _activity.LogAsync(Me, task.ProjectId, "Task", task.Id, ActivityAction.StatusChanged, $"Status → {req.Status}", ct: ct);
        else if (assigneeChanged)
            await _activity.LogAsync(Me, task.ProjectId, "Task", task.Id, ActivityAction.Assigned, "Assignee changed", ct: ct);
        else
            await _activity.LogAsync(Me, task.ProjectId, "Task", task.Id, ActivityAction.Updated, $"Updated task '{task.Title}'", ct: ct);

        if (assigneeChanged && task.AssigneeId.HasValue && task.AssigneeId.Value != Me)
        {
            var actorName = await GetUserDisplayNameAsync(Me, ct);
            await _notifications.CreateAsync(
                task.AssigneeId.Value,
                NotificationType.TaskAssigned,
                $"{actorName} assigned you to '{task.Title}'",
                task.ProjectId, task.Id, null, Me, ct);
        }

        return Map(await LoadAsync(task.Id, ct));
    }

    public async Task<TaskDto> MoveAsync(Guid id, MoveTaskRequest req, CancellationToken ct)
    {
        var task = await LoadAsync(id, ct);
        await EnsureProjectAccessAsync(task.ProjectId, ProjectRole.Member, ct);

        var statusChanged = task.Status != req.Status;
        task.Status = req.Status;
        task.OrderIndex = req.OrderIndex;
        if (req.Status == TaskStatus.Done && task.CompletedAt is null)
            task.CompletedAt = DateTime.UtcNow;
        else if (req.Status != TaskStatus.Done)
            task.CompletedAt = null;

        await _db.SaveChangesAsync(ct);

        if (statusChanged)
            await _activity.LogAsync(Me, task.ProjectId, "Task", task.Id, ActivityAction.StatusChanged, $"Status → {req.Status}", ct: ct);

        return Map(await LoadAsync(task.Id, ct));
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await LoadAsync(id, ct);
        await EnsureProjectAccessAsync(task.ProjectId, ProjectRole.Admin, ct);

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, task.ProjectId, "Task", id, ActivityAction.Deleted, $"Deleted task '{task.Title}'", ct: ct);
    }

    public async Task<IReadOnlyList<TaskDto>> MyTasksAsync(CancellationToken ct)
    {
        var userId = Me;
        var tasks = await _db.Tasks
            .Where(t => t.AssigneeId == userId)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .Include(t => t.Project)
            .OrderBy(t => t.Status).ThenBy(t => t.DueDate)
            .ToListAsync(ct);
        return tasks.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TaskDto>> OverdueAsync(CancellationToken ct)
    {
        var userId = Me;
        var now = DateTime.UtcNow;
        var tasks = await _db.Tasks
            .Where(t => t.DueDate != null
                && t.DueDate < now
                && t.Status != TaskStatus.Done
                && (t.Project!.OwnerId == userId
                    || t.Project.Members.Any(m => m.UserId == userId)))
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .Include(t => t.Project)
            .OrderBy(t => t.DueDate)
            .ToListAsync(ct);
        return tasks.Select(Map).ToList();
    }

    private async Task<TaskItem> LoadAsync(Guid id, CancellationToken ct) =>
        await _db.Tasks
            .Include(t => t.Assignee).Include(t => t.CreatedBy).Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new NotFoundException("Task", id);

    private async Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken ct) =>
        await _db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).FirstOrDefaultAsync(ct) ?? "Someone";

    private async Task EnsureProjectAccessAsync(Guid projectId, ProjectRole minRole, CancellationToken ct)
    {
        var userId = Me;
        var project = await _db.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);
        if (project.OwnerId == userId) return;
        var member = project.Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new ForbiddenException("You are not a member of this project.");
        if ((int)member.Role > (int)minRole)
            throw new ForbiddenException("Insufficient project role.");
    }

    private async Task EnsureAssigneeIsProjectMemberAsync(Guid projectId, Guid? assigneeId, CancellationToken ct)
    {
        if (assigneeId is null) return;
        var project = await _db.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);
        if (project.OwnerId == assigneeId.Value) return;
        if (project.Members.Any(m => m.UserId == assigneeId.Value)) return;
        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["AssigneeId"] = new[] { "Assignee must be a member of the project." }
        });
    }

    private static TaskDto Map(TaskItem t) => new(
        t.Id, t.ProjectId, t.Title, t.Description,
        t.Status, t.Priority, t.OrderIndex,
        t.AssigneeId, t.Assignee?.DisplayName, t.Assignee?.AvatarUrl,
        t.CreatedById, t.CreatedBy?.DisplayName ?? "",
        t.DueDate, t.CompletedAt, t.Comments.Count, t.CreatedAt, t.UpdatedAt);
}
