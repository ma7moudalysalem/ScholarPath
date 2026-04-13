import { useTranslation } from "react-i18next";
import { Languages } from "lucide-react";
import { cn } from "@/lib/utils";

export function LanguageSwitcher({ className }: { className?: string }) {
  const { i18n, t } = useTranslation();
  const next = i18n.language === "ar" ? "en" : "ar";

  return (
    <button
      type="button"
      onClick={() => void i18n.changeLanguage(next)}
      aria-label={t("language.label")}
      className={cn(
        "inline-flex items-center gap-2 rounded-md border border-border-subtle bg-bg-elevated px-3 py-1.5 text-sm font-medium text-text-primary transition hover:border-border-default",
        className,
      )}
    >
      <Languages aria-hidden className="size-4" />
      {next.toUpperCase()}
    </button>
  );
}
