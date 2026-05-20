import { apiClient } from '@/lib/apiClient';
import type {
  AuthResponse,
  ProjectDetailDto,
  ProjectSummaryDto,
  ProjectRole,
  TaskDto,
  TaskStatus,
  TaskPriority,
  CommentDto,
  ActivityLogDto,
  DashboardOverviewDto,
  NotificationDto,
  UserDto,
} from './types';

export const authApi = {
  googleLogin: (idToken: string) =>
    apiClient.post<AuthResponse>('/auth/google', { idToken }).then((r) => r.data),
  guestLogin: () =>
    apiClient.post<AuthResponse>('/auth/guest').then((r) => r.data),
  me: () => apiClient.get<UserDto>('/auth/me').then((r) => r.data),
  logout: (refreshToken: string) =>
    apiClient.post('/auth/logout', { refreshToken }).then((r) => r.data),
};

export const projectsApi = {
  list: () => apiClient.get<ProjectSummaryDto[]>('/projects').then((r) => r.data),
  get: (id: string) => apiClient.get<ProjectDetailDto>(`/projects/${id}`).then((r) => r.data),
  create: (data: { name: string; description?: string; color?: string }) =>
    apiClient.post<ProjectDetailDto>('/projects', data).then((r) => r.data),
  update: (id: string, data: { name: string; description?: string; color?: string }) =>
    apiClient.put<ProjectDetailDto>(`/projects/${id}`, data).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/projects/${id}`).then((r) => r.data),
  addMember: (id: string, data: { email: string; role: ProjectRole }) =>
    apiClient.post<ProjectMemberish>(`/projects/${id}/members`, data).then((r) => r.data),
  updateMember: (id: string, userId: string, role: ProjectRole) =>
    apiClient
      .put<ProjectMemberish>(`/projects/${id}/members/${userId}`, { role })
      .then((r) => r.data),
  removeMember: (id: string, userId: string) =>
    apiClient.delete(`/projects/${id}/members/${userId}`).then((r) => r.data),
};

type ProjectMemberish = unknown;

export const tasksApi = {
  byProject: (projectId: string) =>
    apiClient.get<TaskDto[]>(`/tasks/by-project/${projectId}`).then((r) => r.data),
  mine: () => apiClient.get<TaskDto[]>('/tasks/mine').then((r) => r.data),
  overdue: () => apiClient.get<TaskDto[]>('/tasks/overdue').then((r) => r.data),
  get: (id: string) => apiClient.get<TaskDto>(`/tasks/${id}`).then((r) => r.data),
  create: (data: {
    projectId: string;
    title: string;
    description?: string;
    priority?: TaskPriority;
    assigneeId?: string | null;
    dueDate?: string | null;
  }) => apiClient.post<TaskDto>('/tasks', data).then((r) => r.data),
  update: (
    id: string,
    data: {
      title: string;
      description?: string | null;
      status: TaskStatus;
      priority: TaskPriority;
      assigneeId?: string | null;
      dueDate?: string | null;
    },
  ) => apiClient.put<TaskDto>(`/tasks/${id}`, data).then((r) => r.data),
  move: (id: string, status: TaskStatus, orderIndex: number) =>
    apiClient
      .patch<TaskDto>(`/tasks/${id}/move`, { status, orderIndex })
      .then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/tasks/${id}`).then((r) => r.data),
};

export const commentsApi = {
  byTask: (taskId: string) =>
    apiClient.get<CommentDto[]>(`/comments/by-task/${taskId}`).then((r) => r.data),
  create: (data: { taskItemId: string; content: string; mentionedUserIds?: string[] }) =>
    apiClient.post<CommentDto>('/comments', data).then((r) => r.data),
  update: (id: string, data: { content: string }) =>
    apiClient.put<CommentDto>(`/comments/${id}`, data).then((r) => r.data),
  remove: (id: string) => apiClient.delete(`/comments/${id}`).then((r) => r.data),
};

export const activityApi = {
  recent: (take = 30) =>
    apiClient.get<ActivityLogDto[]>(`/activity/recent`, { params: { take } }).then((r) => r.data),
  byProject: (projectId: string, take = 50) =>
    apiClient
      .get<ActivityLogDto[]>(`/activity/by-project/${projectId}`, { params: { take } })
      .then((r) => r.data),
};

export const dashboardApi = {
  overview: (days = 14) =>
    apiClient
      .get<DashboardOverviewDto>('/dashboard/overview', { params: { days } })
      .then((r) => r.data),
};

export const notificationsApi = {
  list: (params: { take?: number; unreadOnly?: boolean } = {}) =>
    apiClient
      .get<NotificationDto[]>('/notifications', { params })
      .then((r) => r.data),
  unreadCount: () =>
    apiClient
      .get<{ count: number }>('/notifications/unread-count')
      .then((r) => r.data.count),
  markRead: (id: string) =>
    apiClient.post(`/notifications/${id}/read`).then((r) => r.data),
  markAllRead: () => apiClient.post('/notifications/read-all').then((r) => r.data),
};
