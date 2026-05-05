import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { UserDto } from '@/types';
import { UserRole } from '@/types';

interface AuthState {
  user: UserDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

interface AuthActions {
  setAuth: (user: UserDto) => void;
  logout: () => void;
  setUser: (user: UserDto) => void;
  setLoading: (isLoading: boolean) => void;
}

type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      ...initialState,

      setAuth: (user) =>
        set({
          user,
          isAuthenticated: true,
          isLoading: false,
        }),

      logout: () => set({ ...initialState }),

      setUser: (user) => set({ user }),

      setLoading: (isLoading) => set({ isLoading }),
    }),
    {
      name: 'scholarpath-auth',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// Selectors
export const selectIsAdmin = (state: AuthStore) => state.user?.role === UserRole.Admin;
export const selectRole = (state: AuthStore) => state.user?.role ?? null;
export const selectIsOnboarded = (state: AuthStore) => state.user?.isOnboardingComplete ?? false;
