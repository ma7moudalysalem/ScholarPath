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
import { bookingsApi } from "@/services/api/bookings";
import { notificationsApi } from "@/services/api/notifications";
import { queryKeys } from "@/lib/queryClient";
import {
  WelcomeBanner,
  StatCard,
  HubCard,
  ActivityFeed,
  QuickActions,
  ChartCard,
  CategoryBars,
  type ActivityItem,
  type CategoryBar,
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
// two no-show statuses are merged into a single "NoShow" row at render time.
const CONSULTANT_BOOKING_STATUSES = ["Requested", "Confirmed", "Completed", "Cancelled"];

export function ConsultantDashboard() {
  const { t, i18n } = useTranslation(["dashboard"]);
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

  const { data: bookings = [] } = useQuery({
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
    (bookingCounts.NoShowStudent ?? 0) + (bookingCounts.NoShowConsultant ?? 0);
  const bookingBreakdown: CategoryBar[] = [
    ...CONSULTANT_BOOKING_STATUSES.map((s) => ({
      label: t(`dashboard:consultant.bookingsByStatus.statuses.${s}`),
      count: bookingCounts[s] ?? 0,
    })),
    { label: t("dashboard:consultant.bookingsByStatus.statuses.NoShow"), count: noShowCount },
  ].filter((x) => x.count > 0);

  const activities = useMemo<ActivityItem[]>(() => {
    if (!notifPage) return [];
    const isAr = i18n.language === "ar";
    return notifPage.items.slice(0, 5).map((n) => {
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
  }, [notifPage, i18n.language]);

  const CARDS: Array<{ icon: LucideIcon; to: string; titleKey: string; accent: StatAccent }> = [
    { icon: Clock, to: "/consultant/availability", titleKey: "availability", accent: "brand" },
    { icon: CalendarCheck, to: "/consultant/bookings", titleKey: "bookings", accent: "success" },
    { icon: Wallet, to: "/consultant/earnings", titleKey: "earnings", accent: "warning" },
  ];

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
          mock numbers that misled the consultant about real growth). */}
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
          to="/consultant/earnings"
          icon={Wallet}
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

      <div className="grid gap-6 lg:grid-cols-12">
        <div className="space-y-6 lg:col-span-8">
          <ChartCard
            title={t("dashboard:consultant.bookingsByStatus.title")}
            subtitle={t("dashboard:consultant.bookingsByStatus.subtitle")}
          >
            <CategoryBars
              items={bookingBreakdown}
              emptyLabel={t("dashboard:consultant.bookingsByStatus.empty")}
            />
          </ChartCard>

          <section>
            <header className="mb-4">
              <h2 className="text-lg font-semibold text-text-primary">
                {t("dashboard:consultant.cards.bookings.title")}
              </h2>
            </header>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {CARDS.map(({ icon, to, titleKey, accent }, idx) => (
                <HubCard
                  key={to}
                  icon={icon}
                  to={to}
                  title={t(`dashboard:consultant.cards.${titleKey}.title`)}
                  description={t(`dashboard:consultant.cards.${titleKey}.desc`)}
                  accent={accent}
                  delay={idx * 0.04}
                />
              ))}
            </div>
          </section>
        </div>

        <aside className="space-y-6 lg:col-span-4">
          <QuickActions
            title={t("dashboard:quickActions.title")}
            actions={[
              { icon: Clock, label: t("dashboard:consultant.quick.availability"), to: "/consultant/availability", accent: "brand" },
              { icon: CalendarCheck, label: t("dashboard:consultant.quick.bookings"), to: "/consultant/bookings", accent: "success" },
              { icon: Wallet, label: t("dashboard:consultant.quick.earnings"), to: "/consultant/earnings", accent: "warning" },
              { icon: FileEdit, label: t("dashboard:consultant.quick.profile"), to: "/settings/profile", accent: "neutral" },
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
