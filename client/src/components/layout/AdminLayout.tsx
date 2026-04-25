import { Link, NavLink, Outlet, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import {
  ShieldCheck,
  LayoutDashboard,
  Users,
  UserCheck,
  UserPlus,
  BarChart3,
  Sparkles,
  ShieldAlert,
  Megaphone,
  FileText,
  LogOut,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  key: string;
  icon: LucideIcon;
  end?: boolean;
}

const NAV: NavItem[] = [
  { to: "/admin", key: "nav.dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/users", key: "nav.users", icon: Users },
  { to: "/admin/onboarding", key: "nav.onboarding", icon: UserCheck },
  { to: "/admin/upgrades", key: "nav.upgrades", icon: UserPlus },
  { to: "/admin/analytics", key: "nav.analytics", icon: BarChart3 },
  { to: "/admin/ai-economy", key: "nav.aiEconomy", icon: Sparkles },
  { to: "/admin/redaction-audit", key: "nav.redactionAudit", icon: ShieldAlert },
  { to: "/admin/broadcast", key: "nav.broadcast", icon: Megaphone },
  { to: "/admin/audit-log", key: "nav.auditLog", icon: FileText },
];

export function AdminLayout() {
  const { t } = useTranslation(["admin", "common"]);
  const navigate = useNavigate();
  const { user, clear } = useAuthStore();

  const onSignOut = () => {
    clear();
    navigate("/");
  };

  return (
    <div className="flex min-h-screen bg-bg-subtle text-text-primary">
      <aside className="sticky top-0 flex h-screen w-64 shrink-0 flex-col border-e border-border-subtle bg-bg-elevated">
        <Link to="/admin" className="flex h-14 items-center gap-2 border-b border-border-subtle px-4 font-semibold">
          <ShieldCheck aria-hidden className="size-5 text-brand-500" />
          <span>{t("admin:title")}</span>
        </Link>

        <nav className="flex-1 space-y-0.5 overflow-y-auto p-3">
          {NAV.map(({ to, key, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
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
              {t(`admin:${key}`)}
            </NavLink>
          ))}
        </nav>

        <div className="space-y-0.5 border-t border-border-subtle p-3">
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
