using TeamFlow.Domain.Common;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    public Guid? CommentId { get; set; }
    public Comment? Comment { get; set; }

    public Guid? ActorId { get; set; }
    public ApplicationUser? Actor { get; set; }

    public bool IsRead { get; set; }
}
