import { useState, useEffect } from "react";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap, Menu, X } from "lucide-react";
import { motion, AnimatePresence } from "motion/react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";

export function PublicLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const [mobileOpen, setMobileOpen] = useState(false);

  // Close drawer on route change / resize to desktop
  useEffect(() => {
    const onResize = () => { if (window.innerWidth >= 640) setMobileOpen(false); };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);

  // Prevent body scroll while drawer is open
  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [mobileOpen]);

  return (
    <div className="min-h-screen bg-bg-canvas text-text-primary">
      <header className="sticky top-0 z-40 border-b border-border-subtle/60 bg-bg-canvas/80 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:px-6">
          {/* Logo */}
          <Link
            to="/"
            className="inline-flex items-center gap-2 font-semibold"
            onClick={() => setMobileOpen(false)}
          >
            <GraduationCap aria-hidden className="size-5 text-brand-500" />
            <span>{t("appName")}</span>
          </Link>

          {/* Desktop nav */}
          <nav className="hidden items-center gap-2 sm:flex">
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
              className="cta-pill btn-brand bg-brand-500 text-white"
            >
              {t("cta.getStarted")}
            </Link>
          </nav>

          {/* Mobile: utility icons + hamburger */}
          <div className="flex items-center gap-1 sm:hidden">
            <LanguageSwitcher />
            <ThemeToggle />
            <button
              type="button"
              aria-label={mobileOpen ? t("nav.close") : t("nav.open")}
              aria-expanded={mobileOpen}
              onClick={() => setMobileOpen((v) => !v)}
              className="inline-flex size-9 items-center justify-center rounded-md text-text-primary transition hover:bg-bg-subtle"
            >
              {mobileOpen ? <X className="size-5" /> : <Menu className="size-5" />}
            </button>
          </div>
        </div>
      </header>

      {/* Mobile drawer */}
      <AnimatePresence>
        {mobileOpen && (
          <>
            {/* Backdrop */}
            <motion.div
              key="backdrop"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2 }}
              className="fixed inset-0 z-30 bg-text-primary/20 backdrop-blur-sm sm:hidden"
              onClick={() => setMobileOpen(false)}
              aria-hidden
            />

            {/* Drawer panel */}
            <motion.div
              key="drawer"
              initial={{ opacity: 0, y: -8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.22, ease: [0.22, 1, 0.36, 1] }}
              className="fixed inset-x-0 top-14 z-40 border-b border-border-subtle bg-bg-elevated px-4 pb-5 pt-4 shadow-lg sm:hidden"
            >
              <nav className="flex flex-col gap-2">
                <Link
                  to="/login"
                  onClick={() => setMobileOpen(false)}
                  className="flex h-11 items-center rounded-xl px-4 text-sm font-medium text-text-primary transition hover:bg-bg-subtle"
                >
                  {t("cta.signIn")}
                </Link>
                <Link
                  to="/register"
                  onClick={() => setMobileOpen(false)}
                  className="flex h-11 items-center justify-center rounded-xl bg-brand-500 text-sm font-semibold text-white shadow-[0_4px_16px_rgb(23_96_240/0.3)] transition hover:bg-brand-600"
                >
                  {t("cta.getStarted")}
                </Link>
              </nav>
            </motion.div>
          </>
        )}
      </AnimatePresence>

      <main>{children}</main>

      <footer className="mt-24 border-t border-border-subtle py-10 text-sm text-text-tertiary">
        <div className="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-4 sm:flex-row sm:px-6">
          <p>© {new Date().getFullYear()} {t("appName")}</p>
          <p>{t("tagline")}</p>
        </div>
      </footer>
    </div>
  );
}
