import { Sun, Moon, Monitor } from "lucide-react";
import { useTranslation } from "react-i18next";
import { useUiStore, type ThemeMode } from "@/stores/uiStore";
import { cn } from "@/lib/utils";

const modes: ThemeMode[] = ["light", "dark", "system"];

export function ThemeToggle({ className }: { className?: string }) {
  const { theme, setTheme } = useUiStore();
  const { t } = useTranslation();
  const next = modes[(modes.indexOf(theme) + 1) % modes.length];

  return (
    <button
      type="button"
      onClick={() => setTheme(next)}
      aria-label={t("theme.label")}
      title={`${t(`theme.${theme}`)} → ${t(`theme.${next}`)}`}
      className={cn(
        "inline-flex size-9 items-center justify-center rounded-md border border-border-subtle bg-bg-elevated text-text-primary transition hover:border-border-default",
        className,
      )}
    >
      {theme === "light" ? (
        <Sun aria-hidden className="size-4" />
      ) : theme === "dark" ? (
        <Moon aria-hidden className="size-4" />
      ) : (
        <Monitor aria-hidden className="size-4" />
      )}
    </button>
  );
}
