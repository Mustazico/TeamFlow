using TeamFlow.Domain.Enums;
using TaskStatus = TeamFlow.Domain.Enums.TaskStatus;

namespace TeamFlow.Application.Dashboard.Dtos;

public record DashboardStatsDto(
    int TotalProjects,
    int TotalTasks,
    int OpenTasks,
    int DoneTasks,
    int MyAssignedOpen,
    int Overdue);

public record TaskStatusCountDto(TaskStatus Status, int Count);
public record TaskPriorityCountDto(TaskPriority Priority, int Count);
public record TasksCompletedByDayDto(DateTime Date, int Count);

public record DashboardOverviewDto(
    DashboardStatsDto Stats,
    IReadOnlyList<TaskStatusCountDto> StatusBreakdown,
    IReadOnlyList<TaskPriorityCountDto> PriorityBreakdown,
    IReadOnlyList<TasksCompletedByDayDto> CompletedByDay);
