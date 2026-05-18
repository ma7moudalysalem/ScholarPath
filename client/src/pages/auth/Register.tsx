import { useMemo } from "react";
import { Link, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion } from "motion/react";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { ApiError } from "@/services/api/client";

function makeRegisterSchema(t: TFunction) {
  return z.object({
    firstName: z
      .string()
      .min(1, t("errors:validate.required"))
      .max(100, t("errors:validate.tooLong")),
    lastName: z
      .string()
      .min(1, t("errors:validate.required"))
      .max(100, t("errors:validate.tooLong")),
    email: z.string().email(t("errors:validate.email")),
    password: z
      .string()
      .min(8, t("errors:validate.passwordMin"))
      .regex(/[A-Z]/, t("errors:validate.passwordUppercase"))
      .regex(/[0-9]/, t("errors:validate.passwordDigit"))
      .regex(/[^a-zA-Z0-9]/, t("errors:validate.passwordSpecial")),
  });
}

type RegisterInput = z.infer<ReturnType<typeof makeRegisterSchema>>;

export function Register() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const registerSchema = useMemo(() => makeRegisterSchema(t), [t]);
  const form = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: { firstName: "", lastName: "", email: "", password: "" },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      const user = applyAuthSession(await authApi.register(values));
      navigate(postAuthPath(user), { replace: true });
    } catch (err) {
      const status = err instanceof ApiError ? err.status : 0;
      toast.error(t(status === 409 ? "auth:errors.emailTaken" : "auth:errors.generic"));
    }
  });

  const isSubmitting = form.formState.isSubmitting;

  return (
    <div className="relative flex min-h-[calc(100vh-3.5rem)] items-center justify-center overflow-hidden px-4 py-12">
      {/* Backdrop glow */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-80 bg-[radial-gradient(ellipse_70%_50%_at_50%_-10%,rgba(23,96,240,0.09),transparent)]"
      />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
        className="w-full max-w-md"
      >
        {/* Form card */}
        <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-8 shadow-md sm:p-10">
          {/* Brand mark */}
          <Link to="/" className="mb-8 flex items-center gap-2 text-sm font-semibold text-text-primary">
            <span className="flex size-7 items-center justify-center rounded-lg bg-brand-500 text-white text-xs font-bold">S</span>
            ScholarPath
          </Link>

          <h1 className="mb-1.5 text-2xl font-bold tracking-tight">
            {t("auth:register.title")}
          </h1>
          <p className="mb-8 text-sm text-text-secondary">
            {t("auth:register.subtitle")}
          </p>

          <form onSubmit={(e) => void onSubmit(e)} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <Field
                id="firstName"
                type="text"
                label={t("auth:register.firstName")}
                error={form.formState.errors.firstName?.message}
                registration={form.register("firstName")}
              />
              <Field
                id="lastName"
                type="text"
                label={t("auth:register.lastName")}
                error={form.formState.errors.lastName?.message}
                registration={form.register("lastName")}
              />
            </div>

            <Field
              id="email"
              type="email"
              label={t("auth:register.emailLabel")}
              error={form.formState.errors.email?.message}
              registration={form.register("email")}
            />

            <Field
              id="password"
              type="password"
              label={t("auth:register.passwordLabel")}
              hint={t("auth:register.passwordHint")}
              error={form.formState.errors.password?.message}
              registration={form.register("password")}
            />

            <button
              type="submit"
              disabled={isSubmitting}
              className="cta-pill btn-brand mt-2 h-11 w-full bg-brand-500 text-sm text-white disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isSubmitting ? (
                <Loader2 size={16} className="animate-spin" aria-hidden />
              ) : (
                t("auth:register.submit")
              )}
            </button>
          </form>

          <Separator text={t("auth:sso.or")} />
          <SsoButtons />

          <p className="mt-7 text-center text-sm text-text-secondary">
            {t("auth:register.hasAccount")}{" "}
            <Link
              to="/login"
              className="font-semibold text-brand-500 transition hover:text-brand-600 hover:underline"
            >
              {t("auth:register.signIn")}
            </Link>
          </p>
        </div>
      </motion.div>
    </div>
  );
}

type FieldRegistration = ReturnType<ReturnType<typeof useForm<RegisterInput>>["register"]>;

function Field({
  id,
  label,
  type,
  hint,
  error,
  registration,
}: {
  id: string;
  label: string;
  type: string;
  hint?: string;
  error?: string;
  registration: FieldRegistration;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="text-sm font-medium text-text-primary">
        {label}
      </label>
      <input
        id={id}
        type={type}
        className={[
          "h-11 w-full rounded-xl border bg-bg-subtle px-4 text-sm text-text-primary placeholder:text-text-tertiary",
          "transition focus:border-brand-500 focus:bg-bg-elevated focus:outline-none focus:ring-2 focus:ring-brand-500/20",
          error ? "border-danger-400" : "border-border-default",
        ].join(" ")}
        {...registration}
      />
      {hint && !error && <p className="text-xs text-text-tertiary">{hint}</p>}
      {error && <p className="text-xs text-danger-500">{error}</p>}
    </div>
  );
}

function Separator({ text }: { text: string }) {
  return (
    <div className="my-6 flex items-center gap-3 text-xs font-medium uppercase tracking-wider text-text-tertiary">
      <div className="h-px flex-1 bg-border-subtle" />
      {text}
      <div className="h-px flex-1 bg-border-subtle" />
    </div>
  );
}

function SsoButtons() {
  const { t } = useTranslation("auth");
  return (
    <div className="space-y-2.5">
      <button
        type="button"
        onClick={() => authApi.beginSso("google")}
        className="flex h-11 w-full items-center justify-center gap-3 rounded-xl border border-border-default bg-bg-subtle px-4 text-sm font-medium text-text-primary transition hover:border-border-strong hover:bg-bg-elevated"
      >
        <GoogleIcon />
        {t("sso.google")}
      </button>
      <button
        type="button"
        onClick={() => authApi.beginSso("microsoft")}
        className="flex h-11 w-full items-center justify-center gap-3 rounded-xl border border-border-default bg-bg-subtle px-4 text-sm font-medium text-text-primary transition hover:border-border-strong hover:bg-bg-elevated"
      >
        <MicrosoftIcon />
        {t("sso.microsoft")}
      </button>
    </div>
  );
}

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4 shrink-0" aria-hidden>
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
    </svg>
  );
}

function MicrosoftIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4 shrink-0" aria-hidden>
      <path d="M11.4 2H2v9.4h9.4V2z" fill="#F35325" />
      <path d="M22 2h-9.4v9.4H22V2z" fill="#81BC06" />
      <path d="M11.4 12.6H2V22h9.4v-9.4z" fill="#05A6F0" />
      <path d="M22 12.6h-9.4V22H22v-9.4z" fill="#FFBA08" />
    </svg>
  );
}
