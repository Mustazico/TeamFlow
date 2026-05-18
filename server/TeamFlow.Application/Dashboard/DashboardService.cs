using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Dashboard.Dtos;
using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(int days, CancellationToken ct);
}

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public DashboardService(IAppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(int days, CancellationToken ct)
    {
        var userId = _current.UserId ?? throw new ForbiddenException();
        days = Math.Clamp(days, 1, 90);

        var projectIds = await _db.Projects
            .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
            .Select(p => p.Id).ToListAsync(ct);

        var tasks = await _db.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .Select(t => new { t.Status, t.Priority, t.DueDate, t.CompletedAt, t.AssigneeId })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var stats = new DashboardStatsDto(
            TotalProjects: projectIds.Count,
            TotalTasks: tasks.Count,
            OpenTasks: tasks.Count(t => t.Status != TaskStatus.Done),
            DoneTasks: tasks.Count(t => t.Status == TaskStatus.Done),
            MyAssignedOpen: tasks.Count(t => t.AssigneeId == userId && t.Status != TaskStatus.Done),
            Overdue: tasks.Count(t => t.DueDate.HasValue && t.DueDate < now && t.Status != TaskStatus.Done));

        var statusBreakdown = Enum.GetValues<TaskStatus>()
            .Select(s => new TaskStatusCountDto(s, tasks.Count(t => t.Status == s)))
            .ToList();

        var priorityBreakdown = Enum.GetValues<TaskPriority>()
            .Select(p => new TaskPriorityCountDto(p, tasks.Count(t => t.Priority == p)))
            .ToList();

        var since = now.Date.AddDays(-(days - 1));
        var completed = tasks
            .Where(t => t.CompletedAt.HasValue && t.CompletedAt.Value.Date >= since)
            .GroupBy(t => t.CompletedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var byDay = Enumerable.Range(0, days)
            .Select(i => since.AddDays(i))
            .Select(d => new TasksCompletedByDayDto(d, completed.GetValueOrDefault(d, 0)))
            .ToList();

        return new DashboardOverviewDto(stats, statusBreakdown, priorityBreakdown, byDay);
    }
}
