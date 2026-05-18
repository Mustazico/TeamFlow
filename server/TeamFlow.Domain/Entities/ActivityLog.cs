using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public string EntityType { get; set; } = string.Empty; // "Project", "Task", "Comment"
    public Guid? EntityId { get; set; }
    public ActivityAction Action { get; set; }
    public string? Summary { get; set; }
    public string? Metadata { get; set; } // JSON for extra info
}
