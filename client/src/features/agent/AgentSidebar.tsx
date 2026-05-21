import { useState, useRef, useEffect } from 'react';
import { X, Bot, Send, Trash2, Square } from 'lucide-react';
import { useAgentStore } from '@/stores/agentStore';
import { useAgentChat } from './useAgentChat';
import { AgentChatMessage } from './AgentChatMessage';
import { cn } from '@/lib/utils';

export function AgentSidebar() {
  const { isOpen, close, clear } = useAgentStore();
  const { messages, isStreaming, sendMessage, stop, confirmProposal, cancelProposal } = useAgentChat();
  const [input, setInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  useEffect(() => {
    if (isOpen) inputRef.current?.focus();
  }, [isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isStreaming) return;
    sendMessage(input.trim());
    setInput('');
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  return (
    <div
      className={cn(
        'fixed top-0 right-0 h-full w-96 bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-700 shadow-xl z-50 flex flex-col transition-transform duration-300',
        isOpen ? 'translate-x-0' : 'translate-x-full',
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-slate-200 dark:border-slate-700">
        <div className="flex items-center gap-2">
          <Bot size={18} className="text-indigo-500" />
          <span className="font-semibold text-sm">AI Assistant</span>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={clear}
            className="p-1.5 rounded hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500"
            title="Clear chat"
          >
            <Trash2 size={14} />
          </button>
          <button
            onClick={close}
            className="p-1.5 rounded hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500"
          >
            <X size={14} />
          </button>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.length === 0 && (
          <div className="text-center text-slate-400 dark:text-slate-500 text-sm mt-8">
            <Bot size={32} className="mx-auto mb-3 text-slate-300 dark:text-slate-600" />
            <p className="font-medium">How can I help you?</p>
            <p className="mt-1 text-xs">
              I can create tasks, manage projects, and give you an overview of your work.
            </p>
          </div>
        )}
        {messages.map((msg) => (
          <AgentChatMessage
            key={msg.id}
            message={msg}
            onConfirmProposal={
              msg.proposal?.status === 'pending'
                ? () => confirmProposal(msg.id, msg.proposal!.action, msg.proposal!.parameters)
                : undefined
            }
            onCancelProposal={
              msg.proposal?.status === 'pending' ? () => cancelProposal(msg.id) : undefined
            }
          />
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSubmit} className="p-3 border-t border-slate-200 dark:border-slate-700">
        <div className="flex items-end gap-2">
          <textarea
            ref={inputRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask me anything..."
            rows={1}
            className="flex-1 resize-none rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:text-slate-100"
          />
          {isStreaming ? (
            <button
              type="button"
              onClick={stop}
              className="p-2 rounded-lg bg-red-500 text-white hover:bg-red-600"
            >
              <Square size={16} />
            </button>
          ) : (
            <button
              type="submit"
              disabled={!input.trim()}
              className="p-2 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Send size={16} />
            </button>
          )}
        </div>
      </form>
    </div>
  );
}
