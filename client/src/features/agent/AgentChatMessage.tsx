import type { ChatMessage } from '@/stores/agentStore';
import { Bot, User, Loader2, CheckCircle2, XCircle } from 'lucide-react';
import { ProposalCard } from './ProposalCard';
import { cn } from '@/lib/utils';

interface Props {
  message: ChatMessage;
  onConfirmProposal?: () => void;
  onCancelProposal?: () => void;
}

export function AgentChatMessage({ message, onConfirmProposal, onCancelProposal }: Props) {
  const isUser = message.role === 'user';

  return (
    <div className={cn('flex gap-3', isUser ? 'flex-row-reverse' : '')}>
      <div
        className={cn(
          'flex-shrink-0 w-7 h-7 rounded-full flex items-center justify-center',
          isUser ? 'bg-indigo-100 dark:bg-indigo-900' : 'bg-slate-100 dark:bg-slate-800',
        )}
      >
        {isUser ? <User size={14} /> : <Bot size={14} />}
      </div>
      <div className={cn('flex flex-col gap-1 max-w-[85%]', isUser ? 'items-end' : 'items-start')}>
        {message.toolCalls && message.toolCalls.length > 0 && (
          <div className="flex flex-col gap-1 mb-1">
            {message.toolCalls.map((tc, i) => (
              <div
                key={i}
                className="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400"
              >
                {tc.status === 'running' && <Loader2 size={12} className="animate-spin" />}
                {tc.status === 'done' && <CheckCircle2 size={12} className="text-green-500" />}
                {tc.status === 'error' && <XCircle size={12} className="text-red-500" />}
                <span className="font-mono">{formatToolName(tc.name)}</span>
              </div>
            ))}
          </div>
        )}
        {message.content && (
          <div
            className={cn(
              'px-3 py-2 rounded-lg text-sm whitespace-pre-wrap',
              isUser
                ? 'bg-indigo-600 text-white'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-slate-100',
            )}
          >
            {message.content}
          </div>
        )}
        {message.proposal && (
          <ProposalCard
            proposal={message.proposal}
            onConfirm={onConfirmProposal ?? (() => {})}
            onCancel={onCancelProposal ?? (() => {})}
          />
        )}
      </div>
    </div>
  );
}

function formatToolName(name: string): string {
  return name.replace(/_/g, ' ');
}
