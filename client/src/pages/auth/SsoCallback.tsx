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

  // Capture the handshake ONCE on mount. completeSso clears the stashed provider
  // from storage, and re-reading provider/params on every render would flip the
  // screen to the error state WHILE the exchange is still succeeding — the bug
  // where SSO briefly showed "Sign-in could not be completed" and then logged in.
  // SEC-06 / GAP-2 — the provider echoes the `state` nonce we sent; forward it so
  // the server can validate the handshake and reject forged callbacks.
  const [handshake] = useState(() => ({
    code: params.get("code"),
    state: params.get("state"),
    provider: authApi.pendingSsoProvider(),
  }));
  const { code, state, provider } = handshake;

  useEffect(() => {
    if (ran.current || !code || !provider || !state) return;
    ran.current = true;

    authApi
      .completeSso(provider, code, state)
      .then((res) => {
        const user = applyAuthSession(res);
        toast.success(t("auth:login.welcomeBack", { name: user.firstName || user.fullName }));
        navigate(postAuthPath(user), { replace: true });
      })
      .catch((err) => {
        setExchangeFailed(true);
        toast.error(apiErrorMessage(err, t("auth:errors.ssoFailed")));
      });
  }, [code, provider, state, navigate, t]);

  // A missing code / pending-provider is knowable at render time — no effect
  // is needed to flag it; only the async token exchange sets state (in catch).
  const failed = exchangeFailed || !code || !provider || !state;

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
