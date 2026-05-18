using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Tests.Common;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options);
    }

    public static ApplicationUser AddUser(this AppDbContext db, string name, string email)
    {
        var u = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = name,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    public static Project AddProject(this AppDbContext db, ApplicationUser owner, string name = "Demo")
    {
        var p = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Color = "#6366f1",
            OwnerId = owner.Id
        };
        p.Members.Add(new ProjectMember { ProjectId = p.Id, UserId = owner.Id, Role = ProjectRole.Owner });
        db.Projects.Add(p);
        db.SaveChanges();
        return p;
    }

    public static ProjectMember AddMember(this AppDbContext db, Project project, ApplicationUser user, ProjectRole role = ProjectRole.Member)
    {
        var m = new ProjectMember { ProjectId = project.Id, UserId = user.Id, Role = role };
        db.ProjectMembers.Add(m);
        db.SaveChanges();
        return m;
    }
}
