import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { CheckCircle2, XCircle, Loader2, Mail } from "lucide-react";
import { toast } from "sonner";
import { authApi } from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";
import { useAuthStore } from "@/stores/authStore";

type Status = "verifying" | "success" | "error";

/**
 * Lands here from the "Verify my email" link in the account-verification email
 * (`/verify-email?userId=…&token=…`). Confirms the address against the API and
 * shows the outcome. On failure the user can request a fresh link.
 */
export function VerifyEmail() {
  const { t } = useTranslation("auth");
  const [searchParams] = useSearchParams();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token");
  const sessionUser = useAuthStore((s) => s.user);

  // A link missing its params is an error from the first render — deriving the
  // initial state avoids a setState call inside the effect.
  const [status, setStatus] = useState<Status>(userId && token ? "verifying" : "error");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const startedRef = useRef(false);

  // Resend uses the email from the signed-in session if available, otherwise
  // a small inline form prompts for it (we don't trust querystring email).
  const [resendEmail, setResendEmail] = useState(sessionUser?.email ?? "");
  const [resending, setResending] = useState(false);

  useEffect(() => {
    // The token is single-use — guard against React StrictMode's double-invoke
    // so we don't consume it twice and report a false failure.
    if (startedRef.current || !userId || !token) return;
    startedRef.current = true;

    authApi
      .verifyEmail(userId, token)
      .then(() => setStatus("success"))
      .catch((err: unknown) => {
        setErrorMessage(apiErrorMessage(err, t("verifyEmail.errorBody")));
        setStatus("error");
      });
  }, [userId, token, t]);

  async function onResend() {
    if (!resendEmail.trim() || resending) return;
    setResending(true);
    try {
      await authApi.resendVerification(resendEmail.trim());
      // Backend always 204s — keep the message non-enumerating.
      toast.success(t("verifyEmail.resend.sent", "If the email is registered, a new verification link is on the way."));
    } catch (err) {
      toast.error(apiErrorMessage(err, t("verifyEmail.errorBody")));
    } finally {
      setResending(false);
    }
  }

  return (
    <section className="mx-auto flex max-w-md flex-col items-center px-4 py-20 text-center sm:px-6">
      {status === "verifying" && (
        <>
          <Loader2 aria-hidden className="mb-4 size-10 animate-spin text-brand-500" />
          <p className="text-text-secondary">{t("verifyEmail.verifying")}</p>
        </>
      )}

      {status === "success" && (
        <>
          <CheckCircle2 aria-hidden className="mb-4 size-12 text-success-500" />
          <h1 className="mb-2 text-2xl font-semibold text-text-primary">
            {t("verifyEmail.successTitle")}
          </h1>
          <p className="mb-6 text-text-secondary">{t("verifyEmail.successBody")}</p>
          <Link
            to="/login"
            className="inline-flex h-11 items-center justify-center rounded-lg bg-brand-500 px-6 text-sm font-medium text-white transition hover:bg-brand-600"
          >
            {t("verifyEmail.goToLogin")}
          </Link>
        </>
      )}

      {status === "error" && (
        <>
          <XCircle aria-hidden className="mb-4 size-12 text-danger-500" />
          <h1 className="mb-2 text-2xl font-semibold text-text-primary">
            {t("verifyEmail.errorTitle")}
          </h1>
          <p className="mb-6 text-text-secondary">{errorMessage ?? t("verifyEmail.errorBody")}</p>

          <div className="mb-6 w-full rounded-lg border border-border-subtle bg-bg-elevated p-4 text-start">
            <h2 className="mb-1 text-sm font-semibold text-text-primary">
              {t("verifyEmail.resend.title", "Need a new link?")}
            </h2>
            <p className="mb-3 text-xs text-text-secondary">
              {t(
                "verifyEmail.resend.subtitle",
                "We'll email a fresh verification link to your account.",
              )}
            </p>
            <div className="flex gap-2">
              <input
                type="email"
                value={resendEmail}
                onChange={(e) => setResendEmail(e.target.value)}
                placeholder={t("verifyEmail.resend.emailPlaceholder", "you@example.com")}
                disabled={resending}
                aria-label={t("verifyEmail.resend.emailLabel", "Email")}
                className="h-10 min-w-0 flex-1 rounded-md border border-border-default bg-bg-subtle px-3 text-sm focus:border-brand-500 focus:outline-none"
              />
              <button
                type="button"
                onClick={() => void onResend()}
                disabled={!resendEmail.trim() || resending}
                className="inline-flex h-10 shrink-0 items-center gap-2 rounded-md bg-brand-500 px-3 text-sm font-medium text-white hover:bg-brand-600 disabled:opacity-50"
              >
                {resending ? (
                  <Loader2 aria-hidden className="size-4 animate-spin" />
                ) : (
                  <Mail aria-hidden className="size-4" />
                )}
                {t("verifyEmail.resend.cta", "Resend")}
              </button>
            </div>
          </div>

          <Link
            to="/login"
            className="inline-flex h-11 items-center justify-center rounded-lg bg-brand-500 px-6 text-sm font-medium text-white transition hover:bg-brand-600"
          >
            {t("verifyEmail.goToLogin")}
          </Link>
        </>
      )}
    </section>
  );
}
