import { useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { authApi, applyAuthSession, postAuthPath } from "@/services/api/auth";
import { apiErrorMessage } from "@/services/api/client";

export function SsoCallback() {
  const { t } = useTranslation(["auth", "common"]);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [exchangeFailed, setExchangeFailed] = useState(false);
  const ran = useRef(false);

  const code = params.get("code");
  const provider = authApi.pendingSsoProvider();

  useEffect(() => {
    if (ran.current || !code || !provider) return;
    ran.current = true;

    authApi
      .completeSso(provider, code)
      .then((res) => {
        const user = applyAuthSession(res);
        toast.success(t("auth:login.welcomeBack", { name: user.firstName || user.fullName }));
        navigate(postAuthPath(user), { replace: true });
      })
      .catch((err) => {
        setExchangeFailed(true);
        toast.error(apiErrorMessage(err, t("auth:errors.ssoFailed")));
      });
  }, [code, provider, navigate, t]);

  // A missing code / pending-provider is knowable at render time — no effect
  // is needed to flag it; only the async token exchange sets state (in catch).
  const failed = exchangeFailed || !code || !provider;

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
