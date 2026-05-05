import { create } from 'zustand';

type AuthModalView = 'login' | 'register' | 'forgotPassword' | null;

interface AuthModalState {
  view: AuthModalView;
  open: (view: AuthModalView) => void;
  close: () => void;
  switchTo: (view: AuthModalView) => void;
}

export const useAuthModal = create<AuthModalState>((set) => ({
  view: null,
  open: (view) => set({ view }),
  close: () => set({ view: null }),
  switchTo: (view) => set({ view }),
}));
