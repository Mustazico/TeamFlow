import { useDraggable, useDroppable } from '@dnd-kit/core';
import type { TaskDto } from '@/lib/types';
import { PriorityBadge } from '@/components/ui/Badge';
import { Avatar } from '@/components/ui/Avatar';
import { MessageSquare, Calendar } from 'lucide-react';
import { formatDate, cn } from '@/lib/utils';

export function KanbanColumn({
  id,
  title,
  tasks,
  onTaskClick,
}: {
  id: string;
  title: string;
  tasks: TaskDto[];
  onTaskClick: (id: string) => void;
}) {
  const { setNodeRef, isOver } = useDroppable({ id });
  return (
    <div
      ref={setNodeRef}
      className={cn(
        'bg-slate-50 rounded-lg border border-slate-200 flex flex-col min-h-[300px]',
        isOver && 'ring-2 ring-indigo-400',
      )}
    >
      <div className="px-3 py-2 border-b border-slate-200 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700">{title}</h3>
        <span className="text-xs text-slate-500 bg-white border border-slate-200 rounded-full px-2 py-0.5">
          {tasks.length}
        </span>
      </div>
      <div className="p-2 space-y-2 flex-1">
        {tasks.map((t) => (
          <DraggableCard key={t.id} task={t} onClick={() => onTaskClick(t.id)} />
        ))}
        {tasks.length === 0 && (
          <div className="text-xs text-slate-400 text-center py-6">Drop tasks here</div>
        )}
      </div>
    </div>
  );
}

function DraggableCard({ task, onClick }: { task: TaskDto; onClick: () => void }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({ id: task.id });
  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      onClick={onClick}
      className={cn('cursor-grab active:cursor-grabbing', isDragging && 'opacity-30')}
    >
      <KanbanCard task={task} />
    </div>
  );
}

export function KanbanCard({ task, dragging }: { task: TaskDto; dragging?: boolean }) {
  return (
    <div
      className={cn(
        'bg-white rounded-md border border-slate-200 shadow-sm p-3 hover:border-indigo-300 transition-colors',
        dragging && 'shadow-lg ring-2 ring-indigo-300',
      )}
    >
      <p className="text-sm font-medium text-slate-900 mb-2 line-clamp-2">{task.title}</p>
      <div className="flex items-center justify-between text-xs text-slate-500">
        <div className="flex items-center gap-2">
          <PriorityBadge priority={task.priority} />
          {task.dueDate && (
            <span className="inline-flex items-center gap-1">
              <Calendar size={12} />
              {formatDate(task.dueDate)}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {task.commentCount > 0 && (
            <span className="inline-flex items-center gap-1">
              <MessageSquare size={12} />
              {task.commentCount}
            </span>
          )}
          {task.assigneeName && <Avatar name={task.assigneeName} size="sm" />}
        </div>
      </div>
    </div>
  );
}
