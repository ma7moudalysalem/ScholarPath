import { Suspense } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
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
  AlertTriangle,
  Clock,
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
  Wallet,
  Bell,
  ChevronDown,
  User as UserIcon,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";
import { useNotificationHub } from "@/hooks/useNotificationHub";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { notificationsApi, UNREAD_COUNT_QUERY_KEY } from "@/services/api/notifications";
import { cn } from "@/lib/utils";

interface NavItem {
  to: string;
  key: string;
  icon: LucideIcon;
  end?: boolean;
  /** If true, renders a thin horizontal divider before this item. */
  divider?: boolean;
  /** Hidden when the platform runs in free mode (payments.enabled=false). */
  paymentGated?: boolean;
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
  { to: "/admin/low-rated-companies", key: "nav.lowRatedCompanies", icon: AlertTriangle },
  { to: "/admin/no-show-reports", key: "nav.noShowReports", icon: Clock },
  // Finance — hidden entirely in free mode (payments.enabled=false)
  { to: "/admin/payments", key: "nav.payments", icon: CreditCard, divider: true, paymentGated: true },
  { to: "/admin/profit-share", key: "nav.profitShare", icon: PieChart, paymentGated: true },
  { to: "/admin/financial-config", key: "nav.financialConfig", icon: Coins, paymentGated: true },
  // Intelligence
  { to: "/admin/analytics", key: "nav.analytics", icon: BarChart3, divider: true },
  { to: "/admin/reports/revenue", key: "nav.revenueReport", icon: Wallet, paymentGated: true },
  { to: "/admin/ai-economy", key: "nav.aiEconomy", icon: Sparkles },
  { to: "/admin/knowledge-base", key: "nav.knowledgeBase", icon: Database },
  { to: "/admin/redaction-audit", key: "nav.redactionAudit", icon: ShieldAlert },
  // System
  { to: "/admin/broadcast", key: "nav.broadcast", icon: Megaphone, divider: true },
  { to: "/admin/audit-log", key: "nav.auditLog", icon: FileText },
  { to: "/admin/settings", key: "nav.settings", icon: Settings },
];

export function AdminLayout() {
  const { t, i18n } = useTranslation(["admin", "common", "nav"]);
  const navigate = useNavigate();
  const { user, clear } = useAuthStore();
  const location = useLocation();
  const isRtl = i18n.language.startsWith("ar");

  // Free mode: hide every payment/revenue nav entry (Finance section + revenue
  // report). The payments.enabled toggle itself lives under /admin/settings,
  // which stays reachable.
  const paymentsEnabled = usePaymentsEnabled();
  const navItems = NAV.filter((i) => paymentsEnabled || !i.paymentGated);

  // Same realtime + polling pair the AuthenticatedLayout uses — admins on
  // /admin/* now get a live unread-count badge and a Bell shortcut to the
  // notifications page.
  useNotificationHub();
  const { data: unreadCount = 0 } = useQuery({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: () => notificationsApi.unreadCount(),
    enabled: !!user,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });

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
          {navItems.map(({ to, key, icon: Icon, end, divider }, idx) => (
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
          <NavLink
            to="/notifications"
            aria-label={
              unreadCount > 0
                ? `${t("nav:common.notifications")} (${unreadCount})`
                : t("nav:common.notifications")
            }
            className="relative inline-flex size-9 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated text-text-primary transition hover:bg-bg-subtle"
          >
            <Bell aria-hidden className="size-4" />
            {unreadCount > 0 && (
              <span className="absolute -end-1 -top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger-500 px-1 text-[10px] font-bold leading-none text-white">
                {unreadCount > 9 ? "9+" : unreadCount}
              </span>
            )}
          </NavLink>
          {user && (
            <DropdownMenu.Root dir={isRtl ? "rtl" : "ltr"}>
              <DropdownMenu.Trigger asChild>
                <button
                  type="button"
                  aria-label={t("common:nav.profile", "Open profile menu")}
                  className="ms-2 flex items-center gap-2 rounded-md bg-bg-elevated px-3 py-1.5 text-sm transition-colors hover:bg-bg-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                >
                  <span className="flex size-6 items-center justify-center rounded-full bg-brand-500 text-xs font-semibold text-text-on-brand">
                    {user.firstName[0]}
                  </span>
                  <span className="font-medium">{user.fullName}</span>
                  <ChevronDown aria-hidden className="size-3.5 text-text-tertiary" />
                </button>
              </DropdownMenu.Trigger>
              <DropdownMenu.Portal>
                <DropdownMenu.Content
                  side="bottom"
                  align="end"
                  sideOffset={8}
                  collisionPadding={16}
                  className="z-50 min-w-[220px] overflow-hidden rounded-md border border-border-subtle bg-bg-elevated p-1 text-sm text-text-primary shadow-lg text-start"
                >
                  <div className="px-3 py-2.5">
                    <p className="truncate font-medium">{user.fullName}</p>
                    <p className="truncate text-xs text-text-secondary">{user.email}</p>
                  </div>
                  <DropdownMenu.Separator className="my-1 h-px bg-border-subtle" />
                  <DropdownMenu.Item
                    onSelect={(e) => { e.preventDefault(); navigate("/profile"); }}
                    className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[highlighted]:bg-bg-subtle"
                  >
                    <UserIcon aria-hidden className="size-4 text-text-tertiary" />
                    {t("nav:common.profile", "Profile")}
                  </DropdownMenu.Item>
                  <DropdownMenu.Item
                    onSelect={(e) => { e.preventDefault(); navigate("/admin/settings"); }}
                    className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[highlighted]:bg-bg-subtle"
                  >
                    <Settings aria-hidden className="size-4 text-text-tertiary" />
                    {t("nav:common.settings", "Settings")}
                  </DropdownMenu.Item>
                  <DropdownMenu.Separator className="my-1 h-px bg-border-subtle" />
                  <DropdownMenu.Item
                    onSelect={(e) => { e.preventDefault(); onSignOut(); }}
                    className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start text-danger-500 outline-none data-[highlighted]:bg-danger-500/10"
                  >
                    <LogOut aria-hidden className="size-4" />
                    {t("common:cta.signOut")}
                  </DropdownMenu.Item>
                </DropdownMenu.Content>
              </DropdownMenu.Portal>
            </DropdownMenu.Root>
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
