import type { ReactNode } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap } from "lucide-react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";

export function PublicLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  return (
    <div className="min-h-screen bg-bg-canvas text-text-primary">
      <header className="sticky top-0 z-40 border-b border-border-subtle/60 bg-bg-canvas/80 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:px-6">
          <Link to="/" className="inline-flex items-center gap-2 font-semibold">
            <GraduationCap aria-hidden className="size-5 text-brand-500" />
            <span>{t("appName")}</span>
          </Link>

          <nav className="flex items-center gap-2">
            <LanguageSwitcher />
            <ThemeToggle />
            <Link
              to="/login"
              className="inline-flex h-9 items-center rounded-md px-4 text-sm font-medium text-text-primary transition hover:bg-bg-subtle"
            >
              {t("cta.signIn")}
            </Link>
            <Link
              to="/register"
              className="cta-pill bg-text-primary text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
            >
              {t("cta.getStarted")}
            </Link>
          </nav>
        </div>
      </header>

      <main>{children}</main>

      <footer className="mt-24 border-t border-border-subtle py-10 text-sm text-text-tertiary">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6">
          <p>
            © {new Date().getFullYear()} {t("appName")}
          </p>
          <p>{t("tagline")}</p>
        </div>
      </footer>
    </div>
  );
}
