using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TeamFlow.Application;

// Marker for assembly scanning
public sealed class AssemblyMarker { }

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();
        services.AddScoped<Projects.IProjectService, Projects.ProjectService>();
        services.AddScoped<Tasks.ITaskService, Tasks.TaskService>();
        services.AddScoped<Comments.ICommentService, Comments.CommentService>();
        services.AddScoped<Activity.IActivityService, Activity.ActivityService>();
        services.AddScoped<Dashboard.IDashboardService, Dashboard.DashboardService>();
        services.AddScoped<Notifications.INotificationService, Notifications.NotificationService>();
        services.AddScoped<Agent.IAgentToolExecutor, Agent.AgentToolExecutor>();
        return services;
    }
}
