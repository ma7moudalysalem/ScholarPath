import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";

export type ThemeMode = "light" | "dark" | "system";

interface UiState {
  theme: ThemeMode;
  sidebarCollapsed: boolean;

  setTheme: (theme: ThemeMode) => void;
  toggleSidebar: () => void;
}

export const useUiStore = create<UiState>()(
  persist(
    (set) => ({
      theme: "system",
      sidebarCollapsed: false,

      setTheme: (theme) => {
        set({ theme });
        applyTheme(theme);
      },

      toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
    }),
    {
      name: "scholarpath_ui",
      storage: createJSONStorage(() => localStorage),
    },
  ),
);

function applyTheme(theme: ThemeMode) {
  const actual =
    theme === "system"
      ? window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light"
      : theme;
  document.documentElement.setAttribute("data-theme", actual);
}

if (typeof window !== "undefined") {
  const stored = useUiStore.getState().theme;
  applyTheme(stored);

  window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", () => {
    if (useUiStore.getState().theme === "system") applyTheme("system");
  });
}
