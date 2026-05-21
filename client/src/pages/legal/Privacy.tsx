import { useTranslation } from "react-i18next";
import { LegalLayout } from "./LegalLayout";

/**
 * Public privacy notice. Plain-language, no legalese — covers what data we
 * collect, why we collect it, and the user's rights under common data
 * protection regimes (PDPL / GDPR style). Wording is deliberately concrete so
 * it stays useful as the platform evolves.
 */
export function Privacy() {
  const { t } = useTranslation(["legal"]);

  return (
    <LegalLayout
      eyebrow={t("legal:eyebrow.legal")}
      title={t("legal:privacy.title")}
      subtitle={t("legal:privacy.subtitle")}
      updatedLabel={t("legal:lastUpdated", { date: "2026-05-21" })}
    >
      <p>{t("legal:privacy.intro")}</p>

      <h2>{t("legal:privacy.collect.heading")}</h2>
      <p>{t("legal:privacy.collect.body")}</p>
      <ul>
        <li>{t("legal:privacy.collect.account")}</li>
        <li>{t("legal:privacy.collect.profile")}</li>
        <li>{t("legal:privacy.collect.usage")}</li>
        <li>{t("legal:privacy.collect.payment")}</li>
        <li>{t("legal:privacy.collect.communications")}</li>
      </ul>

      <h2>{t("legal:privacy.use.heading")}</h2>
      <p>{t("legal:privacy.use.body")}</p>
      <ul>
        <li>{t("legal:privacy.use.matching")}</li>
        <li>{t("legal:privacy.use.bookings")}</li>
        <li>{t("legal:privacy.use.support")}</li>
        <li>{t("legal:privacy.use.safety")}</li>
        <li>{t("legal:privacy.use.legal")}</li>
      </ul>

      <h2>{t("legal:privacy.share.heading")}</h2>
      <p>{t("legal:privacy.share.body")}</p>
      <ul>
        <li>{t("legal:privacy.share.consultants")}</li>
        <li>{t("legal:privacy.share.companies")}</li>
        <li>{t("legal:privacy.share.processors")}</li>
        <li>{t("legal:privacy.share.legal")}</li>
      </ul>

      <h2>{t("legal:privacy.security.heading")}</h2>
      <p>{t("legal:privacy.security.body")}</p>

      <h2>{t("legal:privacy.rights.heading")}</h2>
      <p>{t("legal:privacy.rights.body")}</p>
      <ul>
        <li>{t("legal:privacy.rights.access")}</li>
        <li>{t("legal:privacy.rights.correct")}</li>
        <li>{t("legal:privacy.rights.delete")}</li>
        <li>{t("legal:privacy.rights.export")}</li>
        <li>{t("legal:privacy.rights.optOut")}</li>
      </ul>

      <h2>{t("legal:privacy.retention.heading")}</h2>
      <p>{t("legal:privacy.retention.body")}</p>

      <h2>{t("legal:privacy.contact.heading")}</h2>
      <p>{t("legal:privacy.contact.body")}</p>
    </LegalLayout>
  );
}
