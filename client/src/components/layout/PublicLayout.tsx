import { useState, useEffect } from "react";
import type { ReactNode } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { GraduationCap, Menu, X, Mail } from "lucide-react";
import { motion, AnimatePresence } from "motion/react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";

/** Email address used for placeholder footer links until proper static pages ship. */
const SUPPORT_EMAIL = "support@scholarpath.local";

/**
 * Builds a footer link that respects the visitor's auth state:
 *   - Logged-in users go straight to the authenticated destination.
 *   - Anonymous users go through `/login?redirect=...` so they land on the same
 *     page after signing in.
 */
function authedFooterHref(authedPath: string, isAuthed: boolean): string {
  return isAuthed
    ? authedPath
    : `/login?redirect=${encodeURIComponent(authedPath)}`;
}

export function PublicLayout({ children }: { children: ReactNode }) {
  const { t } = useTranslation(["common", "home"]);
  const [mobileOpen, setMobileOpen] = useState(false);

  // Close drawer on route change / resize to desktop
  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth >= 640) setMobileOpen(false);
    };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);

  // Prevent body scroll while drawer is open
  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
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
            <span>{t("common:appName")}</span>
          </Link>

          {/* Desktop nav */}
          <nav className="hidden items-center gap-2 sm:flex">
            <LanguageSwitcher />
            <ThemeToggle />
            <Link
              to="/login"
              className="inline-flex h-9 items-center rounded-md px-4 text-sm font-medium text-text-primary transition hover:bg-bg-subtle"
            >
              {t("common:cta.signIn")}
            </Link>
            <Link
              to="/register"
              className="cta-pill btn-brand bg-brand-500 text-white"
            >
              {t("common:cta.getStarted")}
            </Link>
          </nav>

          {/* Mobile: utility icons + hamburger */}
          <div className="flex items-center gap-1 sm:hidden">
            <LanguageSwitcher />
            <ThemeToggle />
            <button
              type="button"
              aria-label={mobileOpen ? t("common:nav.close", "Close menu") : t("common:nav.open", "Open menu")}
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
                  {t("common:cta.signIn")}
                </Link>
                <Link
                  to="/register"
                  onClick={() => setMobileOpen(false)}
                  className="flex h-11 items-center justify-center rounded-xl bg-brand-500 text-sm font-semibold text-white shadow-[0_4px_16px_rgb(23_96_240/0.3)] transition hover:bg-brand-600"
                >
                  {t("common:cta.getStarted")}
                </Link>
              </nav>
            </motion.div>
          </>
        )}
      </AnimatePresence>

      <main>{children}</main>

      <SiteFooter />
    </div>
  );
}

function SiteFooter() {
  const { t } = useTranslation(["home", "common"]);
  const isAuthed = useAuthStore((s) => s.user !== null);

  // Product links lead to authenticated areas — anonymous users go through
  // /login?redirect=… so Login.tsx restores their intent post-auth.
  // Informational pages (Privacy / Terms / Help / About / Contact) have real
  // routes under `/legal/*`, `/help`, `/about`, `/contact`. We still send a
  // handful of secondary links (Blog / Careers / Guides / Pricing) to the
  // most useful adjacent page until dedicated content lands.
  const supportMailto = `mailto:${SUPPORT_EMAIL}`;

  const columns = [
    {
      heading: t("home:footer.product.heading"),
      links: [
        {
          label: t("home:footer.product.scholarships"),
          href: authedFooterHref("/student/scholarships", isAuthed),
        },
        {
          label: t("home:footer.product.consultants"),
          href: authedFooterHref("/student/consultants", isAuthed),
        },
        {
          label: t("home:footer.product.community"),
          href: authedFooterHref("/student/community", isAuthed),
        },
        // Pricing has no public page yet — anchor to the feature pillars
        // section on the home page so the link is at least informational.
        { label: t("home:footer.product.pricing"), href: "/#pillars" },
      ],
    },
    {
      heading: t("home:footer.company.heading"),
      links: [
        { label: t("home:footer.company.about"), href: "/about" },
        { label: t("home:footer.company.blog"), href: "/about" },
        { label: t("home:footer.company.careers"), href: "/contact" },
        { label: t("home:footer.company.contact"), href: "/contact" },
      ],
    },
    {
      heading: t("home:footer.resources.heading"),
      links: [
        { label: t("home:footer.resources.help"), href: "/help" },
        { label: t("home:footer.resources.guides"), href: authedFooterHref("/student/resources", isAuthed) },
        { label: t("home:footer.resources.privacy"), href: "/legal/privacy" },
        { label: t("home:footer.resources.terms"), href: "/legal/terms" },
      ],
    },
  ];

  const socials = [
    { label: t("home:footer.connect.twitter"), href: "#", icon: TwitterIcon },
    { label: t("home:footer.connect.linkedin"), href: "#", icon: LinkedinIcon },
    { label: t("home:footer.connect.github"), href: "#", icon: GithubIcon },
    { label: t("home:footer.connect.email"), href: supportMailto, icon: Mail },
  ];

  return (
    <footer className="mt-24 border-t border-border-subtle bg-bg-subtle">
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6">
        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-[1.4fr_1fr_1fr_1fr_1fr]">
          {/* Brand */}
          <div>
            <Link to="/" className="inline-flex items-center gap-2 font-semibold">
              <GraduationCap aria-hidden className="size-5 text-brand-500" />
              <span className="text-base">{t("common:appName")}</span>
            </Link>
            <p className="mt-4 max-w-xs text-sm leading-relaxed text-text-secondary">
              {t("home:footer.tagline")}
            </p>
          </div>

          {/* Link columns */}
          {columns.map((col) => (
            <div key={col.heading}>
              <h4 className="text-sm font-semibold text-text-primary">
                {col.heading}
              </h4>
              <ul className="mt-4 space-y-2.5">
                {col.links.map((link) => (
                  <li key={link.label}>
                    <FooterLink href={link.href}>{link.label}</FooterLink>
                  </li>
                ))}
              </ul>
            </div>
          ))}

          {/* Connect */}
          <div>
            <h4 className="text-sm font-semibold text-text-primary">
              {t("home:footer.connect.heading")}
            </h4>
            <ul className="mt-4 flex flex-wrap gap-2">
              {socials.map((s) => (
                <li key={s.label}>
                  <a
                    href={s.href}
                    aria-label={s.label}
                    className="inline-flex size-9 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated text-text-secondary transition hover:border-brand-200 hover:bg-brand-50 hover:text-brand-600 dark:hover:border-brand-500/40 dark:hover:bg-brand-500/10 dark:hover:text-brand-400"
                  >
                    <s.icon aria-hidden className="size-4" />
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Bottom bar */}
        <div className="mt-12 flex flex-col items-center justify-between gap-4 border-t border-border-subtle pt-6 text-xs text-text-tertiary sm:flex-row">
          <p>
            © {new Date().getFullYear()} {t("common:appName")}. {t("home:footer.rights")}
          </p>
          <div className="flex items-center gap-4">
            <Link
              to="/legal/privacy"
              className="transition hover:text-text-secondary"
            >
              {t("home:footer.resources.privacy")}
            </Link>
            <Link
              to="/legal/terms"
              className="transition hover:text-text-secondary"
            >
              {t("home:footer.resources.terms")}
            </Link>
          </div>
        </div>
      </div>
    </footer>
  );
}

/**
 * A footer link that picks `<a>` vs react-router `<Link>` based on the href:
 *   - `mailto:`/`tel:`/`http(s)://` → native `<a>` (Link only handles internal paths)
 *   - `/foo#bar` or `#bar` → native `<a>` so the browser scrolls to the anchor
 *   - `/foo` → react-router `<Link>` for client-side navigation
 */
function FooterLink({ href, children }: { href: string; children: ReactNode }) {
  const isExternalProtocol = /^(mailto:|tel:|https?:\/\/)/.test(href);
  const isAnchor = href.startsWith("#") || href.includes("#");

  const className =
    "text-sm text-text-secondary transition hover:text-brand-600 dark:hover:text-brand-400";

  if (isExternalProtocol || isAnchor) {
    return (
      <a href={href} className={className}>
        {children}
      </a>
    );
  }
  return (
    <Link to={href} className={className}>
      {children}
    </Link>
  );
}

// ─── Brand icons (inline SVG — keeps lucide-react version untouched) ───

function TwitterIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z" />
    </svg>
  );
}

function LinkedinIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.268 2.37 4.268 5.455v6.286zM5.337 7.433a2.062 2.062 0 01-2.063-2.065 2.063 2.063 0 112.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" />
    </svg>
  );
}

function GithubIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12" />
    </svg>
  );
}
