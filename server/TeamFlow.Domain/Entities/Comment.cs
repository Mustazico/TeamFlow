using TeamFlow.Domain.Common;

namespace TeamFlow.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
    public Guid AuthorId { get; set; }
    public ApplicationUser? Author { get; set; }
    public string Content { get; set; } = string.Empty;
}
