import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import i18n from '@/i18n';

type Language = 'en' | 'ar';
type Direction = 'ltr' | 'rtl';
type ThemeMode = 'light' | 'dark';

interface UiState {
  language: Language;
  direction: Direction;
  sidebarOpen: boolean;
  themeMode: ThemeMode;
  designTheme: number;
}

interface UiActions {
  toggleLanguage: () => void;
  setLanguage: (lang: Language) => void;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  toggleTheme: () => void;
  setThemeMode: (mode: ThemeMode) => void;
  setDesignTheme: (id: number) => void;
}

type UiStore = UiState & UiActions;

const langToDir: Record<Language, Direction> = {
  en: 'ltr',
  ar: 'rtl',
};

function applyDirection(dir: Direction) {
  document.documentElement.setAttribute('dir', dir);
  document.documentElement.setAttribute('lang', dir === 'rtl' ? 'ar' : 'en');
}

export const useUiStore = create<UiStore>()(
  persist(
    (set, get) => ({
      language: 'en',
      direction: 'ltr',
      sidebarOpen: true,
      themeMode: 'light',
      designTheme: 0,

      toggleLanguage: () => {
        const newLang: Language = get().language === 'en' ? 'ar' : 'en';
        const newDir = langToDir[newLang];
        i18n.changeLanguage(newLang);
        applyDirection(newDir);
        set({ language: newLang, direction: newDir });
      },

      setLanguage: (lang) => {
        const dir = langToDir[lang];
        i18n.changeLanguage(lang);
        applyDirection(dir);
        set({ language: lang, direction: dir });
      },

      toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),

      setSidebarOpen: (open) => set({ sidebarOpen: open }),

      toggleTheme: () => set((s) => ({ themeMode: s.themeMode === 'light' ? 'dark' : 'light' })),

      setThemeMode: (mode) => set({ themeMode: mode }),

      setDesignTheme: (id) => set({ designTheme: id }),
    }),
    {
      name: 'scholarpath-ui',
      partialize: (state) => ({
        language: state.language,
        direction: state.direction,
        themeMode: state.themeMode,
        designTheme: state.designTheme,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          applyDirection(state.direction);
          i18n.changeLanguage(state.language);
        }
      },
    }
  )
);
