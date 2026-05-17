import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { authApi } from "@/services/api/auth";

export function ForgotPassword() {
  const { t } = useTranslation(["auth", "common"]);
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await authApi.forgotPassword(email);
    } catch {
      // Endpoint is intentionally non-enumerating — surface success either way.
    } finally {
      setSubmitting(false);
      setSent(true);
    }
  };

  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <h1 className="mb-2 text-3xl">{t("auth:forgotPassword.title")}</h1>
      <p className="mb-8 text-text-secondary">{t("auth:forgotPassword.subtitle")}</p>

      {sent ? (
        <p className="rounded-md bg-success-50 p-4 text-sm text-text-primary">
          {t("auth:forgotPassword.success")}
        </p>
      ) : (
        <form className="space-y-4" onSubmit={(e) => void onSubmit(e)}>
          <div className="space-y-1.5">
            <label htmlFor="email" className="text-sm font-medium text-text-primary">
              {t("auth:forgotPassword.emailLabel")}
            </label>
            <input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
            />
          </div>
          <button
            type="submit"
            disabled={submitting}
            className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 disabled:opacity-50 dark:bg-brand-500 dark:text-text-on-brand"
          >
            {t("auth:forgotPassword.submit")}
          </button>
        </form>
      )}

      <Link to="/login" className="mt-6 text-center text-sm text-brand-500 hover:underline">
        {t("auth:forgotPassword.backToLogin")}
      </Link>
    </div>
  );
}
