using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class ProjectMember
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
