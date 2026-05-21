import type { ActionProposal } from '@/stores/agentStore';
import { CheckCircle2, XCircle, FolderPlus, ListPlus, MessageSquarePlus, ArrowRightLeft, Pencil } from 'lucide-react';
import { cn } from '@/lib/utils';

interface Props {
  proposal: ActionProposal;
  onConfirm: () => void;
  onCancel: () => void;
}

const actionMeta: Record<string, { label: string; icon: typeof FolderPlus }> = {
  create_project: { label: 'Create Project', icon: FolderPlus },
  create_task: { label: 'Create Task', icon: ListPlus },
  update_task: { label: 'Update Task', icon: Pencil },
  move_task: { label: 'Move Task', icon: ArrowRightLeft },
  add_comment: { label: 'Add Comment', icon: MessageSquarePlus },
};

export function ProposalCard({ proposal, onConfirm, onCancel }: Props) {
  const meta = actionMeta[proposal.action] ?? { label: proposal.action, icon: ListPlus };
  const Icon = meta.icon;
  const params = proposal.parameters;

  return (
    <div className="rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-850 overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-2 px-3 py-2 bg-slate-50 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700">
        <Icon size={14} className="text-indigo-500" />
        <span className="text-xs font-semibold text-slate-700 dark:text-slate-300">{meta.label}</span>
        {proposal.status === 'confirmed' && (
          <span className="ml-auto flex items-center gap-1 text-xs text-green-600">
            <CheckCircle2 size={12} /> Confirmed
          </span>
        )}
        {proposal.status === 'cancelled' && (
          <span className="ml-auto flex items-center gap-1 text-xs text-slate-400">
            <XCircle size={12} /> Cancelled
          </span>
        )}
      </div>

      {/* Parameters */}
      <div className="px-3 py-2 space-y-1">
        {Object.entries(params)
          .filter(([, v]) => v != null && v !== '')
          .map(([key, value]) => (
            <div key={key} className="flex gap-2 text-xs">
              <span className="text-slate-500 dark:text-slate-400 min-w-[80px] font-medium">
                {formatKey(key)}
              </span>
              <span className="text-slate-800 dark:text-slate-200 break-all">
                {String(value)}
              </span>
            </div>
          ))}
      </div>

      {/* Actions */}
      {proposal.status === 'pending' && (
        <div className="flex gap-2 px-3 py-2 border-t border-slate-200 dark:border-slate-700">
          <button
            onClick={onConfirm}
            className={cn(
              'flex-1 px-3 py-1.5 rounded-md text-xs font-medium',
              'bg-indigo-600 text-white hover:bg-indigo-700 transition-colors',
            )}
          >
            Confirm
          </button>
          <button
            onClick={onCancel}
            className={cn(
              'flex-1 px-3 py-1.5 rounded-md text-xs font-medium',
              'bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors',
            )}
          >
            Cancel
          </button>
        </div>
      )}
    </div>
  );
}

function formatKey(key: string): string {
  return key
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, (s) => s.toUpperCase())
    .replace(/Id$/, ' ID');
}
