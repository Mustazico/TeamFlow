namespace TeamFlow.Domain.Enums;

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ProjectRole
{
    Owner = 0,
    Admin = 1,
    Member = 2,
    Viewer = 3
}

public enum ActivityAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    StatusChanged = 3,
    Assigned = 4,
    Commented = 5,
    MemberAdded = 6,
    MemberRemoved = 7
}
