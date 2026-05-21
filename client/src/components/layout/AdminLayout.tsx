import { Suspense } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "motion/react";
import {
  ShieldCheck,
  LayoutDashboard,
  Users,
  UserCheck,
  UserPlus,
  BarChart3,
  Sparkles,
  Database,
  ShieldAlert,
  Megaphone,
  FileText,
  Coins,
  LogOut,
  GraduationCap,
  Star,
  BookOpen,
  MessageSquare,
  CreditCard,
  PieChart,
  Settings,
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
  /** If true, renders a thin horizontal divider before this item. */
  divider?: boolean;
}

const NAV: NavItem[] = [
  // Core
  { to: "/admin", key: "nav.dashboard", icon: LayoutDashboard, end: true },
  { to: "/admin/users", key: "nav.users", icon: Users },
  { to: "/admin/onboarding", key: "nav.onboarding", icon: UserCheck },
  { to: "/admin/upgrades", key: "nav.upgrades", icon: UserPlus },
  // Content
  { to: "/admin/scholarships",          key: "nav.scholarships",          icon: GraduationCap, divider: true },
  { to: "/admin/featured-scholarships", key: "nav.featuredScholarships",  icon: Star },
  { to: "/admin/articles", key: "nav.articles", icon: BookOpen },
  { to: "/admin/community", key: "nav.community", icon: MessageSquare },
  // Finance
  { to: "/admin/payments", key: "nav.payments", icon: CreditCard, divider: true },
  { to: "/admin/profit-share", key: "nav.profitShare", icon: PieChart },
  { to: "/admin/financial-config", key: "nav.financialConfig", icon: Coins },
  // Intelligence
  { to: "/admin/analytics", key: "nav.analytics", icon: BarChart3, divider: true },
  { to: "/admin/ai-economy", key: "nav.aiEconomy", icon: Sparkles },
  { to: "/admin/knowledge-base", key: "nav.knowledgeBase", icon: Database },
  { to: "/admin/redaction-audit", key: "nav.redactionAudit", icon: ShieldAlert },
  // System
  { to: "/admin/broadcast", key: "nav.broadcast", icon: Megaphone, divider: true },
  { to: "/admin/audit-log", key: "nav.auditLog", icon: FileText },
  { to: "/admin/settings", key: "nav.settings", icon: Settings },
];

export function AdminLayout() {
  const { t } = useTranslation(["admin", "common"]);
  const navigate = useNavigate();
  const { user, clear } = useAuthStore();
  const location = useLocation();

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

        <nav className="flex-1 overflow-y-auto p-3">
          {NAV.map(({ to, key, icon: Icon, end, divider }, idx) => (
            <motion.div
              key={to}
              initial={{ opacity: 0, x: -8 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{
                duration: 0.28,
                ease: [0.22, 1, 0.36, 1],
                delay: Math.min(idx, 12) * 0.03,
              }}
            >
              {divider && (
                <div className="my-1.5 border-t border-border-subtle" />
              )}
              <NavLink
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
            </motion.div>
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
            <div className="ms-2 flex items-center gap-2 rounded-md bg-bg-elevated px-3 py-1.5 text-sm">
              <div className="flex size-6 items-center justify-center rounded-full bg-brand-500 text-xs font-semibold text-text-on-brand">
                {user.firstName[0]}
              </div>
              <span className="font-medium">{user.fullName}</span>
            </div>
          )}
        </header>

        <main className="flex-1 p-6">
          <Suspense
            fallback={
              <div className="flex min-h-[60vh] items-center justify-center">
                <div className="size-7 animate-spin rounded-full border-2 border-border-subtle border-t-brand-500" />
              </div>
            }
          >
            <AnimatePresence mode="wait" initial={false}>
              <motion.div
                key={location.pathname}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
              >
                <Outlet />
              </motion.div>
            </AnimatePresence>
          </Suspense>
        </main>
      </div>
    </div>
  );
}
