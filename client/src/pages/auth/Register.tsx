import { useMemo } from "react";
import { Link, useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion } from "motion/react";
import { toast } from "sonner";
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

  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32 }}
      >
        <h1 className="mb-2 text-3xl">{t("auth:register.title")}</h1>
        <p className="mb-8 text-text-secondary">{t("auth:register.subtitle")}</p>

        <form onSubmit={(e) => void onSubmit(e)} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Field
              id="firstName"
              type="text"
              label={t("auth:register.firstName")}
              error={form.formState.errors.firstName?.message}
              {...form.register("firstName")}
            />
            <Field
              id="lastName"
              type="text"
              label={t("auth:register.lastName")}
              error={form.formState.errors.lastName?.message}
              {...form.register("lastName")}
            />
          </div>
          <Field
            id="email"
            type="email"
            label={t("auth:register.emailLabel")}
            error={form.formState.errors.email?.message}
            {...form.register("email")}
          />
          <Field
            id="password"
            type="password"
            label={t("auth:register.passwordLabel")}
            hint={t("auth:register.passwordHint")}
            error={form.formState.errors.password?.message}
            {...form.register("password")}
          />

          <button
            type="submit"
            disabled={form.formState.isSubmitting}
            className="cta-pill mt-2 w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
          >
            {t("auth:register.submit")}
          </button>
        </form>

        <p className="mt-8 text-center text-sm text-text-secondary">
          {t("auth:register.hasAccount")}{" "}
          <Link to="/login" className="font-medium text-brand-500 hover:underline">
            {t("auth:register.signIn")}
          </Link>
        </p>
      </motion.div>
    </div>
  );
}

interface FieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  id: string;
  label: string;
  hint?: string;
  error?: string;
}

const Field = ({ id, label, hint, error, ...rest }: FieldProps) => (
  <div className="space-y-1.5">
    <label htmlFor={id} className="text-sm font-medium text-text-primary">
      {label}
    </label>
    <input
      id={id}
      className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary shadow-xs transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
      {...rest}
    />
    {hint && !error && <p className="text-xs text-text-tertiary">{hint}</p>}
    {error && <p className="text-xs text-danger-500">{error}</p>}
  </div>
);
