import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { UserDto } from '@/types';
import { UserRole } from '@/types';

interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isSessionExpired: boolean;
}

interface AuthActions {
  setAuth: (user: UserDto, accessToken: string, refreshToken: string) => void;
  logout: () => void;
  updateTokens: (accessToken: string, refreshToken: string) => void;
  setUser: (user: UserDto) => void;
  setLoading: (isLoading: boolean) => void;
  setSessionExpired: (expired: boolean) => void;
}

type AuthStore = AuthState & AuthActions;

const initialState: AuthState = {
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  isSessionExpired: false,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      ...initialState,

      setAuth: (user, accessToken, refreshToken) =>
        set({
          user,
          accessToken,
          refreshToken,
          isAuthenticated: true,
          isLoading: false,
          isSessionExpired: false, // Clear expiry flag on fresh auth
        }),

      logout: () => set({ ...initialState }),

      updateTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),

      setUser: (user) => set({ user }),

      setLoading: (isLoading) => set({ isLoading }),

      setSessionExpired: (expired) => set({ isSessionExpired: expired }),
    }),
    {
      name: 'scholarpath-auth',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// Selectors
export const selectIsAdmin = (state: AuthStore) => state.user?.role === UserRole.Admin;
export const selectRole = (state: AuthStore) => state.user?.role ?? null;
export const selectIsOnboarded = (state: AuthStore) => state.user?.isOnboardingComplete ?? false;
