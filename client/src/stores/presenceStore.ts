import { create } from "zustand";

/**
 * Live chat presence — the set of user ids currently holding at least one
 * SignalR connection to the chat hub. Ephemeral by design: it is seeded from
 * the hub's `GetOnlineUsers` on connect, kept fresh by `UserOnline` /
 * `UserOffline` events, and cleared when the connection drops. Never persisted.
 */
interface PresenceState {
  onlineUserIds: Set<string>;
  /** Replaces the whole set — used to seed from the hub's GetOnlineUsers. */
  setOnlineUsers: (userIds: string[]) => void;
  /** Marks a single user online (a `UserOnline` event). */
  markOnline: (userId: string) => void;
  /** Marks a single user offline (a `UserOffline` event). */
  markOffline: (userId: string) => void;
  /** Clears all presence — on hub disconnect / sign-out. */
  reset: () => void;
}

export const usePresenceStore = create<PresenceState>((set) => ({
  onlineUserIds: new Set<string>(),

  setOnlineUsers: (userIds) => set({ onlineUserIds: new Set(userIds) }),

  markOnline: (userId) =>
    set((state) => {
      if (state.onlineUserIds.has(userId)) return state;
      const next = new Set(state.onlineUserIds);
      next.add(userId);
      return { onlineUserIds: next };
    }),

  markOffline: (userId) =>
    set((state) => {
      if (!state.onlineUserIds.has(userId)) return state;
      const next = new Set(state.onlineUserIds);
      next.delete(userId);
      return { onlineUserIds: next };
    }),

  reset: () => set({ onlineUserIds: new Set<string>() }),
}));

/**
 * Subscribes to a single user's online state. Re-renders the caller only when
 * that specific user's presence flips, not on every presence change.
 */
export function useIsUserOnline(userId: string | undefined): boolean {
  return usePresenceStore((state) => (userId ? state.onlineUserIds.has(userId) : false));
}
