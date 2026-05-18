using TeamFlow.Domain.Common;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;
using TaskPriority = TeamFlow.Domain.Enums.TaskPriority;

namespace TeamFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int OrderIndex { get; set; }

    public Guid? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    public Guid CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
