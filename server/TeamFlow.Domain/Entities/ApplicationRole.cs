using Microsoft.AspNetCore.Identity;

namespace TeamFlow.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string name) : base(name) { }
}
