import { useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { X, Trash2, Send, AtSign } from 'lucide-react';
import { commentsApi, projectsApi, tasksApi } from '@/lib/api';
import type { TaskPriority, TaskStatus } from '@/lib/types';
import { Button } from '@/components/ui/Button';
import { Input, Label, Textarea } from '@/components/ui/Input';
import { Avatar } from '@/components/ui/Avatar';
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge';
import { relativeTime } from '@/lib/utils';
import { useAuthStore } from '@/stores/authStore';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';

interface Props {
  taskId: string | null;
  projectId: string;
  onClose: () => void;
}

export function TaskDetailDrawer({ taskId, projectId, onClose }: Props) {
  const qc = useQueryClient();
  const me = useAuthStore((s) => s.user);

  const { data: task } = useQuery({
    queryKey: ['task', taskId],
    queryFn: () => tasksApi.get(taskId!),
    enabled: !!taskId,
  });

  const { data: project } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectsApi.get(projectId),
    enabled: !!projectId,
  });

  const { data: comments = [] } = useQuery({
    queryKey: ['comments', taskId],
    queryFn: () => commentsApi.byTask(taskId!),
    enabled: !!taskId,
  });

  const [title, setTitle] = useState(task?.title ?? '');
  const [description, setDescription] = useState(task?.description ?? '');
  const [status, setStatus] = useState<TaskStatus>(task?.status ?? 'Todo');
  const [priority, setPriority] = useState<TaskPriority>(task?.priority ?? 'Medium');
  const [assigneeId, setAssigneeId] = useState<string>(task?.assigneeId ?? '');
  const [dueDate, setDueDate] = useState(task?.dueDate ? task.dueDate.slice(0, 10) : '');
  const [comment, setComment] = useState('');
  const [mentioned, setMentioned] = useState<Map<string, string>>(new Map());
  const [mentionOpen, setMentionOpen] = useState(false);
  const mentionRef = useRef<HTMLDivElement | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const [lastTaskId, setLastTaskId] = useState<string | undefined>(task?.id);

  // Sync form state when the loaded task changes (replaces useEffect + setState)
  if (task && task.id !== lastTaskId) {
    setLastTaskId(task.id);
    setTitle(task.title);
    setDescription(task.description ?? '');
    setStatus(task.status);
    setPriority(task.priority);
    setAssigneeId(task.assigneeId ?? '');
    setDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '');
  }

  useEffect(() => {
    if (!mentionOpen) return;
    const onDocClick = (e: MouseEvent) => {
      if (!mentionRef.current?.contains(e.target as Node)) setMentionOpen(false);
    };
    document.addEventListener('mousedown', onDocClick);
    return () => document.removeEventListener('mousedown', onDocClick);
  }, [mentionOpen]);

  const update = useMutation({
    mutationFn: () =>
      tasksApi.update(task!.id, {
        title,
        description: description || null,
        status,
        priority,
        assigneeId: assigneeId || null,
        dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      }),
    onSuccess: () => {
      toast.success('Task saved');
      qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] });
      qc.invalidateQueries({ queryKey: ['task', taskId] });
    },
    onError: (err) => handleErr(err, 'Failed to save'),
  });

  const remove = useMutation({
    mutationFn: () => tasksApi.remove(task!.id),
    onSuccess: () => {
      toast.success('Task deleted');
      qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] });
      onClose();
    },
    onError: (err) => handleErr(err, 'Failed to delete'),
  });

  const addComment = useMutation({
    mutationFn: () =>
      commentsApi.create({
        taskItemId: task!.id,
        content: comment,
        mentionedUserIds: mentioned.size > 0 ? Array.from(mentioned.keys()) : undefined,
      }),
    onSuccess: () => {
      setComment('');
      setMentioned(new Map());
      qc.invalidateQueries({ queryKey: ['comments', taskId] });
      qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] });
    },
    onError: (err) => handleErr(err, 'Failed to comment'),
  });

  const deleteComment = useMutation({
    mutationFn: (id: string) => commentsApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['comments', taskId] });
      qc.invalidateQueries({ queryKey: ['tasks', 'project', projectId] });
    },
  });

  if (!taskId) return null;

  return (
    <div className="fixed inset-0 z-40 flex justify-end bg-slate-900/40" onClick={onClose}>
      <div
        className="bg-white w-full max-w-xl h-full overflow-y-auto shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="sticky top-0 bg-white border-b border-slate-200 px-5 py-3 flex items-center justify-between z-10">
          <div className="flex items-center gap-2">
            <StatusBadge status={status} />
            <PriorityBadge priority={priority} />
          </div>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => {
                if (confirm('Delete this task?')) remove.mutate();
              }}
              title="Delete"
            >
              <Trash2 size={16} />
            </Button>
            <Button variant="ghost" size="icon" onClick={onClose}>
              <X size={18} />
            </Button>
          </div>
        </div>

        <div className="p-5 space-y-4">
          {!task ? (
            <p className="text-slate-500">Loading…</p>
          ) : (
            <>
              <div>
                <Label htmlFor="t-title">Title</Label>
                <Input
                  id="t-title"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                />
              </div>
              <div>
                <Label htmlFor="t-desc">Description</Label>
                <Textarea
                  id="t-desc"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  className="min-h-[120px]"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <Label htmlFor="t-status">Status</Label>
                  <select
                    id="t-status"
                    value={status}
                    onChange={(e) => setStatus(e.target.value as TaskStatus)}
                    className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
                  >
                    <option value="Todo">To do</option>
                    <option value="InProgress">In progress</option>
                    <option value="Review">Review</option>
                    <option value="Done">Done</option>
                  </select>
                </div>
                <div>
                  <Label htmlFor="t-prio">Priority</Label>
                  <select
                    id="t-prio"
                    value={priority}
                    onChange={(e) => setPriority(e.target.value as TaskPriority)}
                    className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
                  >
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                    <option value="Critical">Critical</option>
                  </select>
                </div>
                <div>
                  <Label htmlFor="t-assignee">Assignee</Label>
                  <select
                    id="t-assignee"
                    value={assigneeId}
                    onChange={(e) => setAssigneeId(e.target.value)}
                    className="block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm h-10"
                  >
                    <option value="">Unassigned</option>
                    {project?.members.map((m) => (
                      <option key={m.userId} value={m.userId}>
                        {m.displayName}
                      </option>
                    ))}
                    {task?.assigneeId &&
                      !project?.members.some((m) => m.userId === task.assigneeId) && (
                        <option value={task.assigneeId}>
                          {task.assigneeName ?? 'Unknown user'}
                        </option>
                      )}
                  </select>
                </div>
                <div>
                  <Label htmlFor="t-due">Due date</Label>
                  <Input
                    id="t-due"
                    type="date"
                    value={dueDate}
                    onChange={(e) => setDueDate(e.target.value)}
                  />
                </div>
              </div>
              <div className="flex justify-end">
                <Button onClick={() => update.mutate()} disabled={update.isPending}>
                  {update.isPending ? 'Saving…' : 'Save changes'}
                </Button>
              </div>

              <div className="pt-4 border-t border-slate-100">
                <h3 className="text-sm font-semibold text-slate-900 mb-3">
                  Comments ({comments.length})
                </h3>
                <ul className="space-y-3 mb-4">
                  {comments.map((c) => (
                    <li key={c.id} className="flex gap-3">
                      <Avatar name={c.authorName} />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-medium text-slate-900">
                            {c.authorName}
                          </span>
                          <span className="text-xs text-slate-500">
                            {relativeTime(c.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm text-slate-700 whitespace-pre-wrap">
                          {c.content}
                        </p>
                      </div>
                      {me?.id === c.authorId && (
                        <button
                          onClick={() => deleteComment.mutate(c.id)}
                          className="text-slate-400 hover:text-rose-600 p-1"
                          title="Delete"
                        >
                          <Trash2 size={14} />
                        </button>
                      )}
                    </li>
                  ))}
                  {comments.length === 0 && (
                    <p className="text-sm text-slate-500">No comments yet.</p>
                  )}
                </ul>

                <div className="space-y-2">
                  {mentioned.size > 0 && (
                    <div className="flex flex-wrap gap-1">
                      {Array.from(mentioned.entries()).map(([id, name]) => (
                        <span
                          key={id}
                          className="inline-flex items-center gap-1 rounded-full bg-indigo-50 text-indigo-700 text-xs px-2 py-0.5"
                        >
                          @{name}
                          <button
                            type="button"
                            onClick={() => {
                              const next = new Map(mentioned);
                              next.delete(id);
                              setMentioned(next);
                            }}
                            className="hover:text-indigo-900"
                            aria-label={`Remove mention of ${name}`}
                          >
                            <X size={12} />
                          </button>
                        </span>
                      ))}
                    </div>
                  )}
                  <div className="flex gap-2">
                    <Textarea
                      ref={textareaRef}
                      placeholder="Add a comment\u2026"
                      value={comment}
                      onChange={(e) => setComment(e.target.value)}
                      className="min-h-[60px]"
                    />
                    <div className="flex flex-col gap-2">
                      <div className="relative" ref={mentionRef}>
                        <Button
                          type="button"
                          variant="outline"
                          size="icon"
                          onClick={() => setMentionOpen((v) => !v)}
                          title="Mention"
                        >
                          <AtSign size={16} />
                        </Button>
                        {mentionOpen && (
                          <div className="absolute right-0 bottom-full mb-2 w-56 max-h-64 overflow-y-auto rounded-md border border-slate-200 bg-white shadow-lg z-20">
                            {project?.members && project.members.length > 0 ? (
                              <ul className="py-1">
                                {project.members
                                  .filter((m) => m.userId !== me?.id)
                                  .map((m) => {
                                    const checked = mentioned.has(m.userId);
                                    return (
                                      <li key={m.userId}>
                                        <button
                                          type="button"
                                          onClick={() => {
                                            const next = new Map(mentioned);
                                            if (checked) {
                                              next.delete(m.userId);
                                            } else {
                                              next.set(m.userId, m.displayName);
                                              setComment((c) =>
                                                c.endsWith(' ') || c.length === 0
                                                  ? `${c}@${m.displayName} `
                                                  : `${c} @${m.displayName} `,
                                              );
                                              setTimeout(() => textareaRef.current?.focus(), 0);
                                            }
                                            setMentioned(next);
                                          }}
                                          className="w-full text-left px-3 py-2 text-sm hover:bg-slate-50 flex items-center gap-2"
                                        >
                                          <span
                                            className={`inline-block h-3 w-3 rounded-sm border ${
                                              checked
                                                ? 'bg-indigo-600 border-indigo-600'
                                                : 'border-slate-300'
                                            }`}
                                          />
                                          <span className="truncate">{m.displayName}</span>
                                        </button>
                                      </li>
                                    );
                                  })}
                              </ul>
                            ) : (
                              <p className="px-3 py-2 text-sm text-slate-500">
                                No members to mention
                              </p>
                            )}
                          </div>
                        )}
                      </div>
                      <Button
                        onClick={() => addComment.mutate()}
                        disabled={!comment.trim() || addComment.isPending}
                        size="icon"
                      >
                        <Send size={16} />
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function handleErr(err: unknown, fallback: string) {
  const msg = isAxiosError(err)
    ? (err.response?.data as { error?: string })?.error ?? fallback
    : fallback;
  toast.error(msg);
}
