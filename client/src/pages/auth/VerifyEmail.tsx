import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { CheckCircle2, XCircle, Loader2 } from "lucide-react";
import { authApi } from "@/services/api/auth";

type Status = "verifying" | "success" | "error";

/**
 * Lands here from the "Verify my email" link in the account-verification email
 * (`/verify-email?userId=…&token=…`). Confirms the address against the API and
 * shows the outcome.
 */
export function VerifyEmail() {
  const { t } = useTranslation("auth");
  const [searchParams] = useSearchParams();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token");

  // A link missing its params is an error from the first render — deriving the
  // initial state avoids a setState call inside the effect.
  const [status, setStatus] = useState<Status>(userId && token ? "verifying" : "error");
  const startedRef = useRef(false);

  useEffect(() => {
    // The token is single-use — guard against React StrictMode's double-invoke
    // so we don't consume it twice and report a false failure.
    if (startedRef.current || !userId || !token) return;
    startedRef.current = true;

    authApi
      .verifyEmail(userId, token)
      .then(() => setStatus("success"))
      .catch(() => setStatus("error"));
  }, [userId, token]);

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
          <p className="mb-6 text-text-secondary">{t("verifyEmail.errorBody")}</p>
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
