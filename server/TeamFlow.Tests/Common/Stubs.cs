using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common;

internal sealed class StubCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsInRole(string role) => false;
}

internal sealed class NoopActivityLogger : IActivityLogger
{
    public Task LogAsync(Guid userId, Guid? projectId, string entityType, Guid? entityId, ActivityAction action, string? summary, object? metadata = null, CancellationToken ct = default)
        => Task.CompletedTask;
}
