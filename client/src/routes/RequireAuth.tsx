import { Navigate, useLocation } from "react-router";
import type { ReactNode } from "react";
import { useAuthStore } from "@/stores/authStore";

export function RequireAuth({ children }: { children: ReactNode }) {
  const tokens = useAuthStore((s) => s.tokens);
  const location = useLocation();

  if (!tokens) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }
  return <>{children}</>;
}

/** Where to send a user who lacks the role a route requires — their own home. */
const ROLE_HOME: Record<string, string> = {
  Student: "/student",
  Company: "/company",
  Consultant: "/consultant",
  Admin: "/admin",
  SuperAdmin: "/admin",
};

export function RequireRole({
  roles,
  children,
}: {
  roles: string[];
  children: ReactNode;
}) {
  const user = useAuthStore((s) => s.user);
  if (!user) return <Navigate to="/login" replace />;
  const active = user.activeRole ?? user.roles[0];
  if (!active || !roles.includes(active)) {
    // Redirect to the user's own dashboard — never a fixed path, which would
    // loop when the redirect target is itself role-guarded.
    return <Navigate to={ROLE_HOME[active ?? ""] ?? "/"} replace />;
  }
  return <>{children}</>;
}
