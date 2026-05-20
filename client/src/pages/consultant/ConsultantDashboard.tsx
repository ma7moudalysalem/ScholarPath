import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { motion } from "motion/react";
import {
  Clock,
  CalendarCheck,
  Wallet,
  type LucideIcon,
} from "lucide-react";
import { useAuthStore } from "@/stores/authStore";
import { bookingsApi } from "@/services/api/bookings";
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

export function ConsultantDashboard() {
  const { t } = useTranslation("dashboard");
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

  const { data: bookings = [] } = useQuery({
    queryKey: ["bookings", "consultant"],
    queryFn: bookingsApi.getForConsultant,
    staleTime: 60_000,
  });

  const pendingRequests = bookings.filter((b) => b.status === "Requested").length;

  const upcomingSessions = bookings.filter(
    (b) => b.status === "Confirmed" && new Date(b.scheduledStartAt) > now,
  ).length;

  const sessionsThisMonth = bookings.filter((b) => {
    const start = new Date(b.scheduledStartAt);
    return (
      (b.status === "Confirmed" || b.status === "Completed") &&
      start >= startOfMonth &&
      start <= now
    );
  }).length;

  const CARDS: Array<{ icon: LucideIcon; to: string; titleKey: string }> = [
    { icon: Clock, to: "/consultant/availability", titleKey: "availability" },
    { icon: CalendarCheck, to: "/consultant/bookings", titleKey: "bookings" },
    { icon: Wallet, to: "/consultant/earnings", titleKey: "earnings" },
  ];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
      >
        <h1 className="mb-1.5 text-3xl">{t("consultant.title", { name: firstName })}</h1>
        <p className="text-text-secondary">{t("consultant.subtitle")}</p>
      </motion.div>

      {/* Stat strip */}
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
        className="mt-6 mb-8 grid grid-cols-3 gap-3"
      >
        <StatCard
          label={t("consultant.stats.pending")}
          value={pendingRequests}
          to="/consultant/bookings"
          accent={pendingRequests > 0 ? "warning" : "default"}
        />
        <StatCard
          label={t("consultant.stats.upcoming")}
          value={upcomingSessions}
          to="/consultant/bookings"
          accent={upcomingSessions > 0 ? "brand" : "default"}
        />
        <StatCard
          label={t("consultant.stats.thisMonth")}
          value={sessionsThisMonth}
          to="/consultant/earnings"
        />
      </motion.div>

      {/* Nav cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {CARDS.map(({ icon, to, titleKey }, idx) => (
          <HubCard
            key={to}
            icon={icon}
            to={to}
            title={t(`consultant.cards.${titleKey}.title`)}
            description={t(`consultant.cards.${titleKey}.desc`)}
            delay={idx * 0.05}
          />
        ))}
      </div>
    </div>
  );
}
