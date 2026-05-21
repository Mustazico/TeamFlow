using System.Text.Json;
using TeamFlow.Application.Comments;
using TeamFlow.Application.Comments.Dtos;
using TeamFlow.Application.Dashboard;
using TeamFlow.Application.Projects;
using TeamFlow.Application.Projects.Dtos;
using TeamFlow.Application.Tasks;
using TeamFlow.Application.Tasks.Dtos;
using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Application.Agent;

/// <summary>
/// Mutation tools are ones that change data and require user confirmation.
/// </summary>
public static class AgentToolCategories
{
    public static readonly HashSet<string> MutationTools = new()
    {
        "create_task", "update_task", "move_task", "create_project", "add_comment"
    };

    public static bool IsMutation(string toolName) => MutationTools.Contains(toolName);
}

public interface IAgentToolExecutor
{
    Task<string> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct);
}

public class AgentToolExecutor : IAgentToolExecutor
{
    private readonly ITaskService _tasks;
    private readonly IProjectService _projects;
    private readonly ICommentService _comments;
    private readonly IDashboardService _dashboard;

    public AgentToolExecutor(
        ITaskService tasks,
        IProjectService projects,
        ICommentService comments,
        IDashboardService dashboard)
    {
        _tasks = tasks;
        _projects = projects;
        _comments = comments;
        _dashboard = dashboard;
    }

    public async Task<string> ExecuteAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        var args = JsonDocument.Parse(argumentsJson).RootElement;

        return toolName switch
        {
            "create_task" => await CreateTaskAsync(args, ct),
            "update_task" => await UpdateTaskAsync(args, ct),
            "move_task" => await MoveTaskAsync(args, ct),
            "create_project" => await CreateProjectAsync(args, ct),
            "add_comment" => await AddCommentAsync(args, ct),
            "get_my_tasks" => await GetMyTasksAsync(ct),
            "get_project_tasks" => await GetProjectTasksAsync(args, ct),
            "list_projects" => await ListProjectsAsync(ct),
            "get_dashboard" => await GetDashboardAsync(ct),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
        };
    }

    private async Task<string> CreateTaskAsync(JsonElement args, CancellationToken ct)
    {
        var req = new CreateTaskRequest(
            ProjectId: Guid.Parse(args.GetProperty("projectId").GetString()!),
            Title: args.GetProperty("title").GetString()!,
            Description: args.TryGetProperty("description", out var d) ? d.GetString() : null,
            Status: args.TryGetProperty("status", out var s) ? Enum.Parse<TaskStatus>(s.GetString()!, true) : TaskStatus.Todo,
            Priority: args.TryGetProperty("priority", out var p) ? Enum.Parse<TaskPriority>(p.GetString()!, true) : TaskPriority.Medium,
            AssigneeId: args.TryGetProperty("assigneeId", out var a) && a.ValueKind != JsonValueKind.Null ? Guid.Parse(a.GetString()!) : null,
            DueDate: args.TryGetProperty("dueDate", out var dd) && dd.ValueKind != JsonValueKind.Null ? DateTime.Parse(dd.GetString()!) : null
        );
        var result = await _tasks.CreateAsync(req, ct);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> UpdateTaskAsync(JsonElement args, CancellationToken ct)
    {
        var id = Guid.Parse(args.GetProperty("taskId").GetString()!);
        var existing = await _tasks.GetAsync(id, ct);
        var req = new UpdateTaskRequest(
            Title: args.TryGetProperty("title", out var t) ? t.GetString()! : existing.Title,
            Description: args.TryGetProperty("description", out var d) ? d.GetString() : existing.Description,
            Status: args.TryGetProperty("status", out var s) ? Enum.Parse<TaskStatus>(s.GetString()!, true) : existing.Status,
            Priority: args.TryGetProperty("priority", out var p) ? Enum.Parse<TaskPriority>(p.GetString()!, true) : existing.Priority,
            AssigneeId: args.TryGetProperty("assigneeId", out var a) ? (a.ValueKind != JsonValueKind.Null ? Guid.Parse(a.GetString()!) : null) : existing.AssigneeId,
            DueDate: args.TryGetProperty("dueDate", out var dd) ? (dd.ValueKind != JsonValueKind.Null ? DateTime.Parse(dd.GetString()!) : null) : existing.DueDate
        );
        var result = await _tasks.UpdateAsync(id, req, ct);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> MoveTaskAsync(JsonElement args, CancellationToken ct)
    {
        var id = Guid.Parse(args.GetProperty("taskId").GetString()!);
        var status = Enum.Parse<TaskStatus>(args.GetProperty("status").GetString()!, true);
        var orderIndex = args.TryGetProperty("orderIndex", out var o) ? o.GetInt32() : 0;
        var result = await _tasks.MoveAsync(id, new MoveTaskRequest(status, orderIndex), ct);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> CreateProjectAsync(JsonElement args, CancellationToken ct)
    {
        var req = new CreateProjectRequest(
            Name: args.GetProperty("name").GetString()!,
            Description: args.TryGetProperty("description", out var d) ? d.GetString() : null,
            Color: args.TryGetProperty("color", out var c) ? c.GetString() : null
        );
        var result = await _projects.CreateAsync(req, ct);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> AddCommentAsync(JsonElement args, CancellationToken ct)
    {
        var req = new CreateCommentRequest(
            TaskItemId: Guid.Parse(args.GetProperty("taskId").GetString()!),
            Content: args.GetProperty("content").GetString()!
        );
        var result = await _comments.CreateAsync(req, ct);
        return JsonSerializer.Serialize(result);
    }

    private async Task<string> GetMyTasksAsync(CancellationToken ct)
    {
        var tasks = await _tasks.MyTasksAsync(ct);
        return JsonSerializer.Serialize(tasks);
    }

    private async Task<string> GetProjectTasksAsync(JsonElement args, CancellationToken ct)
    {
        var projectId = Guid.Parse(args.GetProperty("projectId").GetString()!);
        var tasks = await _tasks.ListForProjectAsync(projectId, ct);
        return JsonSerializer.Serialize(tasks);
    }

    private async Task<string> ListProjectsAsync(CancellationToken ct)
    {
        var projects = await _projects.ListAsync(ct);
        return JsonSerializer.Serialize(projects);
    }

    private async Task<string> GetDashboardAsync(CancellationToken ct)
    {
        var overview = await _dashboard.GetOverviewAsync(14, ct);
        return JsonSerializer.Serialize(overview);
    }
}
