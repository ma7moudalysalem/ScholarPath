import { useMemo } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { authApi } from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";

// Mirrors the Register schema so a user does not bump into stricter rules
// at password reset than they faced at sign-up.
function makeResetSchema(t: TFunction) {
  return z
    .object({
      newPassword: z
        .string()
        .min(8, t("errors:validate.passwordMin"))
        .regex(/[A-Z]/, t("errors:validate.passwordUppercase"))
        .regex(/[0-9]/, t("errors:validate.passwordDigit"))
        .regex(/[^a-zA-Z0-9]/, t("errors:validate.passwordSpecial")),
      confirm: z.string(),
    })
    .refine((v) => v.newPassword === v.confirm, {
      path: ["confirm"],
      message: t("auth:resetPassword.mismatch"),
    });
}

type ResetInput = z.infer<ReturnType<typeof makeResetSchema>>;

export function ResetPassword() {
  const { t } = useTranslation(["auth", "common", "errors"]);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";

  const schema = useMemo(() => makeResetSchema(t), [t]);
  const form = useForm<ResetInput>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: "", confirm: "" },
  });

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await authApi.resetPassword(token, values.newPassword);
      toast.success(t("auth:resetPassword.success"));
      navigate("/login", { replace: true });
    } catch (err) {
      toast.error(apiErrorMessage(err, t("auth:errors.generic")));
    }
  });

  if (!token) {
    return (
      <div className="mx-auto max-w-md px-4 py-16 text-center">
        <h1 className="mb-2 text-3xl">{t("auth:resetPassword.title")}</h1>
        <p className="mb-6 text-sm text-danger-500">{t("auth:resetPassword.invalidLink")}</p>
        <Link to="/forgot-password" className="text-sm text-brand-500 hover:underline">
          {t("auth:forgotPassword.title")}
        </Link>
      </div>
    );
  }

  const isSubmitting = form.formState.isSubmitting;

  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <h1 className="mb-2 text-3xl">{t("auth:resetPassword.title")}</h1>
      <form className="mt-8 space-y-4" onSubmit={(e) => void onSubmit(e)} noValidate>
        <Field
          id="newPassword"
          label={t("auth:resetPassword.newPasswordLabel")}
          error={form.formState.errors.newPassword?.message}
          registration={form.register("newPassword")}
        />
        <p className="text-xs text-text-tertiary">{t("auth:resetPassword.passwordHint")}</p>
        <Field
          id="confirm"
          label={t("auth:resetPassword.confirmLabel")}
          error={form.formState.errors.confirm?.message}
          registration={form.register("confirm")}
        />
        <button
          type="submit"
          disabled={isSubmitting}
          className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 disabled:opacity-50 dark:bg-brand-500 dark:text-text-on-brand"
        >
          {t("auth:resetPassword.submit")}
        </button>
      </form>
    </div>
  );
}

type FieldRegistration = ReturnType<ReturnType<typeof useForm<ResetInput>>["register"]>;

const Field = ({
  id,
  label,
  error,
  registration,
}: {
  id: string;
  label: string;
  error?: string;
  registration: FieldRegistration;
}) => (
  <div className="space-y-1.5">
    <label htmlFor={id} className="text-sm font-medium text-text-primary">
      {label}
    </label>
    <input
      id={id}
      type="password"
      className={[
        "w-full rounded-md border bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none",
        error ? "border-danger-400" : "border-border-default",
      ].join(" ")}
      {...registration}
    />
    {error && <p className="text-xs text-danger-500">{error}</p>}
  </div>
);
