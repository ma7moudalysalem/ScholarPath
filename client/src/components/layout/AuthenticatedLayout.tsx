import { useMemo, useState, useEffect, Suspense } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  GraduationCap,
  LayoutDashboard,
  Search,
  ListChecks,
  ClipboardCheck,
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
  FileEdit,
  BarChart2,
  ChevronDown,
  ChevronRight,
  Repeat,
  User as UserIcon,
  TrendingUp,
  PieChart,
  Star,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import { motion, AnimatePresence } from "motion/react";
import { toast } from "sonner";
import { LanguageSwitcher } from "@/components/common/LanguageSwitcher";
import { ThemeToggle } from "@/components/common/ThemeToggle";
import { useAuthStore } from "@/stores/authStore";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";
import { useNotificationHub } from "@/hooks/useNotificationHub";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { notificationsApi, UNREAD_COUNT_QUERY_KEY } from "@/services/api/notifications";
import { cn } from "@/lib/utils";
import { userPhotoUrl } from "@/lib/userPhoto";

const SWITCHABLE_ROLES = ["Student", "Consultant", "ScholarshipProvider", "Admin"] as const;

function ProfileMenu() {
  const { t, i18n } = useTranslation(["common", "nav"]);
  const { user, clear } = useAuthStore();
  const navigate = useNavigate();
  const isRtl = i18n.language.startsWith("ar");

  const switchRoleMut = useMutation({
    mutationFn: (targetRole: string) => authApi.switchRole(targetRole),
    onSuccess: (res) => {
      const next = applyAuthSession(res);
      toast.success(
        t("common:roleSwitch.success", "Active role updated to {{role}}.", {
          role: next.activeRole ?? next.roles[0] ?? "",
        }),
      );
      navigate(postAuthPath(next), { replace: true });
    },
    onError: (err) =>
      toast.error(
        apiErrorMessage(err, t("common:roleSwitch.error", "Could not switch role.")),
      ),
  });

  if (!user) return null;
  const activeRole = user.activeRole ?? user.roles[0] ?? null;
  const switchTargets = user.roles.filter(
    (r) => r !== activeRole && (SWITCHABLE_ROLES as readonly string[]).includes(r),
  );

  const onSignOut = () => {
    // SEC-11 — revoke the refresh token server-side before clearing locally.
    const refreshToken = useAuthStore.getState().tokens?.refreshToken;
    if (refreshToken) void authApi.logout(refreshToken).catch(() => {});
    clear();
    navigate("/");
  };

  return (
    <DropdownMenu.Root dir={isRtl ? "rtl" : "ltr"}>
      <DropdownMenu.Trigger asChild>
        <button
          type="button"
          aria-label={t("common:nav.profile", "Open profile menu")}
          className="ms-1 flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm transition-colors hover:bg-bg-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
        >
          <HeaderAvatar userId={user.id} firstName={user.firstName} />
          <span className="hidden font-medium sm:inline">{user.fullName}</span>
          <ChevronDown aria-hidden className="size-3.5 text-text-tertiary" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        {/*
          The trigger lives in the sticky top header. `side="bottom"` lets
          the menu open downward (the default) and `align="end"` anchors
          its end edge to the trigger's end edge so it opens flush against
          the avatar in both LTR and RTL — Radix swaps start/end correctly
          thanks to `dir` on the Root. `collisionPadding` keeps the floating
          panel away from the viewport edge so it never clips off-screen.
        */}
        <DropdownMenu.Content
          side="bottom"
          align="end"
          sideOffset={8}
          collisionPadding={16}
          className="z-50 min-w-[260px] overflow-hidden rounded-md border border-border-subtle bg-bg-elevated p-1 text-sm text-text-primary shadow-lg text-start"
        >
          <div className="px-3 py-2.5">
            <p className="truncate font-medium">{user.fullName}</p>
            <p className="truncate text-xs text-text-secondary">{user.email}</p>
            {activeRole && (
              <p className="mt-1 inline-flex items-center gap-1 rounded-full bg-brand-500/10 px-2 py-0.5 text-[11px] font-medium text-brand-500">
                {t("common:roleSwitch.activeBadge", "Active: {{role}}", { role: t(`common:roles.${activeRole}`, activeRole) })}
              </p>
            )}
          </div>

          {switchTargets.length > 0 && (
            <>
              <DropdownMenu.Separator className="my-1 h-px bg-border-subtle" />
              <DropdownMenu.Label className="px-3 pb-1 pt-1 text-[11px] uppercase tracking-wide text-text-tertiary">
                {t("common:roleSwitch.heading", "Switch role")}
              </DropdownMenu.Label>
              {switchTargets.map((role) => (
                <DropdownMenu.Item
                  key={role}
                  disabled={switchRoleMut.isPending}
                  onSelect={(e) => {
                    e.preventDefault();
                    switchRoleMut.mutate(role);
                  }}
                  aria-label={t("common:roleSwitch.optionAria", "Switch to {{role}}", { role })}
                  className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[disabled]:cursor-not-allowed data-[disabled]:opacity-50 data-[highlighted]:bg-bg-subtle"
                >
                  <Repeat aria-hidden className="size-4 text-text-tertiary" />
                  {t("common:roleSwitch.option", "Switch to {{role}}", { role })}
                </DropdownMenu.Item>
              ))}
            </>
          )}

          <DropdownMenu.Separator className="my-1 h-px bg-border-subtle" />
          <DropdownMenu.Item
            onSelect={(e) => {
              e.preventDefault();
              navigate("/profile");
            }}
            className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[highlighted]:bg-bg-subtle"
          >
            <UserIcon aria-hidden className="size-4 text-text-tertiary" />
            {t("nav:common.profile", "Profile")}
          </DropdownMenu.Item>
          <DropdownMenu.Item
            onSelect={(e) => {
              e.preventDefault();
              navigate("/notifications/preferences");
            }}
            className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start outline-none data-[highlighted]:bg-bg-subtle"
          >
            <Settings aria-hidden className="size-4 text-text-tertiary" />
            {t("nav:common.settings", "Settings")}
          </DropdownMenu.Item>
          <DropdownMenu.Separator className="my-1 h-px bg-border-subtle" />
          <DropdownMenu.Item
            onSelect={(e) => {
              e.preventDefault();
              onSignOut();
            }}
            className="flex cursor-pointer items-center gap-2 rounded-sm px-3 py-2 text-sm text-start text-danger-500 outline-none data-[highlighted]:bg-danger-500/10"
          >
            <LogOut aria-hidden className="size-4" />
            {t("common:cta.signOut")}
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

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
    { to: "/student/review-requests", key: "student.reviewRequests", icon: ClipboardCheck },
    { to: "/student/bookmarks", key: "student.bookmarks", icon: Bookmark },
    { to: "/student/consultants", key: "student.consultants", icon: Users },
    { to: "/student/bookings", key: "student.bookings", icon: Calendar },
    { to: "/student/community", key: "student.community", icon: MessageSquare },
    { to: "/student/resources", key: "student.resources", icon: BookOpen },
    { to: "/student/resource-progress", key: "student.resourceProgress", icon: TrendingUp },
    { to: "/student/documents", key: "student.documents", icon: FolderOpen },
    { to: "/student/ai", key: "student.ai", icon: Sparkles },
    { to: "/student/messages", key: "student.messages", icon: MessageSquare },
    { to: "/student/analytics", key: "student.analytics", icon: BarChart2 },
  ],
  ScholarshipProvider: [
    { to: "/company", key: "company.dashboard", icon: LayoutDashboard },
    { to: "/company/scholarships", key: "company.scholarships", icon: Search },
    { to: "/company/applications-review", key: "company.applicationsReview", icon: ListChecks },
    { to: "/company/review-requests", key: "company.reviewRequests", icon: ClipboardCheck },
    { to: "/company/reviews", key: "company.reviews", icon: Star },
    { to: "/company/billing", key: "company.billing", icon: Calendar },
    { to: "/author/resources", key: "company.resources", icon: FileEdit },
    { to: "/company/insights", key: "company.insights", icon: PieChart },
  ],
  Consultant: [
    { to: "/consultant", key: "consultant.dashboard", icon: LayoutDashboard },
    { to: "/consultant/availability", key: "consultant.availability", icon: Calendar },
    { to: "/consultant/bookings", key: "consultant.bookings", icon: ListChecks },
    { to: "/consultant/reviews", key: "consultant.reviews", icon: Star },
    { to: "/consultant/earnings", key: "consultant.earnings", icon: Calendar },
    { to: "/author/resources", key: "consultant.resources", icon: FileEdit },
    { to: "/consultant/analytics", key: "consultant.analytics", icon: BarChart2 },
    { to: "/consultant/earnings-trend", key: "consultant.earningsTrend", icon: TrendingUp },
  ],
};

/**
 * Routes that only exist to show money (billing, earnings, earnings trend).
 * Hidden from the sidebar when the platform runs in free mode
 * (payments.enabled=false); the routes themselves also redirect via
 * RequirePayments, so a hidden link can't be reached by deep link either.
 */
const MONEY_ROUTES = new Set<string>([
  "/company/billing",
  "/consultant/earnings",
  "/consultant/earnings-trend",
]);

/** Shared sidebar content — used in both the desktop sidebar and the mobile drawer */
function SidebarContent({
  navItems,
  homePath,
  user,
  role,
  onNavigate,
}: {
  navItems: NavItem[];
  homePath: string;
  user: { id: string; firstName: string; fullName: string } | null;
  role: string;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation(["common", "nav"]);
  const { clear } = useAuthStore();
  const navigate = useNavigate();

  const onSignOut = () => {
    // SEC-11 — revoke the refresh token server-side before clearing locally.
    const refreshToken = useAuthStore.getState().tokens?.refreshToken;
    if (refreshToken) void authApi.logout(refreshToken).catch(() => {});
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
        className="flex h-14 shrink-0 items-center gap-2.5 border-b border-border-subtle px-4"
      >
        {/* Brand gradient icon box */}
        <div className="flex size-7 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-sm">
          <GraduationCap aria-hidden className="size-4" />
        </div>
        <span className="font-display text-[15px] font-bold tracking-tight text-text-primary">
          {t("common:appName")}
        </span>
      </Link>

      {/* Nav links — staggered fade-in on first mount */}
      <nav className="flex-1 space-y-0.5 overflow-y-auto p-3">
        {navItems.map(({ to, key, icon: Icon }, idx) => (
          <motion.div
            key={to}
            initial={{ opacity: 0, x: -8 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{
              duration: 0.28,
              ease: [0.22, 1, 0.36, 1],
              delay: Math.min(idx, 12) * 0.035,
            }}
          >
            <NavLink
              to={to}
              end={to === `/${role.toLowerCase()}`}
              onClick={onNavigate}
              className={({ isActive }) =>
                cn(
                  "relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-150",
                  isActive
                    ? "nav-item-active"
                    : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
                )
              }
            >
              {({ isActive }) => (
                <>
                  {/* Icon wrapper */}
                  <span
                    className={cn(
                      "flex size-7 shrink-0 items-center justify-center rounded-md transition-colors duration-150",
                      isActive
                        ? "bg-brand-500 text-white"
                        : "text-current",
                    )}
                  >
                    <Icon aria-hidden className="size-4" />
                  </span>
                  {t(`nav:${key}`)}
                </>
              )}
            </NavLink>
          </motion.div>
        ))}
      </nav>

      {/* Bottom section — user card + actions */}
      <div className="border-t border-border-subtle p-3 space-y-1">
        {/* User info card — clickable, navigates to profile */}
        {user && (
          <Link
            to="/profile"
            onClick={onNavigate}
            aria-label={t("common:nav.profile", "Open profile")}
            className="group mb-2 flex items-center gap-2.5 rounded-xl bg-bg-subtle px-3 py-2.5 transition-all hover:bg-brand-50 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            <HeaderAvatar userId={user.id} firstName={user.firstName} />
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-semibold text-text-primary leading-tight group-hover:text-brand-600">
                {user.fullName}
              </div>
              <div className="text-xs text-text-tertiary leading-tight mt-0.5">
                {t(`common:roles.${role}`, role)}
              </div>
            </div>
            <ChevronRight
              aria-hidden
              className="size-3.5 shrink-0 text-text-tertiary opacity-0 transition-opacity group-hover:opacity-100 rtl:rotate-180"
            />
          </Link>
        )}

        {/*
          The user card above already opens /profile. This "Settings" entry
          must lead somewhere distinct — notification preferences — otherwise
          both the card and the gear navigate to the same page (the duplicate
          the team lead flagged). Profile = who you are + password; Settings =
          notification preferences.
        */}
        <NavLink
          to="/notifications/preferences"
          onClick={onNavigate}
          className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle hover:text-text-primary"
        >
          <span className="flex size-7 shrink-0 items-center justify-center">
            <Settings aria-hidden className="size-4" />
          </span>
          {t("nav:common.settings")}
        </NavLink>
        <button
          type="button"
          onClick={onSignOut}
          className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle hover:text-text-primary"
        >
          <span className="flex size-7 shrink-0 items-center justify-center">
            <LogOut aria-hidden className="size-4" />
          </span>
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
  // Free mode: drop billing/earnings nav entries so no payment page is linked.
  const paymentsEnabled = usePaymentsEnabled();
  const navItems = useMemo(() => {
    const base = NAV_BY_ROLE[role] ?? NAV_BY_ROLE.Student;
    return paymentsEnabled ? base : base.filter((i) => !MONEY_ROUTES.has(i.to));
  }, [role, paymentsEnabled]);
  const homePath = user ? postAuthPath(user) : "/";
  const location = useLocation();

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

  // Shared sidebar props
  const sidebarUser = user
    ? { id: user.id, firstName: user.firstName, fullName: user.fullName }
    : null;

  return (
    <div className="flex min-h-screen bg-bg-subtle text-text-primary">

      {/* ── Desktop sidebar (hidden on mobile) ── */}
      <aside className="sticky top-0 hidden h-screen w-64 shrink-0 flex-col border-e border-border-subtle bg-bg-elevated md:flex">
        <SidebarContent
          navItems={navItems}
          homePath={homePath}
          user={sidebarUser}
          role={role}
        />
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
                user={sidebarUser}
                role={role}
                onNavigate={() => setDrawerOpen(false)}
              />
            </motion.aside>
          </>
        )}
      </AnimatePresence>

      {/* ── Main content ── */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header
          className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-border-subtle bg-bg-elevated/90 px-4 backdrop-blur-xl"
          style={{ boxShadow: "var(--shadow-xs)" }}
        >

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
              className="relative inline-flex size-9 items-center justify-center rounded-lg border border-border-subtle bg-bg-elevated text-text-primary transition hover:bg-bg-subtle"
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
            <ProfileMenu />
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
