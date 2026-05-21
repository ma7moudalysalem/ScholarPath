import { useState } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { motion } from "motion/react";
import { ArrowLeft, ArrowRight, CheckCircle2, Loader2 } from "lucide-react";
import { authApi } from "@/services/api/auth";
import { AuthShell, MobileBrandMark } from "./_AuthShell";

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
    <AuthShell>
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
        className="w-full max-w-md"
      >
        <MobileBrandMark />

        <h1 className="font-display text-3xl font-bold tracking-tight text-text-primary sm:text-4xl">
          {t("auth:forgotPassword.title")}
        </h1>
        <p className="mt-2 text-base text-text-secondary">
          {t("auth:forgotPassword.subtitle")}
        </p>

        <div className="mt-8">
          {sent ? (
            <div className="flex items-start gap-3 rounded-xl border border-success-200 bg-success-50 p-4 text-sm text-success-700 dark:border-success-200/40 dark:bg-success-50/40">
              <CheckCircle2
                aria-hidden
                className="mt-0.5 size-5 shrink-0 text-success-500"
              />
              <span>{t("auth:forgotPassword.success")}</span>
            </div>
          ) : (
            <form
              className="space-y-5"
              onSubmit={(e) => void onSubmit(e)}
              noValidate
            >
              <div className="space-y-1.5">
                <label
                  htmlFor="email"
                  className="text-sm font-medium text-text-primary"
                >
                  {t("auth:forgotPassword.emailLabel")}
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder={t("auth:forgotPassword.emailPlaceholder")}
                  autoComplete="email"
                  className="block w-full rounded-xl border border-border-default bg-bg-elevated px-4 py-3 text-sm text-text-primary placeholder:text-text-tertiary transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                />
              </div>

              <button
                type="submit"
                disabled={submitting}
                className="group inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-brand-500 to-brand-700 px-6 text-sm font-semibold text-white shadow-brand transition-all duration-200 hover:shadow-[0_8px_32px_rgb(23_96_240/0.42)] hover:brightness-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {submitting ? (
                  <Loader2 size={18} className="animate-spin" aria-hidden />
                ) : (
                  <>
                    {t("auth:forgotPassword.submit")}
                    <ArrowRight
                      aria-hidden
                      className="size-4 transition-transform duration-200 group-hover:translate-x-0.5 rtl:rotate-180 rtl:group-hover:-translate-x-0.5"
                    />
                  </>
                )}
              </button>
            </form>
          )}
        </div>

        <Link
          to="/login"
          className="mt-8 inline-flex items-center gap-1.5 text-sm font-medium text-brand-600 transition hover:text-brand-700 dark:text-brand-500 dark:hover:text-brand-400"
        >
          <ArrowLeft aria-hidden className="size-4 rtl:rotate-180" />
          {t("auth:forgotPassword.backToLogin")}
        </Link>
      </motion.div>
    </AuthShell>
  );
}
