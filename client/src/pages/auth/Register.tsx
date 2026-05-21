import { useMemo } from "react";
import { Link, useNavigate } from "react-router";
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
      toast.error(
        status === 409
          ? t("auth:errors.emailTaken")
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
          {t("auth:register.title")}
        </h1>
        <p className="mt-2 text-base text-text-secondary">
          {t("auth:register.subtitle")}
        </p>

        <div className="mt-8">
          <SsoButtons />
        </div>

        <Separator text={t("auth:sso.or")} />

        <form onSubmit={(e) => void onSubmit(e)} className="space-y-5" noValidate>
          <div className="grid grid-cols-2 gap-3">
            <AuthField
              id="firstName"
              type="text"
              autoComplete="given-name"
              label={t("auth:register.firstName")}
              placeholder={t("auth:register.firstNamePlaceholder")}
              error={form.formState.errors.firstName?.message}
              inputProps={form.register("firstName")}
            />
            <AuthField
              id="lastName"
              type="text"
              autoComplete="family-name"
              label={t("auth:register.lastName")}
              placeholder={t("auth:register.lastNamePlaceholder")}
              error={form.formState.errors.lastName?.message}
              inputProps={form.register("lastName")}
            />
          </div>

          <AuthField
            id="email"
            type="email"
            autoComplete="email"
            label={t("auth:register.emailLabel")}
            placeholder={t("auth:register.emailPlaceholder")}
            error={form.formState.errors.email?.message}
            inputProps={form.register("email")}
          />

          <AuthField
            id="password"
            type="password"
            autoComplete="new-password"
            label={t("auth:register.passwordLabel")}
            placeholder={t("auth:register.passwordPlaceholder")}
            hint={t("auth:register.passwordHint")}
            error={form.formState.errors.password?.message}
            inputProps={form.register("password")}
          />

          <button
            type="submit"
            disabled={isSubmitting}
            className="group inline-flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-brand-500 to-brand-700 px-6 text-sm font-semibold text-white shadow-brand transition-all duration-200 hover:shadow-[0_8px_32px_rgb(23_96_240/0.42)] hover:brightness-110 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/40 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? (
              <Loader2 size={18} className="animate-spin" aria-hidden />
            ) : (
              <>
                {t("auth:register.submit")}
                <ArrowRight
                  aria-hidden
                  className="size-4 transition-transform duration-200 group-hover:translate-x-0.5 rtl:rotate-180 rtl:group-hover:-translate-x-0.5"
                />
              </>
            )}
          </button>

          <p className="text-center text-xs leading-relaxed text-text-tertiary">
            {t("auth:register.terms")}
          </p>
        </form>

        <p className="mt-6 text-center text-sm text-text-secondary">
          {t("auth:register.hasAccount")}{" "}
          <Link
            to="/login"
            className="font-semibold text-brand-600 transition hover:text-brand-700 dark:text-brand-500 dark:hover:text-brand-400"
          >
            {t("auth:register.signIn")}
          </Link>
        </p>
      </motion.div>
    </AuthShell>
  );
}
