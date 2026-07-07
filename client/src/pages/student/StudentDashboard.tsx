import { useMemo } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import {
  GraduationCap,
  FileText,
  Bookmark,
  Users,
  CalendarCheck,
  MessageSquare,
  MessageCircle,
  Sparkles,
  BookOpen,
  ListChecks,
  Bell,
  ArrowRight,
  Plus,
  type LucideIcon,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { applicationsApi } from "@/services/api/applications";
import { bookingsApi } from "@/services/api/bookings";
import { notificationsApi, UNREAD_COUNT_QUERY_KEY } from "@/services/api/notifications";
import { queryKeys } from "@/lib/queryClient";
import { useBookmarksQuery } from "@/hooks/useScholarshipsQuery";
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
  ApplicationStatusChanged: { icon: FileText, accent: "brand" },
  BookingConfirmed: { icon: CalendarCheck, accent: "success" },
  BookingRequested: { icon: CalendarCheck, accent: "warning" },
  BookingCancelled: { icon: CalendarCheck, accent: "danger" },
  ScholarshipMatch: { icon: GraduationCap, accent: "brand" },
  Message: { icon: MessageCircle, accent: "brand" },
  CommunityReply: { icon: MessageSquare, accent: "brand" },
};

function greetingKey(): "morning" | "afternoon" | "evening" {
  const h = new Date().getHours();
  if (h < 12) return "morning";
  if (h < 18) return "afternoon";
  return "evening";
}

// Application buckets shown in the "Applications by status" breakdown, in
// order. Labels reuse the applications kanban-column translations.
const STUDENT_APP_STATUSES = [
  "Intending",
  "Draft",
  "Applied",
  "Pending",
  "UnderReview",
  // Shortlisted is a live state a provider can move an application into; without
  // it here the chart dropped those rows and disagreed with the "Active" KPI.
  "Shortlisted",
  "WaitingResult",
  "Accepted",
  "Rejected",
  "Withdrawn",
];

export function StudentDashboard() {
  const { t, i18n } = useTranslation(["dashboard", "notifications", "applications"]);
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");

  const { data: applications = [], isLoading: appsLoading } = useQuery({
    queryKey: queryKeys.applications.mine,
    queryFn: applicationsApi.getMyApplications,
    staleTime: 60_000,
  });

  const { data: bookmarks = [], isLoading: bookmarksLoading } = useBookmarksQuery();

  const { data: bookings = [], isLoading: bookingsLoading } = useQuery({
    queryKey: queryKeys.bookings.mine,
    queryFn: bookingsApi.getMine,
    staleTime: 60_000,
  });

  const { data: unread = 0, isLoading: unreadLoading } = useQuery({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: notificationsApi.unreadCount,
    staleTime: 30_000,
  });

  // FR-DSH-46: don't flash hard "0"s as if they were final while the KPI queries
  // are still loading — show skeletons until the first fetch resolves.
  const statsLoading = appsLoading || bookmarksLoading || bookingsLoading || unreadLoading;

  const { data: notifPage, isLoading: notifLoading } = useQuery({
    queryKey: ["notifications", "recent"],
    queryFn: () => notificationsApi.list(1, 5),
    staleTime: 30_000,
  });

  const activeApps = applications.filter(
    (a) => a.status !== "Accepted" && a.status !== "Rejected" && a.status !== "Withdrawn",
  ).length;

  const upcomingBookings = bookings.filter(
    (b) => b.status === "Confirmed" && new Date(b.scheduledStartAt) > new Date(),
  ).length;

  // Real application status distribution (derived straight from the rows).
  const appCounts = applications.reduce<Record<string, number>>((acc, a) => {
    acc[a.status] = (acc[a.status] ?? 0) + 1;
    return acc;
  }, {});
  const appBreakdown: CategoryBar[] = STUDENT_APP_STATUSES.map((s) => ({
    label: t(`applications:kanban.columns.${s}`, { defaultValue: s }),
    count: appCounts[s] ?? 0,
  })).filter((x) => x.count > 0);

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

  const CARDS: Array<{ icon: LucideIcon; to: string; titleKey: string; descKey: string }> = [
    { icon: GraduationCap, to: "/student/scholarships", titleKey: "scholarships", descKey: "scholarships" },
    { icon: FileText, to: "/student/applications", titleKey: "applications", descKey: "applications" },
    { icon: Bookmark, to: "/student/bookmarks", titleKey: "bookmarks", descKey: "bookmarks" },
    { icon: Users, to: "/student/consultants", titleKey: "consultants", descKey: "consultants" },
    { icon: CalendarCheck, to: "/student/bookings", titleKey: "bookings", descKey: "bookings" },
    { icon: MessageSquare, to: "/student/community", titleKey: "community", descKey: "community" },
    { icon: MessageCircle, to: "/student/messages", titleKey: "messages", descKey: "messages" },
    { icon: Sparkles, to: "/student/ai", titleKey: "ai", descKey: "ai" },
    { icon: BookOpen, to: "/student/resources", titleKey: "resources", descKey: "resources" },
  ];

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-4 py-8 sm:px-6 lg:py-10">
      <WelcomeBanner
        eyebrow={t(`dashboard:greeting.${greetingKey()}`, { name: firstName })}
        title={
          <>
            {t("dashboard:student.headlinePrefix")}{" "}
            <span className="text-gradient">{t("dashboard:student.headlineSuffix")}</span>
          </>
        }
        subtitle={t("dashboard:student.banner.subtitle")}
        actions={
          <>
            <Link to="/student/scholarships" className="btn btn-primary">
              {t("dashboard:student.exploreBtn")}
              <ArrowRight aria-hidden className="size-4 rtl:rotate-180" />
            </Link>
            <Link to="/student/applications" className="btn btn-secondary">
              {t("dashboard:student.viewApplicationsBtn")}
            </Link>
          </>
        }
      />

      {/* Stat strip */}
      {statsLoading ? (
        <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <div key={i} className="h-24 animate-pulse rounded-2xl bg-bg-subtle" />
          ))}
        </section>
      ) : (
      <section className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <StatCard
          label={t("dashboard:student.stats.applications")}
          value={activeApps}
          to="/student/applications"
          icon={ListChecks}
          accent="brand"
          delay={0.02}
        />
        <StatCard
          label={t("dashboard:student.stats.saved")}
          value={bookmarks.length}
          to="/student/bookmarks"
          icon={Bookmark}
          accent="warning"
          delay={0.06}
        />
        <StatCard
          label={t("dashboard:student.stats.bookings")}
          value={upcomingBookings}
          to="/student/bookings"
          icon={CalendarCheck}
          accent="success"
          delay={0.1}
        />
        <StatCard
          label={t("dashboard:student.stats.unread")}
          value={unread}
          to="/notifications"
          icon={Bell}
          accent={unread > 0 ? "danger" : "neutral"}
          delay={0.14}
        />
      </section>
      )}

      {/* Main 12-col grid */}
      <div className="grid gap-6 lg:grid-cols-12">
        {/* Left column: nav hub cards */}
        <div className="space-y-6 lg:col-span-8">
          <ChartCard
            title={t("dashboard:student.applicationsByStatus.title")}
            subtitle={t("dashboard:student.applicationsByStatus.subtitle")}
          >
            <CategoryBars
              items={appBreakdown}
              emptyLabel={t("dashboard:student.applicationsByStatus.empty")}
            />
          </ChartCard>

          <section>
            <header className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="text-lg font-semibold text-text-primary">
                  {t("dashboard:student.cards.scholarships.title")}
                </h2>
              </div>
            </header>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {CARDS.map(({ icon, to, titleKey, descKey }, idx) => (
                <HubCard
                  key={to}
                  icon={icon}
                  to={to}
                  title={t(`dashboard:student.cards.${titleKey}.title`)}
                  description={t(`dashboard:student.cards.${descKey}.desc`)}
                  delay={idx * 0.03}
                />
              ))}
            </div>
          </section>
        </div>

        {/* Right column: sticky sidebar */}
        <aside className="space-y-6 lg:col-span-4">
          <QuickActions
            title={t("dashboard:quickActions.title")}
            actions={[
              { icon: Plus, label: t("dashboard:student.quick.newApplication"), to: "/student/scholarships", accent: "brand" },
              { icon: Users, label: t("dashboard:student.quick.browseConsultants"), to: "/student/consultants", accent: "success" },
              { icon: Sparkles, label: t("dashboard:student.quick.askAi"), to: "/student/ai", accent: "warning" },
              { icon: BookOpen, label: t("dashboard:student.quick.viewResources"), to: "/student/resources", accent: "neutral" },
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
