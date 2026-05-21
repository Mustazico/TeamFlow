import { create } from 'zustand';

export interface PrefillCreateProject {
  name: string;
  description?: string;
  color?: string;
}

export interface PrefillCreateTask {
  projectId: string;
  title: string;
  description?: string;
  priority?: string;
  assigneeId?: string;
  dueDate?: string;
}

interface ActionModalState {
  createProject: { open: boolean; prefill?: PrefillCreateProject };
  createTask: { open: boolean; prefill?: PrefillCreateTask };

  openCreateProject: (prefill: PrefillCreateProject) => void;
  closeCreateProject: () => void;
  openCreateTask: (prefill: PrefillCreateTask) => void;
  closeCreateTask: () => void;
}

export const useActionModalStore = create<ActionModalState>((set) => ({
  createProject: { open: false },
  createTask: { open: false },

  openCreateProject: (prefill) => set({ createProject: { open: true, prefill } }),
  closeCreateProject: () => set({ createProject: { open: false, prefill: undefined } }),
  openCreateTask: (prefill) => set({ createTask: { open: true, prefill } }),
  closeCreateTask: () => set({ createTask: { open: false, prefill: undefined } }),
}));
