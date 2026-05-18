import { type HTMLAttributes } from 'react';
import { cn } from '@/lib/utils';
import type { TaskPriority, TaskStatus } from '@/lib/types';

type Tone = 'slate' | 'indigo' | 'amber' | 'emerald' | 'rose' | 'sky' | 'violet';

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: Tone;
}

const tones: Record<Tone, string> = {
  slate: 'bg-slate-100 text-slate-700',
  indigo: 'bg-indigo-100 text-indigo-700',
  amber: 'bg-amber-100 text-amber-800',
  emerald: 'bg-emerald-100 text-emerald-700',
  rose: 'bg-rose-100 text-rose-700',
  sky: 'bg-sky-100 text-sky-700',
  violet: 'bg-violet-100 text-violet-700',
};

export function Badge({ tone = 'slate', className, ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
        tones[tone],
        className,
      )}
      {...props}
    />
  );
}

export function StatusBadge({ status }: { status: TaskStatus }) {
  const map: Record<TaskStatus, { tone: Tone; label: string }> = {
    Todo: { tone: 'slate', label: 'To do' },
    InProgress: { tone: 'sky', label: 'In progress' },
    Review: { tone: 'violet', label: 'Review' },
    Done: { tone: 'emerald', label: 'Done' },
  };
  const m = map[status];
  return <Badge tone={m.tone}>{m.label}</Badge>;
}

export function PriorityBadge({ priority }: { priority: TaskPriority }) {
  const map: Record<TaskPriority, { tone: Tone; label: string }> = {
    Low: { tone: 'slate', label: 'Low' },
    Medium: { tone: 'sky', label: 'Medium' },
    High: { tone: 'amber', label: 'High' },
    Critical: { tone: 'rose', label: 'Critical' },
  };
  const m = map[priority];
  return <Badge tone={m.tone}>{m.label}</Badge>;
}
