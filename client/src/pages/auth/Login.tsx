import { useMemo } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion } from "motion/react";
import { toast } from "sonner";
import { Loader2, ArrowRight } from "lucide-react";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { ApiError, apiErrorMessage } from "@/services/api/client";
import {
  AuthShell,
  AuthField,
  MobileBrandMark,
  Separator,
  SsoButtons,
} from "./_AuthShell";

function makeLoginSchema(t: TFunction) {
  return z.object({
    email: z.string().email(t("errors:validate.email")),
    password: z.string().min(8, t("errors:validate.passwordMin")),
    rememberMe: z.boolean(),
  });
}

type LoginInput = z.infer<ReturnType<typeof makeLoginSchema>>;

/**
 * Whitelists ?redirect=… values to in-app paths only, defeating open-redirect
 * abuse via crafted login URLs.
 * Accepts: "/", "/student/scholarships", "/student/scholarships?tab=open"
 * Rejects: "https://evil.com", "//evil.com", "javascript:…", ".."
 */
function safeRedirect(raw: string | null): string | null {
  if (!raw) return null;
  // Must start with a single forward slash and not be protocol-relative.
  if (!raw.startsWith("/") || raw.startsWith("//")) return null;
  // Block any embedded scheme just in case (e.g. "/foo?next=javascript:…" — fine,
  // but a bare "javascript:" prefix isn't possible after the above guards).
  if (/^\/[\s]/.test(raw)) return null;
  return raw;
}

/**
 * Maps each top-level role prefix to the role that's allowed to enter it.
 * Used to reject a ?redirect=… target the user can't actually open with
 * their current active role (e.g. admin clicks "Pricing" in the footer,
 * which points to /student/scholarships — without this guard they'd land
 * on a 404 / blank page after sign-in).
 */
const ROLE_PREFIX_OWNERS: Readonly<Record<string, readonly string[]>> = {
  "/student":    ["Student"],
  "/company":    ["Company"],
  "/consultant": ["Consultant"],
  "/admin":      ["Admin", "SuperAdmin"],
};

/**
 * True when the chosen ?redirect=… is reachable by the user's active role.
 * Shared / unscoped paths (/profile, /notifications, /legal/*, /help) are
 * always accepted because every authenticated user can open them.
 */
function isRedirectReachable(path: string, activeRole: string | null): boolean {
  for (const [prefix, owners] of Object.entries(ROLE_PREFIX_OWNERS)) {
    if (path === prefix || path.startsWith(`${prefix}/`)) {
      return activeRole !== null && owners.includes(activeRole);
    }
  }
  return true;
}

export function Login() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const redirectParam = safeRedirect(searchParams.get("redirect"));
  const loginSchema = useMemo(() => makeLoginSchema(t), [t]);
  const form = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "", rememberMe: false },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const user = applyAuthSession(await authApi.login(values));
      // If the caller carried a ?redirect=… and the user has finished
      // onboarding AND can actually open the target with their active role,
      // honour it. Otherwise fall back to the role-specific home — e.g.
      // an admin who lands on /login?redirect=/student/community would get
      // a blank screen if we honoured the redirect blindly.
      const canHonourRedirect =
        redirectParam !== null
        && user.isOnboardingComplete
        && isRedirectReachable(redirectParam, user.activeRole ?? null);
      const destination = canHonourRedirect ? redirectParam! : postAuthPath(user);
      navigate(destination, { replace: true });
    } catch (err) {
      const status = err instanceof ApiError ? err.status : 0;
      toast.error(
        status === 409
          ? t("auth:errors.loginFailed")
          : apiErrorMessage(err, t("auth:errors.generic")),
      );
    }
  });

  const isSubmitting = form.formState.isSubmitting;

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
          {t("auth:login.title")}
        </h1>
        <p className="mt-2 text-base text-text-secondary">
          {t("auth:login.subtitle")}
        </p>

        <div className="mt-8">
          <SsoButtons />
        </div>

        <Separator text={t("auth:sso.or")} />

        <form onSubmit={(e) => void onSubmit(e)} className="space-y-5" noValidate>
          <AuthField
            id="email"
            type="email"
            autoComplete="email"
            label={t("auth:login.emailLabel")}
            placeholder={t("auth:login.emailPlaceholder")}
            error={form.formState.errors.email?.message}
            inputProps={form.register("email")}
          />
          <AuthField
            id="password"
            type="password"
            autoComplete="current-password"
            label={t("auth:login.passwordLabel")}
            placeholder={t("auth:login.passwordPlaceholder")}
            error={form.formState.errors.password?.message}
            inputProps={form.register("password")}
          />

          <div className="flex items-center justify-between text-sm">
            <label className="inline-flex cursor-pointer items-center gap-2 text-text-secondary">
              <input
                type="checkbox"
                className="size-4 rounded border-border-default accent-brand-500"
                {...form.register("rememberMe")}
              />
              {t("auth:login.rememberMe")}
            </label>
            <Link
              to="/forgot-password"
              className="font-medium text-brand-600 transition hover:text-brand-700 dark:text-brand-500 dark:hover:text-brand-400"
            >
              {t("auth:login.forgot")}
            </Link>
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className="group inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-brand-500 to-brand-700 px-6 text-sm font-semibold text-white shadow-brand transition-all duration-200 hover:shadow-[0_8px_32px_rgb(23_96_240/0.42)] hover:brightness-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? (
              <Loader2 size={18} className="animate-spin" aria-hidden />
            ) : (
              <>
                {t("auth:login.submit")}
                <ArrowRight
                  aria-hidden
                  className="size-4 transition-transform duration-200 group-hover:translate-x-0.5 rtl:rotate-180 rtl:group-hover:-translate-x-0.5"
                />
              </>
            )}
          </button>
        </form>

        <p className="mt-8 text-center text-sm text-text-secondary">
          {t("auth:login.noAccount")}{" "}
          <Link
            to="/register"
            className="font-semibold text-brand-600 transition hover:text-brand-700 dark:text-brand-500 dark:hover:text-brand-400"
          >
            {t("auth:login.register")}
          </Link>
        </p>
      </motion.div>
    </AuthShell>
  );
}
