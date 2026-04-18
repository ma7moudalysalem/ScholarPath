import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { EmptyState } from "@/components/common/EmptyState";

export function ForgotPassword() {
  const { t } = useTranslation(["auth", "common"]);
  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <h1 className="mb-2 text-3xl">{t("auth:forgotPassword.title")}</h1>
      <p className="mb-8 text-text-secondary">{t("auth:forgotPassword.subtitle")}</p>

      <form className="space-y-4">
        <div className="space-y-1.5">
          <label htmlFor="email" className="text-sm font-medium text-text-primary">
            {t("auth:forgotPassword.emailLabel")}
          </label>
          <input
            id="email"
            type="email"
            className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
          />
        </div>
        <button
          type="submit"
          className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
        >
          {t("auth:forgotPassword.submit")}
        </button>
      </form>

      <Link to="/login" className="mt-6 text-center text-sm text-brand-500 hover:underline">
        {t("auth:forgotPassword.backToLogin")}
      </Link>

      <div className="mt-10">
        <EmptyState
          owner="@Madiha6776"
          module="PB-001 Forgot/Reset password"
          specPath=".specify/specs/PB-001-auth-access-onboarding/tasks.md"
        />
      </div>
    </div>
  );
}
