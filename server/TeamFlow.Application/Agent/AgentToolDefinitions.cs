namespace TeamFlow.Application.Agent;

public static class AgentToolDefinitions
{
    public const string SystemPrompt = """
        You are TeamFlow AI Assistant — a helpful project management assistant embedded in the TeamFlow application.
        You help users manage their projects, tasks, and team collaboration.

        Key behaviors:
        - Be concise and action-oriented
        - When the user asks you to do something, use the appropriate tool immediately
        - When listing tasks or projects, format them clearly
        - If you need a projectId or taskId that you don't have, use list_projects or get_my_tasks first to find it
        - Always confirm what you did after performing an action
        - Use Norwegian or English depending on what language the user writes in

        Available task statuses: Todo, InProgress, Review, Done
        Available task priorities: Low, Medium, High, Critical
        """;

    public static readonly IReadOnlyList<ToolDefinition> Tools =
    [
        new("create_task", "Create a new task in a project", """
            {
              "type": "object",
              "properties": {
                "projectId": { "type": "string", "description": "The GUID of the project" },
                "title": { "type": "string", "description": "Task title" },
                "description": { "type": "string", "description": "Optional task description" },
                "status": { "type": "string", "enum": ["Todo", "InProgress", "Review", "Done"], "description": "Task status, defaults to Todo" },
                "priority": { "type": "string", "enum": ["Low", "Medium", "High", "Critical"], "description": "Task priority, defaults to Medium" },
                "assigneeId": { "type": "string", "description": "Optional user GUID to assign the task to" },
                "dueDate": { "type": "string", "description": "Optional due date in ISO 8601 format" }
              },
              "required": ["projectId", "title"]
            }
            """),

        new("update_task", "Update an existing task's properties", """
            {
              "type": "object",
              "properties": {
                "taskId": { "type": "string", "description": "The GUID of the task to update" },
                "title": { "type": "string", "description": "New title" },
                "description": { "type": "string", "description": "New description" },
                "status": { "type": "string", "enum": ["Todo", "InProgress", "Review", "Done"] },
                "priority": { "type": "string", "enum": ["Low", "Medium", "High", "Critical"] },
                "assigneeId": { "type": "string", "description": "User GUID or null to unassign" },
                "dueDate": { "type": "string", "description": "Due date in ISO 8601 format or null" }
              },
              "required": ["taskId"]
            }
            """),

        new("move_task", "Move a task to a different status column", """
            {
              "type": "object",
              "properties": {
                "taskId": { "type": "string", "description": "The GUID of the task" },
                "status": { "type": "string", "enum": ["Todo", "InProgress", "Review", "Done"], "description": "Target status" },
                "orderIndex": { "type": "integer", "description": "Position in the column, defaults to 0" }
              },
              "required": ["taskId", "status"]
            }
            """),

        new("create_project", "Create a new project", """
            {
              "type": "object",
              "properties": {
                "name": { "type": "string", "description": "Project name" },
                "description": { "type": "string", "description": "Optional project description" },
                "color": { "type": "string", "description": "Optional color hex code, e.g. #6366f1" }
              },
              "required": ["name"]
            }
            """),

        new("add_comment", "Add a comment to a task", """
            {
              "type": "object",
              "properties": {
                "taskId": { "type": "string", "description": "The GUID of the task to comment on" },
                "content": { "type": "string", "description": "Comment text" }
              },
              "required": ["taskId", "content"]
            }
            """),

        new("get_my_tasks", "Get all tasks assigned to the current user", """
            {
              "type": "object",
              "properties": {},
              "required": []
            }
            """),

        new("get_project_tasks", "Get all tasks in a specific project", """
            {
              "type": "object",
              "properties": {
                "projectId": { "type": "string", "description": "The GUID of the project" }
              },
              "required": ["projectId"]
            }
            """),

        new("list_projects", "List all projects the user has access to", """
            {
              "type": "object",
              "properties": {},
              "required": []
            }
            """),

        new("get_dashboard", "Get dashboard overview with stats and charts data", """
            {
              "type": "object",
              "properties": {},
              "required": []
            }
            """)
    ];
}

public record ToolDefinition(string Name, string Description, string ParametersJson);
