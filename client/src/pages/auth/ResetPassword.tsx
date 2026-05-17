import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { authApi } from "@/services/api/auth";

export function ResetPassword() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const token = params.get("token") ?? "";

  const [newPassword, setNewPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirm) {
      toast.error(t("auth:resetPassword.mismatch"));
      return;
    }
    setSubmitting(true);
    try {
      await authApi.resetPassword(token, newPassword);
      toast.success(t("auth:resetPassword.success"));
      navigate("/login", { replace: true });
    } catch {
      toast.error(t("auth:errors.generic"));
    } finally {
      setSubmitting(false);
    }
  };

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

  return (
    <div className="mx-auto flex min-h-[calc(100vh-3.5rem)] max-w-md flex-col [justify-content:safe_center] px-4 py-12 sm:px-6">
      <h1 className="mb-2 text-3xl">{t("auth:resetPassword.title")}</h1>
      <form className="mt-8 space-y-4" onSubmit={(e) => void onSubmit(e)}>
        <Field
          id="newPassword"
          label={t("auth:resetPassword.newPasswordLabel")}
          value={newPassword}
          onChange={setNewPassword}
        />
        <Field
          id="confirm"
          label={t("auth:resetPassword.confirmLabel")}
          value={confirm}
          onChange={setConfirm}
        />
        <button
          type="submit"
          disabled={submitting}
          className="cta-pill w-full bg-text-primary py-3 text-base text-text-inverse hover:bg-text-primary/90 disabled:opacity-50 dark:bg-brand-500 dark:text-text-on-brand"
        >
          {t("auth:resetPassword.submit")}
        </button>
      </form>
    </div>
  );
}

const Field = ({
  id,
  label,
  value,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (v: string) => void;
}) => (
  <div className="space-y-1.5">
    <label htmlFor={id} className="text-sm font-medium text-text-primary">
      {label}
    </label>
    <input
      id={id}
      type="password"
      required
      minLength={8}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
    />
  </div>
);
