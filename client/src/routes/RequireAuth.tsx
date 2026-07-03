import { Navigate, useLocation } from "react-router";
import type { ReactNode } from "react";
import { useAuthStore } from "@/stores/authStore";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";

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
  ScholarshipProvider: "/company",
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

/**
 * Free-mode gate for pages that exist ONLY to show money — earnings, billing,
 * payouts, revenue reports, profit-share and financial-config admin. When the
 * platform master switch `payments.enabled` is off, the whole platform runs
 * free, so these pages have nothing to show and must not be reachable at all
 * (the nav links are hidden too, but a deep link would otherwise still render
 * them). Redirect to the caller's dashboard.
 *
 * `paymentsEnabled` defaults to `true` until `/api/status` resolves, matching
 * the platform-wide "don't break the app while status is loading" stance, so a
 * deep link renders briefly and then redirects once the flag lands.
 */
export function RequirePayments({ children }: { children: ReactNode }) {
  const paymentsEnabled = usePaymentsEnabled();
  const user = useAuthStore((s) => s.user);
  if (!paymentsEnabled) {
    const active = user?.activeRole ?? user?.roles[0];
    return <Navigate to={ROLE_HOME[active ?? ""] ?? "/"} replace />;
  }
  return <>{children}</>;
}
