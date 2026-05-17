import { useTranslation } from "react-i18next";
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
} from "lucide-react";
import { DashboardHub } from "@/components/common/DashboardHub";
import { useAuthStore } from "@/stores/authStore";

export function StudentDashboard() {
  const { t } = useTranslation("dashboard");
  const firstName = useAuthStore((s) => s.user?.firstName ?? "");

  return (
    <DashboardHub
      title={t("student.title", { name: firstName })}
      subtitle={t("student.subtitle")}
      cards={[
        {
          icon: GraduationCap,
          to: "/student/scholarships",
          title: t("student.cards.scholarships.title"),
          description: t("student.cards.scholarships.desc"),
        },
        {
          icon: FileText,
          to: "/student/applications",
          title: t("student.cards.applications.title"),
          description: t("student.cards.applications.desc"),
        },
        {
          icon: Bookmark,
          to: "/student/bookmarks",
          title: t("student.cards.bookmarks.title"),
          description: t("student.cards.bookmarks.desc"),
        },
        {
          icon: Users,
          to: "/student/consultants",
          title: t("student.cards.consultants.title"),
          description: t("student.cards.consultants.desc"),
        },
        {
          icon: CalendarCheck,
          to: "/student/bookings",
          title: t("student.cards.bookings.title"),
          description: t("student.cards.bookings.desc"),
        },
        {
          icon: MessageSquare,
          to: "/student/community",
          title: t("student.cards.community.title"),
          description: t("student.cards.community.desc"),
        },
        {
          icon: MessageCircle,
          to: "/student/messages",
          title: t("student.cards.messages.title"),
          description: t("student.cards.messages.desc"),
        },
        {
          icon: Sparkles,
          to: "/student/ai",
          title: t("student.cards.ai.title"),
          description: t("student.cards.ai.desc"),
        },
        {
          icon: BookOpen,
          to: "/student/resources",
          title: t("student.cards.resources.title"),
          description: t("student.cards.resources.desc"),
        },
      ]}
    />
  );
}
