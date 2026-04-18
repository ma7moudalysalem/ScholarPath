import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion } from "motion/react";
import { EmptyState } from "@/components/common/EmptyState";

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
  rememberMe: z.boolean(),
});

type LoginInput = z.infer<typeof loginSchema>;

export function Login() {
  const { t } = useTranslation(["auth", "common"]);
  const form = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "", rememberMe: false },
  });

  const onSubmit = form.handleSubmit(() => {
    // @Madiha6776: wire to LoginCommand via apiClient.post("/api/auth/login", payload)
  });

  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32 }}
      >
        <h1 className="mb-2 text-3xl">{t("auth:login.title")}</h1>
        <p className="mb-8 text-text-secondary">{t("auth:login.subtitle")}</p>

        <form onSubmit={(e) => void onSubmit(e)} className="space-y-4">
          <Field
            id="email"
            type="email"
            label={t("auth:login.emailLabel")}
            error={form.formState.errors.email?.message}
            registration={form.register("email")}
          />
          <Field
            id="password"
            type="password"
            label={t("auth:login.passwordLabel")}
            error={form.formState.errors.password?.message}
            registration={form.register("password")}
          />
          <div className="flex items-center justify-between text-sm">
            <label className="inline-flex items-center gap-2 text-text-secondary">
              <input
                type="checkbox"
                className="size-4 rounded border-border-default text-brand-500 focus:ring-brand-500"
                {...form.register("rememberMe")}
              />
              {t("auth:login.rememberMe")}
            </label>
            <Link to="/forgot-password" className="text-brand-500 hover:underline">
              {t("auth:login.forgot")}
            </Link>
          </div>

          <button
            type="submit"
            disabled={form.formState.isSubmitting}
            className="cta-pill mt-2 w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
          >
            {t("auth:login.submit")}
          </button>
        </form>

        <Separator text={t("auth:sso.or")} />

        <SsoButtons />

        <p className="mt-8 text-center text-sm text-text-secondary">
          {t("auth:login.noAccount")}{" "}
          <Link to="/register" className="font-medium text-brand-500 hover:underline">
            {t("auth:login.register")}
          </Link>
        </p>
      </motion.div>

      <div className="mt-10">
        <EmptyState
          owner="@Madiha6776"
          module="PB-001 Auth — wire LoginCommand + token persistence"
          specPath=".specify/specs/PB-001-auth-access-onboarding/tasks.md"
        />
      </div>
    </div>
  );
}

type FieldRegistration = ReturnType<ReturnType<typeof useForm<LoginInput>>["register"]>;

function Field({
  id,
  label,
  type,
  error,
  registration,
}: {
  id: string;
  label: string;
  type: string;
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
        className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary shadow-xs transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/30"
        {...registration}
      />
      {error && <p className="text-xs text-danger-500">{error}</p>}
    </div>
  );
}

function Separator({ text }: { text: string }) {
  return (
    <div className="my-6 flex items-center gap-3 text-xs uppercase text-text-tertiary">
      <div className="h-px flex-1 bg-border-subtle" />
      {text}
      <div className="h-px flex-1 bg-border-subtle" />
    </div>
  );
}

function SsoButtons() {
  const { t } = useTranslation("auth");
  return (
    <div className="space-y-2">
      <button
        type="button"
        className="flex w-full items-center justify-center gap-3 rounded-md border border-border-default bg-bg-elevated px-4 py-2.5 text-sm font-medium text-text-primary transition hover:bg-bg-subtle"
        onClick={() => {
          // @Madiha6776: redirect to /api/auth/google/authorize?redirectUri=...
        }}
      >
        {t("sso.google")}
      </button>
      <button
        type="button"
        className="flex w-full items-center justify-center gap-3 rounded-md border border-border-default bg-bg-elevated px-4 py-2.5 text-sm font-medium text-text-primary transition hover:bg-bg-subtle"
        onClick={() => {
          // @Madiha6776: redirect to /api/auth/microsoft/authorize?redirectUri=...
        }}
      >
        {t("sso.microsoft")}
      </button>
    </div>
  );
}
