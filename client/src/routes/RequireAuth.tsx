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
  if (!active || !roles.includes(active)) return <Navigate to="/student" replace />;
  return <>{children}</>;
}
