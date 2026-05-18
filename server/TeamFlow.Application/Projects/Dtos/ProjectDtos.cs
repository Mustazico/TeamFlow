using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Projects.Dtos;

public record CreateProjectRequest(string Name, string? Description, string? Color);
public record UpdateProjectRequest(string Name, string? Description, string? Color);
public record AddMemberRequest(string Email, ProjectRole Role);
public record UpdateMemberRoleRequest(ProjectRole Role);

public record ProjectSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    string Color,
    Guid OwnerId,
    string OwnerName,
    int TaskCount,
    int DoneTaskCount,
    int MemberCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Color,
    Guid OwnerId,
    string OwnerName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ProjectMemberDto> Members);

public record ProjectMemberDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    ProjectRole Role,
    DateTime AddedAt);
