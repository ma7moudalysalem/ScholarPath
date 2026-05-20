import { useMemo, useState, useEffect, Suspense } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  GraduationCap,
  LayoutDashboard,
  Search,
  ListChecks,
  Bookmark,
  Users,
  Calendar,
  MessageSquare,
  BookOpen,
  FolderOpen,
  Sparkles,
  Bell,
  Settings,
  LogOut,
  Menu,
  X,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { motion, AnimatePresence } from "motion/react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";
import { postAuthPath } from "@/services/api/auth";
import { useNotificationHub } from "@/hooks/useNotificationHub";
import { notificationsApi, UNREAD_COUNT_QUERY_KEY } from "@/services/api/notifications";
import { cn } from "@/lib/utils";
import { userPhotoUrl } from "@/lib/userPhoto";

interface NavItem {
  to: string;
  key: string;
  icon: LucideIcon;
}

/**
 * Small header avatar — shows the user's profile photo, falling back to their
 * first initial if the user has no photo (the serve endpoint 404s).
 */
function HeaderAvatar({ userId, firstName }: { userId: string; firstName: string }) {
  const [failed, setFailed] = useState(false);
  const initial = firstName[0]?.toUpperCase() ?? "?";

  if (failed) {
    return (
      <div className="flex size-6 items-center justify-center rounded-full bg-brand-500 text-xs font-semibold text-text-on-brand">
        {initial}
      </div>
    );
  }

  return (
    <img
      src={userPhotoUrl(userId)}
      alt={initial}
      onError={() => setFailed(true)}
      className="size-6 rounded-full object-cover"
    />
  );
}

const NAV_BY_ROLE: Record<string, NavItem[]> = {
  Student: [
    { to: "/student", key: "student.dashboard", icon: LayoutDashboard },
    { to: "/student/scholarships", key: "student.scholarships", icon: Search },
    { to: "/student/applications", key: "student.applications", icon: ListChecks },
    { to: "/student/bookmarks", key: "student.bookmarks", icon: Bookmark },
    { to: "/student/consultants", key: "student.consultants", icon: Users },
    { to: "/student/bookings", key: "student.bookings", icon: Calendar },
    { to: "/student/community", key: "student.community", icon: MessageSquare },
    { to: "/student/resources", key: "student.resources", icon: BookOpen },
    { to: "/student/documents", key: "student.documents", icon: FolderOpen },
    { to: "/student/ai", key: "student.ai", icon: Sparkles },
    { to: "/student/messages", key: "student.messages", icon: MessageSquare },
  ],
  Company: [
    { to: "/company", key: "company.dashboard", icon: LayoutDashboard },
    { to: "/company/scholarships", key: "company.scholarships", icon: Search },
    { to: "/company/applications-review", key: "company.applicationsReview", icon: ListChecks },
    { to: "/company/billing", key: "company.billing", icon: Calendar },
  ],
  Consultant: [
    { to: "/consultant", key: "consultant.dashboard", icon: LayoutDashboard },
    { to: "/consultant/availability", key: "consultant.availability", icon: Calendar },
    { to: "/consultant/bookings", key: "consultant.bookings", icon: ListChecks },
    { to: "/consultant/earnings", key: "consultant.earnings", icon: Calendar },
  ],
};

/** Shared sidebar content — used in both the desktop sidebar and the mobile drawer */
function SidebarContent({
  navItems,
  homePath,
  onNavigate,
}: {
  navItems: NavItem[];
  homePath: string;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation(["common", "nav"]);
  const { user, clear } = useAuthStore();
  const navigate = useNavigate();
  const role = user?.activeRole ?? user?.roles[0] ?? "Student";

  const onSignOut = () => {
    clear();
    navigate("/");
    onNavigate?.();
  };

  return (
    <>
      {/* Logo */}
      <Link
        to={homePath}
        onClick={onNavigate}
        className="flex h-14 shrink-0 items-center gap-2 border-b border-border-subtle px-4 font-semibold"
      >
        <GraduationCap aria-hidden className="size-5 text-brand-500" />
        <span>{t("common:appName")}</span>
      </Link>

      {/* Nav links */}
      <nav className="flex-1 space-y-0.5 overflow-y-auto p-3">
        {navItems.map(({ to, key, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === `/${role.toLowerCase()}`}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition",
                isActive
                  ? "bg-brand-500/10 text-brand-500"
                  : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
              )
            }
          >
            <Icon aria-hidden className="size-4 shrink-0" />
            {t(`nav:${key}`)}
          </NavLink>
        ))}
      </nav>

      {/* Bottom actions */}
      <div className="space-y-0.5 border-t border-border-subtle p-3">
        <NavLink
          to="/profile"
          onClick={onNavigate}
          className="flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-text-secondary transition hover:bg-bg-subtle hover:text-text-primary"
        >
          <Settings aria-hidden className="size-4" />
          {t("nav:common.settings")}
        </NavLink>
        <button
          type="button"
          onClick={onSignOut}
          className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-text-secondary transition hover:bg-bg-subtle hover:text-text-primary"
        >
          <LogOut aria-hidden className="size-4" />
          {t("common:cta.signOut")}
        </button>
      </div>
    </>
  );
}

export function AuthenticatedLayout() {
  const { t } = useTranslation(["common", "nav"]);
  const { user } = useAuthStore();
  const role = user?.activeRole ?? user?.roles[0] ?? "Student";
  const navItems = useMemo(() => NAV_BY_ROLE[role] ?? NAV_BY_ROLE.Student, [role]);
  const homePath = user ? postAuthPath(user) : "/";

  const [drawerOpen, setDrawerOpen] = useState(false);

  // Close drawer on resize to desktop — setState inside an event handler (not
  // the effect body itself) is allowed by react-hooks/set-state-in-effect.
  // The useState setter is guaranteed stable, so no ref is needed.
  useEffect(() => {
    const onResize = () => {
      if (window.innerWidth >= 768) setDrawerOpen(false);
    };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, []);

  // Prevent body scroll while drawer open
  useEffect(() => {
    document.body.style.overflow = drawerOpen ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [drawerOpen]);

  // Subscribe to the notification hub while the user is authenticated
  useNotificationHub();

  // Unread-notification count for the header bell badge. Polled as a safety
  // net; the notification hub also invalidates this key the moment one arrives.
  const { data: unreadCount = 0 } = useQuery({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: () => notificationsApi.unreadCount(),
    enabled: !!user,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });

  return (
    <div className="flex min-h-screen bg-bg-subtle text-text-primary">

      {/* ── Desktop sidebar (hidden on mobile) ── */}
      <aside className="sticky top-0 hidden h-screen w-60 shrink-0 flex-col border-e border-border-subtle bg-bg-elevated md:flex">
        <SidebarContent navItems={navItems} homePath={homePath} />
      </aside>

      {/* ── Mobile drawer overlay ── */}
      <AnimatePresence>
        {drawerOpen && (
          <>
            <motion.div
              key="overlay"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.2 }}
              className="fixed inset-0 z-40 bg-text-primary/30 backdrop-blur-sm md:hidden"
              onClick={() => setDrawerOpen(false)}
              aria-hidden
            />
            <motion.aside
              key="drawer"
              initial={{ x: "-100%" }}
              animate={{ x: 0 }}
              exit={{ x: "-100%" }}
              transition={{ duration: 0.28, ease: [0.22, 1, 0.36, 1] }}
              className="fixed inset-y-0 start-0 z-50 flex w-72 flex-col border-e border-border-subtle bg-bg-elevated shadow-lg md:hidden"
            >
              {/* Close button */}
              <button
                type="button"
                aria-label={t("nav.close")}
                onClick={() => setDrawerOpen(false)}
                className="absolute end-3 top-3.5 flex size-8 items-center justify-center rounded-md text-text-secondary hover:bg-bg-subtle"
              >
                <X className="size-4" />
              </button>
              <SidebarContent
                navItems={navItems}
                homePath={homePath}
                onNavigate={() => setDrawerOpen(false)}
              />
            </motion.aside>
          </>
        )}
      </AnimatePresence>

      {/* ── Main content ── */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-border-subtle bg-bg-canvas/80 px-4 backdrop-blur-xl">

          {/* Hamburger — mobile only */}
          <button
            type="button"
            aria-label={t("nav.open")}
            onClick={() => setDrawerOpen(true)}
            className="me-1 inline-flex size-9 items-center justify-center rounded-md text-text-primary transition hover:bg-bg-subtle md:hidden"
          >
            <Menu className="size-5" />
          </button>

          <div className="flex flex-1 items-center justify-end gap-2">
            <LanguageSwitcher />
            <ThemeToggle />
            <NavLink
              to="/notifications"
              className="relative inline-flex size-9 items-center justify-center rounded-md border border-border-subtle bg-bg-elevated text-text-primary transition hover:border-border-default"
              aria-label={
                unreadCount > 0
                  ? `${t("nav:common.notifications")} (${unreadCount})`
                  : t("nav:common.notifications")
              }
            >
              <Bell aria-hidden className="size-4" />
              {unreadCount > 0 && (
                <span className="absolute -end-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger-500 px-1 text-[10px] font-bold leading-none text-white">
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              )}
            </NavLink>
            {user && (
              // Clickable user chip → /profile. Users naturally try to click
              // their avatar to manage their account, and the chip was a
              // dead-end div before.
              <Link
                to="/profile"
                aria-label={t("common:nav.profile", "Open profile")}
                className="ms-1 flex items-center gap-2 rounded-md bg-bg-elevated px-3 py-1.5 text-sm transition-colors hover:bg-bg-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                <HeaderAvatar userId={user.id} firstName={user.firstName} />
                <span className="hidden font-medium sm:inline">{user.fullName}</span>
              </Link>
            )}
          </div>
        </header>

        <main className="flex-1 p-4 sm:p-6">
          <Suspense
            fallback={
              <div className="flex min-h-[60vh] items-center justify-center">
                <div className="size-7 animate-spin rounded-full border-2 border-border-subtle border-t-brand-500" />
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  );
}
