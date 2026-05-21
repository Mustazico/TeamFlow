import { create } from 'zustand';

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  toolCalls?: ToolCallStatus[];
  proposal?: ActionProposal;
  timestamp: number;
}

export interface ToolCallStatus {
  name: string;
  status: 'running' | 'done' | 'error';
  result?: string;
}

export interface ActionProposal {
  action: string;
  parameters: Record<string, unknown>;
  status: 'pending' | 'confirmed' | 'cancelled';
}

interface AgentState {
  isOpen: boolean;
  messages: ChatMessage[];
  isStreaming: boolean;
  toggle: () => void;
  open: () => void;
  close: () => void;
  addUserMessage: (content: string) => string;
  startAssistantMessage: () => string;
  appendToAssistant: (id: string, content: string) => void;
  addToolCall: (msgId: string, toolCall: ToolCallStatus) => void;
  updateToolCall: (msgId: string, name: string, status: ToolCallStatus['status'], result?: string) => void;
  setProposal: (msgId: string, proposal: ActionProposal) => void;
  updateProposalStatus: (msgId: string, status: ActionProposal['status']) => void;
  setStreaming: (v: boolean) => void;
  clear: () => void;
}

let nextId = 0;
const genId = () => `msg-${Date.now()}-${nextId++}`;

export const useAgentStore = create<AgentState>((set) => ({
  isOpen: false,
  messages: [],
  isStreaming: false,

  toggle: () => set((s) => ({ isOpen: !s.isOpen })),
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),

  addUserMessage: (content) => {
    const id = genId();
    set((s) => ({
      messages: [...s.messages, { id, role: 'user', content, timestamp: Date.now() }],
    }));
    return id;
  },

  startAssistantMessage: () => {
    const id = genId();
    set((s) => ({
      messages: [...s.messages, { id, role: 'assistant', content: '', toolCalls: [], timestamp: Date.now() }],
    }));
    return id;
  },

  appendToAssistant: (id, content) => {
    set((s) => ({
      messages: s.messages.map((m) => (m.id === id ? { ...m, content: m.content + content } : m)),
    }));
  },

  addToolCall: (msgId, toolCall) => {
    set((s) => ({
      messages: s.messages.map((m) =>
        m.id === msgId ? { ...m, toolCalls: [...(m.toolCalls ?? []), toolCall] } : m,
      ),
    }));
  },

  updateToolCall: (msgId, name, status, result) => {
    set((s) => ({
      messages: s.messages.map((m) =>
        m.id === msgId
          ? {
              ...m,
              toolCalls: (m.toolCalls ?? []).map((tc) =>
                tc.name === name ? { ...tc, status, result: result ?? tc.result } : tc,
              ),
            }
          : m,
      ),
    }));
  },

  setProposal: (msgId, proposal) => {
    set((s) => ({
      messages: s.messages.map((m) => (m.id === msgId ? { ...m, proposal } : m)),
    }));
  },

  updateProposalStatus: (msgId, status) => {
    set((s) => ({
      messages: s.messages.map((m) =>
        m.id === msgId && m.proposal ? { ...m, proposal: { ...m.proposal, status } } : m,
      ),
    }));
  },

  setStreaming: (v) => set({ isStreaming: v }),

  clear: () => set({ messages: [], isStreaming: false }),
}));
