export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: UserDto;
}

export type TaskStatus = 'Todo' | 'InProgress' | 'Review' | 'Done';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type ProjectRole = 'Owner' | 'Admin' | 'Member' | 'Viewer';

export interface ProjectSummaryDto {
  id: string;
  name: string;
  description?: string | null;
  color: string;
  ownerId: string;
  ownerName: string;
  memberCount: number;
  taskCount: number;
  doneTaskCount: number;
  createdAt: string;
}

export interface ProjectMemberDto {
  userId: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  role: ProjectRole;
  addedAt: string;
}

export interface ProjectDetailDto extends ProjectSummaryDto {
  members: ProjectMemberDto[];
}

export interface TaskDto {
  id: string;
  projectId: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  orderIndex: number;
  assigneeId?: string | null;
  assigneeName?: string | null;
  assigneeAvatarUrl?: string | null;
  createdById: string;
  createdByName: string;
  dueDate?: string | null;
  completedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  commentCount: number;
}

export interface CommentDto {
  id: string;
  taskItemId: string;
  authorId: string;
  authorName: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface ActivityLogDto {
  id: string;
  createdAt: string;
  userId: string;
  userName: string;
  projectId?: string | null;
  projectName?: string | null;
  entityType: string;
  entityId?: string | null;
  action: string;
  summary?: string | null;
}

export interface DashboardOverviewDto {
  stats: {
    totalProjects: number;
    totalTasks: number;
    openTasks: number;
    doneTasks: number;
    myAssignedOpen: number;
    overdue: number;
  };
  statusBreakdown: Array<{ status: TaskStatus; count: number }>;
  priorityBreakdown: Array<{ priority: TaskPriority; count: number }>;
  completedByDay: Array<{ date: string; count: number }>;
}

export type NotificationType = 'TaskAssigned' | 'Mentioned';

export interface NotificationDto {
  id: string;
  type: NotificationType;
  message: string;
  projectId?: string | null;
  taskItemId?: string | null;
  commentId?: string | null;
  actorId?: string | null;
  actorName?: string | null;
  isRead: boolean;
  createdAt: string;
}
