import { useEffect } from "react";
import { useNavigate } from "react-router";
import { EmptyState } from "@/components/common/EmptyState";

export function SsoCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    // @Madiha6776: parse ?code and ?state from URL, POST to /api/auth/google|microsoft/callback,
    // persist tokens, then navigate to onboarding (new user) or dashboard (existing)
    const timeout = window.setTimeout(() => {
      navigate("/onboarding", { replace: true });
    }, 1200);
    return () => window.clearTimeout(timeout);
  }, [navigate]);

  return (
    <div className="mx-auto max-w-md p-10 text-center">
      <EmptyState
        owner="@Madiha6776"
        module="PB-001 SSO callback"
        specPath=".specify/specs/PB-001-auth-access-onboarding/tasks.md"
      />
    </div>
  );
}
