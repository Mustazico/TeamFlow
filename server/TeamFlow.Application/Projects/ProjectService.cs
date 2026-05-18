using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Exceptions;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Projects.Dtos;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Application.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(CancellationToken ct);
    Task<ProjectDetailDto> GetAsync(Guid id, CancellationToken ct);
    Task<ProjectSummaryDto> CreateAsync(CreateProjectRequest req, CancellationToken ct);
    Task<ProjectSummaryDto> UpdateAsync(Guid id, UpdateProjectRequest req, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddMemberRequest req, CancellationToken ct);
    Task<ProjectMemberDto> UpdateMemberRoleAsync(Guid projectId, Guid userId, UpdateMemberRoleRequest req, CancellationToken ct);
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken ct);
}

public class ProjectService : IProjectService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IActivityLogger _activity;

    public ProjectService(IAppDbContext db, ICurrentUserService current, IActivityLogger activity)
    {
        _db = db;
        _current = current;
        _activity = activity;
    }

    private Guid Me => _current.UserId ?? throw new ForbiddenException();

    public async Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(CancellationToken ct)
    {
        var userId = Me;
        var query = _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
            .OrderByDescending(p => p.UpdatedAt);

        var projects = await query.ToListAsync(ct);
        return projects.Select(Map).ToList();
    }

    public async Task<ProjectDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Project", id);

        await EnsureMemberAsync(project, ProjectRole.Viewer, ct);

        return new ProjectDetailDto(
            project.Id, project.Name, project.Description, project.Color,
            project.OwnerId, project.Owner?.DisplayName ?? "",
            project.CreatedAt, project.UpdatedAt,
            project.Members.Select(m => new ProjectMemberDto(
                m.UserId, m.User?.Email ?? "", m.User?.DisplayName ?? "", m.User?.AvatarUrl,
                m.Role, m.AddedAt)).ToList());
    }

    public async Task<ProjectSummaryDto> CreateAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var userId = Me;
        var project = new Project
        {
            Name = req.Name,
            Description = req.Description,
            Color = string.IsNullOrWhiteSpace(req.Color) ? "#6366f1" : req.Color!,
            OwnerId = userId
        };
        project.Members.Add(new ProjectMember { UserId = userId, Role = ProjectRole.Owner });
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(userId, project.Id, "Project", project.Id, ActivityAction.Created, $"Created project '{project.Name}'", ct: ct);

        var withOwner = await _db.Projects
            .Include(p => p.Owner).Include(p => p.Members).Include(p => p.Tasks)
            .FirstAsync(p => p.Id == project.Id, ct);
        return Map(withOwner);
    }

    public async Task<ProjectSummaryDto> UpdateAsync(Guid id, UpdateProjectRequest req, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.Owner).Include(p => p.Members).Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Project", id);

        await EnsureMemberAsync(project, ProjectRole.Admin, ct);

        project.Name = req.Name;
        project.Description = req.Description;
        if (!string.IsNullOrWhiteSpace(req.Color)) project.Color = req.Color!;
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, project.Id, "Project", project.Id, ActivityAction.Updated, $"Updated project '{project.Name}'", ct: ct);
        return Map(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Project", id);

        if (project.OwnerId != Me)
            throw new ForbiddenException("Only the owner can delete the project.");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, null, "Project", id, ActivityAction.Deleted, $"Deleted project '{project.Name}'", ct: ct);
    }

    public async Task<ProjectMemberDto> AddMemberAsync(Guid projectId, AddMemberRequest req, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        await EnsureMemberAsync(project, ProjectRole.Admin, ct);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct)
            ?? throw new NotFoundException("User", req.Email);

        if (project.Members.Any(m => m.UserId == user.Id))
            throw new ConflictException("User is already a member.");

        var member = new ProjectMember { ProjectId = projectId, UserId = user.Id, Role = req.Role };
        _db.ProjectMembers.Add(member);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, projectId, "ProjectMember", user.Id, ActivityAction.MemberAdded, $"Added {user.DisplayName}", ct: ct);

        return new ProjectMemberDto(user.Id, user.Email ?? "", user.DisplayName, user.AvatarUrl, req.Role, member.AddedAt);
    }

    public async Task<ProjectMemberDto> UpdateMemberRoleAsync(Guid projectId, Guid userId, UpdateMemberRoleRequest req, CancellationToken ct)
    {
        var project = await _db.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        await EnsureMemberAsync(project, ProjectRole.Admin, ct);

        var member = project.Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("Member", userId);

        if (member.Role == ProjectRole.Owner)
            throw new ConflictException("Cannot change the owner's role.");

        member.Role = req.Role;
        await _db.SaveChangesAsync(ct);

        var user = await _db.Users
            .FirstAsync(u => u.Id == userId, ct);
        return new ProjectMemberDto(user.Id, user.Email ?? "", user.DisplayName, user.AvatarUrl, member.Role, member.AddedAt);
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken ct)
    {
        var project = await _db.Projects.Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new NotFoundException("Project", projectId);

        await EnsureMemberAsync(project, ProjectRole.Admin, ct);

        var member = project.Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("Member", userId);

        if (member.Role == ProjectRole.Owner)
            throw new ConflictException("Cannot remove the owner.");

        _db.ProjectMembers.Remove(member);
        await _db.SaveChangesAsync(ct);

        await _activity.LogAsync(Me, projectId, "ProjectMember", userId, ActivityAction.MemberRemoved, "Removed member", ct: ct);
    }

    private Task EnsureMemberAsync(Project project, ProjectRole minRole, CancellationToken ct)
    {
        var userId = Me;
        if (project.OwnerId == userId) return Task.CompletedTask;
        var member = project.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) throw new ForbiddenException("You are not a member of this project.");
        // Lower enum value = higher privilege (Owner=0, Admin=1, Member=2, Viewer=3)
        if ((int)member.Role > (int)minRole)
            throw new ForbiddenException("Insufficient project role.");
        return Task.CompletedTask;
    }

    private static ProjectSummaryDto Map(Project p) => new(
        p.Id, p.Name, p.Description, p.Color,
        p.OwnerId, p.Owner?.DisplayName ?? "",
        p.Tasks.Count,
        p.Tasks.Count(t => t.Status == TaskStatus.Done),
        p.Members.Count,
        p.CreatedAt, p.UpdatedAt);
}
