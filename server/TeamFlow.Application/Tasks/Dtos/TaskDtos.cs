using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;
using TaskPriority = TeamFlow.Domain.Enums.TaskPriority;

namespace TeamFlow.Application.Tasks.Dtos;

public record CreateTaskRequest(
    Guid ProjectId,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueDate);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueDate);

public record MoveTaskRequest(TaskStatus Status, int OrderIndex);

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    int OrderIndex,
    Guid? AssigneeId,
    string? AssigneeName,
    string? AssigneeAvatarUrl,
    Guid CreatedById,
    string CreatedByName,
    DateTime? DueDate,
    DateTime? CompletedAt,
    int CommentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
