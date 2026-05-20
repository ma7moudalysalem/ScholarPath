import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { motion } from "motion/react";
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
  type LucideIcon,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { applicationsApi } from "@/services/api/applications";
import { bookingsApi } from "@/services/api/bookings";
import { notificationsApi, UNREAD_COUNT_QUERY_KEY } from "@/services/api/notifications";
import { queryKeys } from "@/lib/queryClient";
import { useBookmarksQuery } from "@/hooks/useScholarshipsQuery";
import { cn } from "@/lib/utils";

// ─── Stat card ────────────────────────────────────────────────────────────────

function StatCard({
  label,
  value,
  to,
  accent = "default",
}: {
  label: string;
  value: number;
  to: string;
  accent?: "default" | "brand" | "warning";
}) {
  return (
    <Link
      to={to}
      className="flex flex-col rounded-xl border border-border-subtle bg-bg-elevated p-4 shadow-xs transition hover:border-brand-200 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
    >
      <span
        className={cn(
          "text-2xl font-bold tabular-nums",
          accent === "brand"   && "text-brand-500",
          accent === "warning" && "text-warning-500",
          accent === "default" && "text-text-primary",
        )}
      >
        {value}
      </span>
      <span className="mt-1 text-xs font-medium text-text-secondary">{label}</span>
    </Link>
  );
}

// ─── Hub nav card ─────────────────────────────────────────────────────────────

function HubCard({
  icon: Icon,
  title,
  description,
  to,
  delay,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  to: string;
  delay: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay }}
    >
      <Link
        to={to}
        className="group flex h-full flex-col rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition-all duration-200 hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-md"
      >
        <div className="mb-4 flex size-10 items-center justify-center rounded-xl bg-brand-50 text-brand-500 transition-all duration-200 group-hover:bg-brand-500 group-hover:text-white">
          <Icon aria-hidden className="size-5" />
        </div>
        <h2 className="mb-1 font-semibold text-text-primary transition-colors group-hover:text-brand-500">
          {title}
        </h2>
        <p className="text-sm leading-relaxed text-text-secondary">{description}</p>
      </Link>
    </motion.div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function StudentDashboard() {
  const { t } = useTranslation("dashboard");
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");

  // Fetch stat data — all stale-time-guarded so they don't hurt load time
  const { data: applications = [] } = useQuery({
    queryKey: queryKeys.applications.mine,
    queryFn: applicationsApi.getMyApplications,
    staleTime: 60_000,
  });

  const { data: bookmarks = [] } = useBookmarksQuery();

  const { data: bookings = [] } = useQuery({
    queryKey: ["bookings", "mine"],
    queryFn: bookingsApi.getMine,
    staleTime: 60_000,
  });

  const { data: unread = 0 } = useQuery({
    queryKey: UNREAD_COUNT_QUERY_KEY,
    queryFn: notificationsApi.unreadCount,
    staleTime: 30_000,
  });

  // Derived counts
  const activeApps = applications.filter(
    (a) => a.status !== "Accepted" && a.status !== "Rejected" && a.status !== "Withdrawn",
  ).length;

  const upcomingBookings = bookings.filter(
    (b) => b.status === "Confirmed" && new Date(b.scheduledStartAt) > new Date(),
  ).length;

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
    <div className="mx-auto max-w-6xl px-4 py-10">
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
      >
        <h1 className="mb-1.5 text-3xl">{t("student.title", { name: firstName })}</h1>
        <p className="text-text-secondary">{t("student.subtitle")}</p>
      </motion.div>

      {/* Stat strip */}
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
        className="mt-6 mb-8 grid grid-cols-2 gap-3 sm:grid-cols-4"
      >
        <StatCard
          label={t("student.stats.applications")}
          value={activeApps}
          to="/student/applications"
          accent={activeApps > 0 ? "brand" : "default"}
        />
        <StatCard
          label={t("student.stats.saved")}
          value={bookmarks.length}
          to="/student/bookmarks"
        />
        <StatCard
          label={t("student.stats.bookings")}
          value={upcomingBookings}
          to="/student/bookings"
          accent={upcomingBookings > 0 ? "brand" : "default"}
        />
        <StatCard
          label={t("student.stats.unread")}
          value={unread}
          to="/notifications"
          accent={unread > 0 ? "warning" : "default"}
        />
      </motion.div>

      {/* Nav cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {CARDS.map(({ icon, to, titleKey, descKey }, idx) => (
          <HubCard
            key={to}
            icon={icon}
            to={to}
            title={t(`student.cards.${titleKey}.title`)}
            description={t(`student.cards.${descKey}.desc`)}
            delay={idx * 0.05}
          />
        ))}
      </div>
    </div>
  );
}
