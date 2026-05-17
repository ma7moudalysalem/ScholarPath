import { useTranslation } from "react-i18next";
import { Clock, CalendarCheck, Wallet } from "lucide-react";
import { DashboardHub } from "@/components/common/DashboardHub";
import { useAuthStore } from "@/stores/authStore";

export function ConsultantDashboard() {
  const { t } = useTranslation("dashboard");
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");

  return (
    <DashboardHub
      title={t("consultant.title", { name: firstName })}
      subtitle={t("consultant.subtitle")}
      cards={[
        {
          icon: Clock,
          to: "/consultant/availability",
          title: t("consultant.cards.availability.title"),
          description: t("consultant.cards.availability.desc"),
        },
        {
          icon: CalendarCheck,
          to: "/consultant/bookings",
          title: t("consultant.cards.bookings.title"),
          description: t("consultant.cards.bookings.desc"),
        },
        {
          icon: Wallet,
          to: "/consultant/earnings",
          title: t("consultant.cards.earnings.title"),
          description: t("consultant.cards.earnings.desc"),
        },
      ]}
    />
  );
}
