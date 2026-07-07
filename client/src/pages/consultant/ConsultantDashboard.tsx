import { useMemo } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  Clock,
  CalendarCheck,
  Wallet,
  Bell,
  ArrowRight,
  User,
  FileEdit,
  type LucideIcon,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { bookingsApi } from "@/services/api/bookings";
import { notificationsApi } from "@/services/api/notifications";
import { queryKeys } from "@/lib/queryClient";
import { formatDateTime } from "@/lib/bookingFormat";
import {
  WelcomeBanner,
  StatCard,
  ActivityFeed,
  QuickActions,
  ChartCard,
  DonutChart,
  StatusPill,
  type ActivityItem,
  type DonutSegment,
  type StatAccent,
} from "@/components/dashboard/primitives";
import { formatRelativeTime } from "@/components/dashboard/utils";

const NOTIFICATION_ICON: Record<string, { icon: LucideIcon; accent: StatAccent }> = {
  BookingRequested: { icon: CalendarCheck, accent: "warning" },
  BookingConfirmed: { icon: CalendarCheck, accent: "success" },
  BookingCancelled: { icon: CalendarCheck, accent: "danger" },
  BookingCompleted: { icon: CalendarCheck, accent: "success" },
  PayoutInitiated: { icon: Wallet, accent: "success" },
  ReviewReceived: { icon: User, accent: "brand" },
};

function greetingKey(): "morning" | "afternoon" | "evening" {
  const h = new Date().getHours();
  if (h < 12) return "morning";
  if (h < 18) return "afternoon";
  return "evening";
}

// Booking buckets shown in the "Bookings by status" breakdown, in order. The
// no-show statuses (reported + both confirmed sides) are merged into a single
// "NoShow" segment at render time. Rejected (consultant declined) and Expired
// (no response in time) are real terminal outcomes and must be shown.
const CONSULTANT_BOOKING_STATUSES = [
  "Requested",
  "Confirmed",
  "Completed",
  "Cancelled",
  "Rejected",
  "Expired",
];

// Per-status visual encoding: a pill tone (background chip) and a donut color
// (a design-system status token). Keeps the distribution donut and the row
// pills in sync so a status reads the same everywhere. "NoShow" is the merged
// bucket for the three server no-show statuses.
const STATUS_META: Record<string, { tone: StatAccent; color: string }> = {
  Requested: { tone: "warning", color: "var(--color-brand-400)" },
  Confirmed: { tone: "brand",   color: "var(--color-status-applied)" },
  Completed: { tone: "success", color: "var(--color-status-accepted)" },
  Cancelled: { tone: "neutral", color: "var(--color-status-withdrawn)" },
  Rejected:  { tone: "danger",  color: "var(--color-status-rejected)" },
  Expired:   { tone: "neutral", color: "var(--color-text-tertiary)" },
  NoShow:    { tone: "warning", color: "var(--color-status-pending)" },
};

export function ConsultantDashboard() {
  const { t, i18n } = useTranslation(["dashboard"]);
  const paymentsEnabled = usePaymentsEnabled();
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

  const { data: bookings = [], isLoading: statsLoading } = useQuery({
    queryKey: queryKeys.bookings.consultant,
    queryFn: bookingsApi.getForConsultant,
    staleTime: 60_000,
  });

  const { data: notifPage, isLoading: notifLoading } = useQuery({
    queryKey: ["notifications", "recent"],
    queryFn: () => notificationsApi.list(1, 5),
    staleTime: 30_000,
  });

  const pendingRequests = bookings.filter((b) => b.status === "Requested").length;
  const upcomingSessions = bookings.filter(
    (b) => b.status === "Confirmed" && new Date(b.scheduledStartAt) > now,
  ).length;
  // "Sessions this month" = bookings whose scheduled start fell within this
  // calendar month and which actually resolved into a session (completed or
  // no-show). A Confirmed booking whose start is in the past is a data-
  // inconsistency state we don't want to credit as a session.
  const sessionsThisMonth = bookings.filter((b) => {
    const start = new Date(b.scheduledStartAt);
    return (
      (b.status === "Completed" ||
        b.status === "NoShowStudent" ||
        b.status === "NoShowConsultant") &&
      start >= startOfMonth &&
      start <= now
    );
  }).length;
  const completed = bookings.filter((b) => b.status === "Completed").length;
  // Completion rate = sessions that happened / sessions that should have
  // happened. Rejected = consultant declined upfront (never scheduled), so it
  // is excluded; the no-show statuses still count because they consumed a slot.
  const totalRelevant = bookings.filter((b) =>
    [
      "Completed",
      "Cancelled",
      "NoShowStudent",
      "NoShowConsultant",
    ].includes(b.status),
  ).length;
  const completionRate = totalRelevant > 0 ? Math.round((completed / totalRelevant) * 100) : 0;

  // Real booking status distribution (derived straight from the rows). Computed
  // inline — cheap, and avoids a useMemo over the `= []` default reference.
  const bookingCounts = bookings.reduce<Record<string, number>>((acc, b) => {
    acc[b.status] = (acc[b.status] ?? 0) + 1;
    return acc;
  }, {});
  const noShowCount =
    (bookingCounts.NoShowStudent ?? 0) +
    (bookingCounts.NoShowConsultant ?? 0) +
    (bookingCounts.NoShowReported ?? 0);
  // Donut segments encode each status by its design-system color; zero-count
  // segments are dropped so the ring and legend only show real states.
  const bookingSegments: DonutSegment[] = [
    ...CONSULTANT_BOOKING_STATUSES.map((s) => ({
      label: t(`dashboard:consultant.bookingsByStatus.statuses.${s}`),
      count: bookingCounts[s] ?? 0,
      color: STATUS_META[s]?.color ?? "var(--color-text-tertiary)",
    })),
    {
      label: t("dashboard:consultant.bookingsByStatus.statuses.NoShow"),
      count: noShowCount,
      color: STATUS_META.NoShow.color,
    },
  ].filter((x) => x.count > 0);

  // The next confirmed sessions, soonest first — a real, actionable list that
  // replaces the redundant nav-tile grid (those links live in Quick actions).
  const nowMs = now.getTime();
  const upcomingList = useMemo(
    () =>
      bookings
        .filter(
          (b) => b.status === "Confirmed" && new Date(b.scheduledStartAt).getTime() > nowMs,
        )
        .sort(
          (a, b) =>
            new Date(a.scheduledStartAt).getTime() - new Date(b.scheduledStartAt).getTime(),
        )
        .slice(0, 6),
    [bookings, nowMs],
  );

  const activities = useMemo<ActivityItem[]>(() => {
    if (!notifPage) return [];
    const isAr = i18n.language === "ar";
    // In free mode, drop payout/payment-framed notifications so the
    // Wallet-iconed money items don't surface in the activity feed.
    const items = paymentsEnabled
      ? notifPage.items
      : notifPage.items.filter(
          (n) => !/^(Payout|Payment)/.test(n.type),
        );
    return items.slice(0, 5).map((n) => {
      const meta = NOTIFICATION_ICON[n.type] ?? { icon: Bell, accent: "brand" as StatAccent };
      return {
        id: n.id,
        title: isAr ? n.titleAr : n.titleEn,
        timeAgo: formatRelativeTime(n.createdAt, i18n.language),
        icon: meta.icon,
        accent: meta.accent,
        to: n.deepLink ?? "/notifications",
      };
    });
  }, [notifPage, i18n.language, paymentsEnabled]);

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:py-10">
      <WelcomeBanner
        eyebrow={t(`dashboard:greeting.${greetingKey()}`, { name: firstName })}
        title={
          <>
            {t("dashboard:consultant.headlinePrefix")}{" "}
            <span className="text-gradient">{t("dashboard:consultant.headlineSuffix")}</span>
          </>
        }
        subtitle={t("dashboard:consultant.banner.subtitle")}
        actions={
          <>
            <Link to="/consultant/bookings" className="btn btn-primary">
              {t("dashboard:consultant.exploreBtn")}
              <ArrowRight aria-hidden className="size-4 rtl:rotate-180" />
            </Link>
            <Link to="/consultant/availability" className="btn btn-secondary">
              {t("dashboard:consultant.secondaryBtn")}
            </Link>
          </>
        }
      />

      {/* KPI tiles — no fabricated delta/trend props (those were hard-coded
          mock numbers that misled the consultant about real growth).
          FR-DSH-46: skeletons until the bookings query resolves (no 0-flash). */}
      {statsLoading ? (
        <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-2xl bg-bg-subtle" />
          ))}
        </section>
      ) : (
      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <StatCard
          label={t("dashboard:consultant.stats.pending")}
          value={pendingRequests}
          to="/consultant/bookings"
          icon={Clock}
          accent={pendingRequests > 0 ? "warning" : "neutral"}
          delay={0.02}
        />
        <StatCard
          label={t("dashboard:consultant.stats.upcoming")}
          value={upcomingSessions}
          to="/consultant/bookings"
          icon={CalendarCheck}
          accent="brand"
          delay={0.06}
        />
        <StatCard
          label={t("dashboard:consultant.stats.thisMonth")}
          value={sessionsThisMonth}
          // In free mode this stat is a plain session count — don't link it to
          // the earnings page or frame it with the Wallet (money) icon.
          to={paymentsEnabled ? "/consultant/earnings" : undefined}
          icon={paymentsEnabled ? Wallet : CalendarCheck}
          accent="success"
          delay={0.1}
        />
        <StatCard
          label={t("dashboard:consultant.stats.completion")}
          value={`${completionRate}%`}
          to="/consultant/analytics"
          icon={User}
          accent={completionRate >= 80 ? "success" : completionRate >= 60 ? "warning" : "danger"}
          delay={0.14}
        />
      </section>
      )}

      <div className="grid gap-6 lg:grid-cols-12">
        {/* Left column: booking distribution + upcoming-sessions list */}
        <div className="space-y-6 lg:col-span-8">
          <ChartCard
            title={t("dashboard:consultant.bookingsByStatus.title")}
            subtitle={t("dashboard:consultant.bookingsByStatus.subtitle")}
          >
            <DonutChart
              segments={bookingSegments}
              centerValue={bookings.length}
              centerLabel={t("dashboard:consultant.cards.bookings.title")}
              emptyLabel={t("dashboard:consultant.bookingsByStatus.empty")}
            />
          </ChartCard>

          <section className="card-premium p-5 sm:p-6">
            <header className="mb-4 flex items-center justify-between">
              <h2 className="text-base font-semibold text-text-primary">
                {t("dashboard:consultant.stats.upcoming")}
              </h2>
              <Link
                to="/consultant/bookings"
                className="text-xs font-medium text-brand-600 transition-colors hover:text-brand-700 hover:underline"
              >
                {t("dashboard:activity.viewAll")}
              </Link>
            </header>

            {upcomingList.length === 0 ? (
              <div className="rounded-xl border border-dashed border-border-subtle bg-bg-subtle/30 p-8 text-center">
                <p className="text-sm text-text-tertiary">
                  {t("dashboard:consultant.upcomingSessions.empty", {
                    defaultValue: "No upcoming sessions scheduled.",
                  })}
                </p>
              </div>
            ) : (
              <ul className="-mx-2 divide-y divide-border-subtle">
                {upcomingList.map((b) => {
                  const meta = STATUS_META.Confirmed;
                  const initial = (b.studentName || "?").trim().charAt(0).toUpperCase();
                  return (
                    <li key={b.id}>
                      <Link
                        to="/consultant/bookings"
                        className="flex items-center gap-3 rounded-lg px-2 py-3 transition-colors hover:bg-bg-subtle/60"
                      >
                        <span
                          className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-sm font-bold text-brand-600"
                          aria-hidden
                        >
                          {initial}
                        </span>
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-semibold text-text-primary">
                            {b.studentName}
                          </p>
                          <p className="truncate text-xs text-text-tertiary">
                            {formatDateTime(b.scheduledStartAt, i18n.language)}
                          </p>
                        </div>
                        <StatusPill
                          tone={meta.tone}
                          label={t("dashboard:consultant.bookingsByStatus.statuses.Confirmed")}
                        />
                      </Link>
                    </li>
                  );
                })}
              </ul>
            )}
          </section>
        </div>

        <aside className="space-y-6 lg:col-span-4">
          <QuickActions
            title={t("dashboard:quickActions.title")}
            actions={[
              { icon: Clock, label: t("dashboard:consultant.quick.availability"), to: "/consultant/availability", accent: "brand" as StatAccent },
              { icon: CalendarCheck, label: t("dashboard:consultant.quick.bookings"), to: "/consultant/bookings", accent: "success" as StatAccent },
              ...(paymentsEnabled
                ? [{ icon: Wallet, label: t("dashboard:consultant.quick.earnings"), to: "/consultant/earnings", accent: "warning" as StatAccent }]
                : []),
              { icon: FileEdit, label: t("dashboard:consultant.quick.profile"), to: "/profile", accent: "neutral" as StatAccent },
            ]}
          />

          <ActivityFeed
            title={t("dashboard:activity.title")}
            viewAllLabel={t("dashboard:activity.viewAll")}
            viewAllTo="/notifications"
            emptyTitle={t("dashboard:activity.emptyTitle")}
            emptyBody={t("dashboard:activity.emptyBody")}
            items={activities}
            isLoading={notifLoading}
          />
        </aside>
      </div>
    </div>
  );
}
