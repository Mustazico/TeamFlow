import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams, useSearchParams } from 'react-router-dom';
import { useActionModalStore } from '@/stores/actionModalStore';
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
  DragOverlay,
} from '@dnd-kit/core';
import { commentsApi, projectsApi, tasksApi } from '@/lib/api';
import type { ProjectRole, TaskDto, TaskStatus } from '@/lib/types';
import { PageContainer, PageHeader } from '@/components/common/PageHeader';
import { Button } from '@/components/ui/Button';
import { Card, CardBody, CardHeader, CardTitle } from '@/components/ui/Card';
import { Plus, Trash2, UserPlus, UserMinus } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { FieldError, Input, Label, Textarea } from '@/components/ui/Input';
import { Avatar } from '@/components/ui/Avatar';
import { KanbanColumn, KanbanCard } from './kanban';
import { TaskDetailDrawer } from './TaskDetailDrawer';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';
import { useAuthStore } from '@/stores/authStore';

const STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'Review', 'Done'];
const STATUS_LABEL: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Review: 'Review',
  Done: 'Done',
};

const newTaskSchema = z.object({
  title: z.string().min(1, 'Required').max(200),
  description: z.string().max(2000).optional(),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical']),
  assigneeId: z.string().uuid().optional().or(z.literal('')),
});
type NewTaskValues = z.infer<typeof newTaskSchema>;

const memberSchema = z.object({
  email: z.string().email(),
  role: z.enum(['Owner', 'Admin', 'Member', 'Viewer']),
});
type MemberValues = z.infer<typeof memberSchema>;

export function ProjectDetailPage() {
  const { projectId = '' } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const qc = useQueryClient();
  const me = useAuthStore((s) => s.user);
  const [addTaskOpen, setAddTaskOpen] = useState(false);
  const [addMemberOpen, setAddMemberOpen] = useState(false);
  const [activeTaskId, setActiveTaskId] = useState<string | null>(null);
  const taskParam = searchParams.get('task');
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(taskParam);

  // Sync selectedTaskId when URL param changes (e.g. back/forward navigation)
  if (selectedTaskId !== taskParam && taskParam !== null) {
    setSelectedTaskId(taskParam);
  } else if (taskParam === null && selectedTaskId !== null && !searchParams.has('task')) {
    setSelectedTaskId(null);
  }

  const openTask = (id: string) => {
    setSelectedTaskId(id);
    const next = new URLSearchParams(searchParams);
    next.set('task', id);
    setSearchParams(next, { replace: true });
  };

  const closeTask = () => {
    setSelectedTaskId(null);
    const next = new URLSearchParams(searchParams);
    next.delete('task');
    setSearchParams(next, { replace: true });
  };

  const { data: project } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectsApi.get(projectId),
    enabled: !!projectId,
  });

  const { data: tasks = [] } = useQuery({
    queryKey: ['tasks', 'project', projectId],
    queryFn: () => tasksApi.byProject(projectId),
    enabled: !!projectId,
  });

  const taskForm = useForm<NewTaskValues>({
    resolver: zodResolver(newTaskSchema),
    defaultValues: { priority: 'Medium' },
  });

  // Open task modal with AI-prefilled values
  const { createTask: aiCreateTask, closeCreateTask } = useActionModalStore();
  useEffect(() => {
    if (aiCreateTask.open && aiCreateTask.prefill && aiCreateTask.prefill.projectId === projectId) {
      taskForm.reset({
        title: aiCreateTask.prefill.title,
        description: aiCreateTask.prefill.description ?? '',
        priority: (aiCreateTask.prefill.priority as NewTaskValues['priority']) ?? 'Medium',
        assigneeId: aiCreateTask.prefill.assigneeId ?? '',
      });
      setAddTaskOpen(true);
      closeCreateTask();
    }
  }, [aiCreateTask, taskForm, closeCreateTask, projectId]);

  const memberForm = useForm<MemberValues>({
    resolver: zodResolver(memberSchema),
    defaultValues: { role: 'Member' },
  });

  const createTask = useMutation({
    mutationFn: (v: NewTaskValues) =>
      tasksApi.create({
        projectId,
        title: v.title,
        description: v.description?.trim() || undefined,
        priority: v.priority,
        assigneeId: v.assigneeId ? v.assigneeId : null,
      }),
    onSuccess: () => {
      toast.success('Task created');
      qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] });
      setAddTaskOpen(false);
      taskForm.reset({ priority: 'Medium' });
    },
    onError: (err) => handleErr(err, 'Failed to create task'),
  });

  const moveTask = useMutation({
    mutationFn: (v: { id: string; status: TaskStatus; orderIndex: number }) =>
      tasksApi.move(v.id, v.status, v.orderIndex),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] }),
    onError: (err) => handleErr(err, 'Failed to move task'),
  });

  const addMember = useMutation({
    mutationFn: (v: MemberValues) =>
      projectsApi.addMember(projectId, { email: v.email, role: v.role as ProjectRole }),
    onSuccess: () => {
      toast.success('Member added');
      qc.invalidateQueries({ queryKey: ['project', projectId] });
      setAddMemberOpen(false);
      memberForm.reset({ role: 'Member', email: '' });
    },
    onError: (err) => handleErr(err, 'Failed to add member'),
  });

  const updateMemberRole = useMutation({
    mutationFn: (v: { userId: string; role: ProjectRole }) =>
      projectsApi.updateMember(projectId, v.userId, v.role),
    onSuccess: () => {
      toast.success('Role updated');
      qc.invalidateQueries({ queryKey: ['project', projectId] });
    },
    onError: (err) => handleErr(err, 'Failed to update role'),
  });

  const removeMember = useMutation({
    mutationFn: (userId: string) => projectsApi.removeMember(projectId, userId),
    onSuccess: () => {
      toast.success('Member removed');
      qc.invalidateQueries({ queryKey: ['project', projectId] });
    },
    onError: (err) => handleErr(err, 'Failed to remove member'),
  });

  const deleteProject = useMutation({
    mutationFn: () => projectsApi.remove(projectId),
    onSuccess: () => {
      toast.success('Project deleted');
      qc.invalidateQueries({ queryKey: ['projects'] });
      window.location.assign('/projects');
    },
    onError: (err) => handleErr(err, 'Failed to delete'),
  });

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const grouped: Record<TaskStatus, TaskDto[]> = {
    Todo: [],
    InProgress: [],
    Review: [],
    Done: [],
  };
  for (const t of tasks) grouped[t.status].push(t);
  for (const s of STATUSES) grouped[s].sort((a, b) => a.orderIndex - b.orderIndex);

  const activeTask = tasks.find((t) => t.id === activeTaskId) ?? null;

  const onDragStart = (e: DragStartEvent) => setActiveTaskId(String(e.active.id));

  const onDragEnd = (e: DragEndEvent) => {
    setActiveTaskId(null);
    const overId = e.over?.id;
    if (!overId) return;
    const taskId = String(e.active.id);
    const task = tasks.find((t) => t.id === taskId);
    if (!task) return;

    const overIdStr = String(overId);
    let targetStatus: TaskStatus;
    let targetIndex: number;

    if (STATUSES.includes(overIdStr as TaskStatus)) {
      targetStatus = overIdStr as TaskStatus;
      targetIndex = grouped[targetStatus].length;
    } else {
      const overTask = tasks.find((t) => t.id === overIdStr);
      if (!overTask) return;
      targetStatus = overTask.status;
      targetIndex = grouped[targetStatus].findIndex((t) => t.id === overIdStr);
      if (targetIndex < 0) targetIndex = grouped[targetStatus].length;
    }

    if (task.status === targetStatus && task.orderIndex === targetIndex) return;

    moveTask.mutate({ id: taskId, status: targetStatus, orderIndex: targetIndex });
  };

  const isOwner = !!me && project?.ownerId === me.id;
  const myRole = project?.members.find((m) => m.userId === me?.id)?.role;
  const canManageMembers = isOwner || myRole === 'Admin';

  return (
    <PageContainer>
      <PageHeader
        title={project?.name ?? 'Project'}
        description={project?.description ?? undefined}
        actions={
          <>
            <Button onClick={() => setAddTaskOpen(true)}>
              <Plus size={16} /> Add task
            </Button>
            {isOwner && (
              <Button
                variant="danger"
                size="icon"
                title="Delete project"
                onClick={() => {
                  if (confirm('Delete this project? This cannot be undone.')) deleteProject.mutate();
                }}
              >
                <Trash2 size={16} />
              </Button>
            )}
          </>
        }
      />

      <DndContext sensors={sensors} onDragStart={onDragStart} onDragEnd={onDragEnd}>
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
          {STATUSES.map((status) => (
            <KanbanColumn
              key={status}
              id={status}
              title={STATUS_LABEL[status]}
              tasks={grouped[status]}
              onTaskClick={(id) => openTask(id)}
            />
          ))}
        </div>
        <DragOverlay>{activeTask ? <KanbanCard task={activeTask} dragging /> : null}</DragOverlay>
      </DndContext>

      {project && (
        <Card className="mt-6">
          <CardHeader className="flex items-center justify-between">
            <CardTitle>Members ({project.members.length})</CardTitle>
            {canManageMembers && (
              <Button variant="outline" size="sm" onClick={() => setAddMemberOpen(true)}>
                <UserPlus size={14} /> Add member
              </Button>
            )}
          </CardHeader>
          <CardBody>
            <ul className="divide-y divide-slate-100">
              {project.members.map((m) => (
                <li key={m.userId} className="flex items-center gap-3 py-2">
                  <Avatar name={m.displayName} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-slate-900">{m.displayName}</p>
                    <p className="text-xs text-slate-500">{m.email}</p>
                  </div>
                  {canManageMembers && m.role !== 'Owner' ? (
                    <select
                      value={m.role}
                      onChange={(e) =>
                        updateMemberRole.mutate({
                          userId: m.userId,
                          role: e.target.value as ProjectRole,
                        })
                      }
                      disabled={updateMemberRole.isPending}
                      className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs h-8"
                    >
                      <option value="Admin">Admin</option>
                      <option value="Member">Member</option>
                      <option value="Viewer">Viewer</option>
                    </select>
                  ) : (
                    <span className="text-xs text-slate-600">{m.role}</span>
                  )}
                  {canManageMembers && m.role !== 'Owner' && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Remove from project"
                      disabled={removeMember.isPending}
                      onClick={() => {
                        if (confirm(`Remove ${m.displayName} from this project?`))
                          removeMember.mutate(m.userId);
                      }}
                      className="text-slate-400 hover:text-rose-600"
                    >
                      <UserMinus size={16} />
                    </Button>
                  )}
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      )}

      <Modal open={addTaskOpen} onClose={() => setAddTaskOpen(false)} title="New task">
        <form onSubmit={taskForm.handleSubmit((v) => createTask.mutate(v))} className="space-y-4">
          <div>
            <Label htmlFor="title">Title</Label>
            <Input id="title" {...taskForm.register('title')} />
            <FieldError message={taskForm.formState.errors.title?.message} />
          </div>
          <div>
            <Label htmlFor="description">Description</Label>
            <Textarea id="description" {...taskForm.register('description')} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label htmlFor="priority">Priority</Label>
              <select
                id="priority"
                className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
                {...taskForm.register('priority')}
              >
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </div>
            <div>
              <Label htmlFor="assigneeId">Assignee</Label>
              <select
                id="assigneeId"
                className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
                {...taskForm.register('assigneeId')}
              >
                <option value="">Unassigned</option>
                {project?.members.map((m) => (
                  <option key={m.userId} value={m.userId}>
                    {m.displayName}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setAddTaskOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={createTask.isPending}>
              {createTask.isPending ? 'Creating…' : 'Create task'}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal open={addMemberOpen} onClose={() => setAddMemberOpen(false)} title="Add member">
        <form onSubmit={memberForm.handleSubmit((v) => addMember.mutate(v))} className="space-y-4">
          <div>
            <Label htmlFor="email">Email</Label>
            <Input id="email" type="email" {...memberForm.register('email')} />
            <FieldError message={memberForm.formState.errors.email?.message} />
          </div>
          <div>
            <Label htmlFor="role">Role</Label>
            <select
              id="role"
              className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
              {...memberForm.register('role')}
            >
              <option value="Admin">Admin</option>
              <option value="Member">Member</option>
              <option value="Viewer">Viewer</option>
            </select>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setAddMemberOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={addMember.isPending}>
              {addMember.isPending ? 'Adding…' : 'Add member'}
            </Button>
          </div>
        </form>
      </Modal>

      <TaskDetailDrawer
        taskId={selectedTaskId}
        projectId={projectId}
        onClose={closeTask}
      />
    </PageContainer>
  );
}

function handleErr(err: unknown, fallback: string) {
  const msg = isAxiosError(err)
    ? (err.response?.data as { error?: string })?.error ?? fallback
    : fallback;
  toast.error(msg);
}

// Re-export to avoid unused import warning if commentsApi tree-shake fails
void commentsApi;
