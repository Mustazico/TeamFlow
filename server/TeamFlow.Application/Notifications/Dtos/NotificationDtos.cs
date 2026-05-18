using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Notifications.Dtos;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? ProjectId,
    Guid? TaskItemId,
    Guid? CommentId,
    Guid? ActorId,
    string? ActorName,
    bool IsRead,
    DateTime CreatedAt);

public record UnreadCountDto(int Count);
