import { useAuthStore } from "@/stores/authStore";
import { AdminLayout } from "@/components/layout/AdminLayout";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";

/**
 * Layout dispatcher for routes shared by every role — profile, notifications,
 * settings. It exists to kill the "the sidebar changes for the same user" bug:
 *
 * AuthenticatedLayout picks its sidebar from NAV_BY_ROLE[activeRole], which has
 * entries for Student / Consultant / ScholarshipProvider but NONE for Admin or
 * SuperAdmin. So an admin visiting /notifications or /profile fell through to
 * the STUDENT sidebar — the admin saw the full admin shell on /admin/* but a
 * student shell the moment they opened a shared page. Same user, two different
 * navigations, exactly the ambiguity the team lead flagged.
 *
 * Rendering AdminLayout for admins keeps the shell pixel-identical everywhere.
 * Consultants / Providers / Students already resolve to the right sidebar
 * inside AuthenticatedLayout, so they keep it unchanged. Both layouts render an
 * <Outlet/>, so this drops in as a layout-route element with no other changes.
 */
export function RoleLayout() {
  const user = useAuthStore((s) => s.user);
  const role = user?.activeRole ?? user?.roles?.[0] ?? "Student";
  const isAdmin = role === "Admin" || role === "SuperAdmin";
  return isAdmin ? <AdminLayout /> : <AuthenticatedLayout />;
}
