using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Activity.Dtos;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Activity;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityLogDto>> RecentForMeAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<ActivityLogDto>> ForProjectAsync(Guid projectId, int take, CancellationToken ct);
}

public class ActivityService : IActivityService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public ActivityService(IAppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    private Guid Me => _current.UserId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<ActivityLogDto>> RecentForMeAsync(int take, CancellationToken ct)
    {
        var userId = Me;
        var projectIds = await _db.Projects
            .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
            .Select(p => p.Id).ToListAsync(ct);

        var logs = await _db.ActivityLogs
            .Where(l => l.ProjectId != null && projectIds.Contains(l.ProjectId.Value))
            .Include(l => l.User).Include(l => l.Project)
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return logs.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<ActivityLogDto>> ForProjectAsync(Guid projectId, int take, CancellationToken ct)
    {
        var userId = Me;
        var project = await _db.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);
        if (project.OwnerId != userId && project.Members.All(m => m.UserId != userId))
            throw new ForbiddenException();

        var logs = await _db.ActivityLogs
            .Where(l => l.ProjectId == projectId)
            .Include(l => l.User).Include(l => l.Project)
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return logs.Select(Map).ToList();
    }

    private static ActivityLogDto Map(Domain.Entities.ActivityLog l) => new(
        l.Id, l.CreatedAt,
        l.UserId, l.User?.DisplayName ?? "", l.User?.AvatarUrl,
        l.ProjectId, l.Project?.Name,
        l.EntityType, l.EntityId, l.Action, l.Summary);
}
