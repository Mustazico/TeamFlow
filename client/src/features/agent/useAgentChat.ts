import { useCallback, useRef } from 'react';
import { useAgentStore } from '@/stores/agentStore';
import { useAuthStore } from '@/stores/authStore';
import { useActionModalStore } from '@/stores/actionModalStore';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';

export function useAgentChat() {
  const {
    messages,
    isStreaming,
    addUserMessage,
    startAssistantMessage,
    appendToAssistant,
    addToolCall,
    updateToolCall,
    setProposal,
    updateProposalStatus,
    setStreaming,
  } = useAgentStore();

  const abortRef = useRef<AbortController | null>(null);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { openCreateProject, openCreateTask } = useActionModalStore();

  const sendMessage = useCallback(
    async (content: string) => {
      if (!content.trim() || isStreaming) return;

      addUserMessage(content);
      setStreaming(true);
      const assistantId = startAssistantMessage();

      const allMessages = [
        ...useAgentStore.getState().messages.slice(0, -1).map((m) => ({
          role: m.role,
          content: m.content,
        })),
        { role: 'user' as const, content },
      ];

      abortRef.current = new AbortController();

      try {
        const token = useAuthStore.getState().accessToken;
        const res = await fetch('/api/agent/chat', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({ messages: allMessages }),
          signal: abortRef.current.signal,
        });

        if (!res.ok) {
          appendToAssistant(assistantId, 'Sorry, something went wrong. Please try again.');
          setStreaming(false);
          return;
        }

        const reader = res.body?.getReader();
        if (!reader) {
          setStreaming(false);
          return;
        }

        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            if (!line.startsWith('data: ')) continue;
            const json = line.slice(6);
            try {
              const event = JSON.parse(json);
              switch (event.type) {
                case 'delta':
                  appendToAssistant(assistantId, event.content);
                  break;
                case 'tool':
                  if (event.status === 'running') {
                    addToolCall(assistantId, { name: event.name, status: 'running' });
                  } else {
                    updateToolCall(assistantId, event.name, event.status, event.result);
                    invalidateQueries(event.name);
                  }
                  break;
                case 'proposal': {
                  const params = typeof event.parameters === 'string'
                    ? JSON.parse(event.parameters)
                    : event.parameters;
                  handleProposal(event.action, params, assistantId);
                  break;
                }
                case 'error':
                  appendToAssistant(assistantId, event.message ?? 'An error occurred.');
                  break;
                case 'done':
                  break;
              }
            } catch {
              // skip malformed events
            }
          }
        }
      } catch (err) {
        if ((err as Error).name !== 'AbortError') {
          appendToAssistant(assistantId, 'Connection error. Please try again.');
        }
      } finally {
        setStreaming(false);
      }
    },
    [isStreaming, addUserMessage, startAssistantMessage, appendToAssistant, addToolCall, updateToolCall, setProposal, setStreaming],
  );

  const handleProposal = useCallback(
    (action: string, params: Record<string, unknown>, msgId: string) => {
      switch (action) {
        case 'create_project':
          navigate('/projects');
          openCreateProject({
            name: (params.name as string) ?? '',
            description: (params.description as string) ?? undefined,
            color: (params.color as string) ?? undefined,
          });
          break;
        case 'create_task':
          navigate(`/projects/${params.projectId}`);
          // Small delay to allow the page to mount before triggering the modal
          setTimeout(() => {
            openCreateTask({
              projectId: (params.projectId as string) ?? '',
              title: (params.title as string) ?? '',
              description: (params.description as string) ?? undefined,
              priority: (params.priority as string) ?? undefined,
              assigneeId: (params.assigneeId as string) ?? undefined,
              dueDate: (params.dueDate as string) ?? undefined,
            });
          }, 100);
          break;
        default:
          // For update_task, move_task, add_comment — show inline proposal card
          setProposal(msgId, {
            action,
            parameters: params,
            status: 'pending',
          });
          break;
      }
    },
    [navigate, openCreateProject, openCreateTask, setProposal],
  );

  const confirmProposal = useCallback(
    async (msgId: string, action: string, parameters: Record<string, unknown>) => {
      try {
        const token = useAuthStore.getState().accessToken;
        const res = await fetch('/api/agent/confirm', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({ action, parametersJson: JSON.stringify(parameters) }),
        });

        if (res.ok) {
          updateProposalStatus(msgId, 'confirmed');
          invalidateQueries(action);
        } else {
          updateProposalStatus(msgId, 'cancelled');
        }
      } catch {
        updateProposalStatus(msgId, 'cancelled');
      }
    },
    [updateProposalStatus],
  );

  const cancelProposal = useCallback(
    (msgId: string) => {
      updateProposalStatus(msgId, 'cancelled');
    },
    [updateProposalStatus],
  );

  const stop = useCallback(() => {
    abortRef.current?.abort();
    setStreaming(false);
  }, [setStreaming]);

  const invalidateQueries = (toolName: string) => {
    switch (toolName) {
      case 'create_task':
      case 'update_task':
      case 'move_task':
        queryClient.invalidateQueries({ queryKey: ['tasks'] });
        queryClient.invalidateQueries({ queryKey: ['dashboard'] });
        break;
      case 'create_project':
        queryClient.invalidateQueries({ queryKey: ['projects'] });
        queryClient.invalidateQueries({ queryKey: ['dashboard'] });
        break;
      case 'add_comment':
        queryClient.invalidateQueries({ queryKey: ['comments'] });
        break;
    }
  };

  return { messages, isStreaming, sendMessage, stop, confirmProposal, cancelProposal };
}
