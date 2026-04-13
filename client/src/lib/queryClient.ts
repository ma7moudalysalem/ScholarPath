import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      gcTime: 5 * 60_000,
      retry: (failureCount, error: unknown) => {
        if (typeof error === "object" && error !== null && "status" in error) {
          const status = (error as { status: number }).status;
          if (status === 401 || status === 403 || status === 404) return false;
        }
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
});

export const queryKeys = {
  auth: {
    me: ["auth", "me"] as const,
  },
  scholarships: {
    all: ["scholarships"] as const,
    list: (filters: Record<string, unknown>) => ["scholarships", "list", filters] as const,
    detail: (id: string) => ["scholarships", "detail", id] as const,
    bookmarks: ["scholarships", "bookmarks"] as const,
  },
  applications: {
    all: ["applications"] as const,
    mine: ["applications", "mine"] as const,
    detail: (id: string) => ["applications", "detail", id] as const,
  },
  bookings: {
    all: ["bookings"] as const,
    mine: ["bookings", "mine"] as const,
  },
  consultants: {
    directory: (filters: Record<string, unknown>) => ["consultants", "directory", filters] as const,
    detail: (id: string) => ["consultants", "detail", id] as const,
  },
  notifications: {
    mine: ["notifications", "mine"] as const,
    unreadCount: ["notifications", "unreadCount"] as const,
  },
  resources: {
    list: (filters: Record<string, unknown>) => ["resources", "list", filters] as const,
    detail: (id: string) => ["resources", "detail", id] as const,
  },
  admin: {
    users: (filters: Record<string, unknown>) => ["admin", "users", filters] as const,
    onboardingQueue: ["admin", "onboardingQueue"] as const,
  },
} as const;
