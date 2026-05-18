using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Api.Setup;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        foreach (var role in new[] { "Admin", "User", "Guest" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }

        var admin = await EnsureUserAsync(userManager, "admin@teamflow.local", "TeamFlow Admin", "Admin#12345", "Admin", "User");
        var guest = await EnsureUserAsync(userManager, "guest@teamflow.local", "Demo Guest", "Guest#12345", "Guest");

        var demo = await db.Projects.FirstOrDefaultAsync(p => p.Name == "TeamFlow Demo");
        if (demo is null)
        {
            demo = new Project
            {
                Name = "TeamFlow Demo",
                Description = "Sample project for the public demo. Guest accounts can browse but not modify.",
                Color = "#6366f1",
                OwnerId = admin.Id,
            };
            demo.Members.Add(new ProjectMember { UserId = admin.Id, Role = ProjectRole.Owner });
            demo.Members.Add(new ProjectMember { UserId = guest.Id, Role = ProjectRole.Viewer });

            demo.Tasks.Add(new TaskItem
            {
                Title = "Welcome to TeamFlow",
                Description = "This is a read-only demo. Sign in with the admin account to edit, or click 'Continue as guest' to browse.",
                Status = TaskStatus.Todo,
                Priority = TaskPriority.Medium,
                CreatedById = admin.Id,
                OrderIndex = 0,
            });
            demo.Tasks.Add(new TaskItem
            {
                Title = "Drag tasks across columns",
                Description = "The Kanban board uses dnd-kit. Try moving a task between Todo / In Progress / Done.",
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.High,
                CreatedById = admin.Id,
                OrderIndex = 0,
            });
            demo.Tasks.Add(new TaskItem
            {
                Title = "Toggle dark mode",
                Description = "Click the sun/moon icon in the header. Tailwind v4 + @custom-variant dark.",
                Status = TaskStatus.Done,
                Priority = TaskPriority.Low,
                CreatedById = admin.Id,
                OrderIndex = 0,
            });

            db.Projects.Add(demo);
            await db.SaveChangesAsync(default);
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        params string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(user, password);
        }
        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }
}
