import { useTranslation } from "react-i18next";
import { EmptyState } from "@/components/common/EmptyState";

export function ResetPassword() {
  const { t } = useTranslation(["auth", "common"]);
  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <h1 className="mb-2 text-3xl">{t("auth:resetPassword.title")}</h1>
      <form className="mt-8 space-y-4">
        <Field id="newPassword" label={t("auth:resetPassword.newPasswordLabel")} />
        <Field id="confirm" label={t("auth:resetPassword.confirmLabel")} />
        <button
          type="submit"
          className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
        >
          {t("auth:resetPassword.submit")}
        </button>
      </form>
      <div className="mt-10">
        <EmptyState
          owner="@Madiha6776"
          module="PB-001 Reset password"
          specPath=".specify/specs/PB-001-auth-access-onboarding/tasks.md"
        />
      </div>
    </div>
  );
}

const Field = ({ id, label }: { id: string; label: string }) => (
  <div className="space-y-1.5">
    <label htmlFor={id} className="text-sm font-medium text-text-primary">
      {label}
    </label>
    <input
      id={id}
      type="password"
      className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
    />
  </div>
);
