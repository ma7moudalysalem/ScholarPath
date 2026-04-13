import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl: string | null;
  accountStatus: "Unassigned" | "PendingApproval" | "Active" | "Suspended" | "Deactivated";
  isOnboardingComplete: boolean;
  roles: string[];
  activeRole: string | null;
  preferredLanguage: string | null;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface AuthState {
  user: CurrentUser | null;
  tokens: AuthTokens | null;
  isHydrated: boolean;

  setSession: (payload: { user: CurrentUser; tokens: AuthTokens }) => void;
  setUser: (user: CurrentUser) => void;
  setTokens: (tokens: AuthTokens) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      tokens: null,
      isHydrated: false,

      setSession: (payload) => set({ user: payload.user, tokens: payload.tokens }),
      setUser: (user) => set({ user }),
      setTokens: (tokens) => set({ tokens }),
      clear: () => set({ user: null, tokens: null }),
    }),
    {
      name: "scholarpath_auth",
      storage: createJSONStorage(() => localStorage),
      onRehydrateStorage: () => (state) => {
        if (state) state.isHydrated = true;
      },
      partialize: (state) => ({ user: state.user, tokens: state.tokens }),
    },
  ),
);
