using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Activity.Dtos;

public record ActivityLogDto(
    Guid Id,
    DateTime CreatedAt,
    Guid UserId,
    string UserName,
    string? UserAvatarUrl,
    Guid? ProjectId,
    string? ProjectName,
    string EntityType,
    Guid? EntityId,
    ActivityAction Action,
    string? Summary);
