using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(Guid userId, Guid? projectId, string entityType, Guid? entityId, ActivityAction action, string? summary, object? metadata = null, CancellationToken ct = default);
}
