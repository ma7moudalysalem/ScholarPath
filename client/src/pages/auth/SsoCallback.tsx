import { useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";

export function SsoCallback() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [failed, setFailed] = useState(false);
  const ran = useRef(false);

  useEffect(() => {
    if (ran.current) return;
    ran.current = true;

    const code = params.get("code");
    const provider = authApi.pendingSsoProvider();

    if (!code || !provider) {
      setFailed(true);
      return;
    }

    authApi
      .completeSso(provider, code)
      .then((res) => {
        const user = applyAuthSession(res);
        navigate(postAuthPath(user), { replace: true });
      })
      .catch(() => {
        setFailed(true);
        toast.error(t("auth:errors.ssoFailed"));
      });
  }, [params, navigate, t]);

  return (
    <div className="mx-auto max-w-md px-4 py-20 text-center">
      {failed ? (
        <>
          <p className="mb-4 text-sm text-danger-500">{t("auth:errors.ssoFailed")}</p>
          <button
            type="button"
            onClick={() => navigate("/login", { replace: true })}
            className="cta-pill border border-border-default px-5 py-2 text-sm hover:bg-bg-subtle"
          >
            {t("auth:forgotPassword.backToLogin")}
          </button>
        </>
      ) : (
        <p className="text-sm text-text-secondary">{t("common:status.loading")}</p>
      )}
    </div>
  );
}
