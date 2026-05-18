using System.Text.Json;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly IAppDbContext _db;

    public ActivityLogger(IAppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid userId, Guid? projectId, string entityType, Guid? entityId, ActivityAction action, string? summary, object? metadata = null, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
