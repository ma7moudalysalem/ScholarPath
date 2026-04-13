import { useMemo } from "react";
import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
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
  Sparkles,
  Bell,
  Settings,
  LogOut,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";
import { useNotificationHub } from "@/hooks/useNotificationHub";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  key: string;
  icon: LucideIcon;
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

export function AuthenticatedLayout() {
  const { t } = useTranslation(["common", "nav"]);
  const navigate = useNavigate();
  const { user, clear } = useAuthStore();
  const role = user?.activeRole ?? user?.roles[0] ?? "Student";
  const navItems = useMemo(() => NAV_BY_ROLE[role] ?? NAV_BY_ROLE.Student, [role]);

  // Subscribe to the notification hub while the user is authenticated
  useNotificationHub();

  const onSignOut = () => {
    clear();
    navigate("/");
  };

  return (
    <div className="flex min-h-screen bg-bg-subtle text-text-primary">
      <aside className="sticky top-0 flex h-screen w-60 shrink-0 flex-col border-e border-border-subtle bg-bg-elevated">
        <Link to="/" className="flex h-14 items-center gap-2 border-b border-border-subtle px-4 font-semibold">
          <GraduationCap aria-hidden className="size-5 text-brand-500" />
          <span>{t("common:appName")}</span>
        </Link>

        <nav className="flex-1 space-y-0.5 overflow-y-auto p-3">
          {navItems.map(({ to, key, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === `/${role.toLowerCase()}`}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition",
                  isActive
                    ? "bg-brand-500/10 text-brand-500"
                    : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
                )
              }
            >
              <Icon aria-hidden className="size-4" />
              {t(`nav:${key}`)}
            </NavLink>
          ))}
        </nav>

        <div className="space-y-0.5 border-t border-border-subtle p-3">
          <NavLink
            to="/profile"
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
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center justify-end gap-2 border-b border-border-subtle bg-bg-canvas/80 px-4 backdrop-blur-xl">
          <LanguageSwitcher />
          <ThemeToggle />
          <NavLink
            to="/notifications"
            className="inline-flex size-9 items-center justify-center rounded-md border border-border-subtle bg-bg-elevated text-text-primary transition hover:border-border-default"
            aria-label={t("nav:common.notifications")}
          >
            <Bell aria-hidden className="size-4" />
          </NavLink>
          {user && (
            <div className="ml-2 flex items-center gap-2 rounded-md bg-bg-elevated px-3 py-1.5 text-sm">
              <div className="flex size-6 items-center justify-center rounded-full bg-brand-500 text-xs font-semibold text-text-on-brand">
                {user.firstName[0]}
              </div>
              <span className="font-medium">{user.fullName}</span>
            </div>
          )}
        </header>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
