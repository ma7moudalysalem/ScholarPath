import { useTranslation } from "react-i18next";
import { LegalLayout } from "./LegalLayout";

/**
 * Public terms of service. Pairs with the privacy notice — sets out the user
 * obligations, payment / refund terms (Stripe-held bookings), and the limits
 * of liability for the platform.
 */
export function Terms() {
  const { t } = useTranslation(["legal"]);

  return (
    <LegalLayout
      eyebrow={t("legal:eyebrow.legal")}
      title={t("legal:terms.title")}
      subtitle={t("legal:terms.subtitle")}
      updatedLabel={t("legal:lastUpdated", { date: "2026-05-21" })}
    >
      <p>{t("legal:terms.intro")}</p>

      <h2>{t("legal:terms.accounts.heading")}</h2>
      <p>{t("legal:terms.accounts.body")}</p>

      <h2>{t("legal:terms.acceptableUse.heading")}</h2>
      <p>{t("legal:terms.acceptableUse.body")}</p>
      <ul>
        <li>{t("legal:terms.acceptableUse.fraud")}</li>
        <li>{t("legal:terms.acceptableUse.harassment")}</li>
        <li>{t("legal:terms.acceptableUse.scraping")}</li>
        <li>{t("legal:terms.acceptableUse.ip")}</li>
      </ul>

      <h2>{t("legal:terms.payments.heading")}</h2>
      <p>{t("legal:terms.payments.body")}</p>
      <h3>{t("legal:terms.payments.refunds.heading")}</h3>
      <p>{t("legal:terms.payments.refunds.body")}</p>

      <h2>{t("legal:terms.scholarships.heading")}</h2>
      <p>{t("legal:terms.scholarships.body")}</p>

      <h2>{t("legal:terms.consultants.heading")}</h2>
      <p>{t("legal:terms.consultants.body")}</p>

      <h2>{t("legal:terms.ip.heading")}</h2>
      <p>{t("legal:terms.ip.body")}</p>

      <h2>{t("legal:terms.termination.heading")}</h2>
      <p>{t("legal:terms.termination.body")}</p>

      <h2>{t("legal:terms.liability.heading")}</h2>
      <p>{t("legal:terms.liability.body")}</p>

      <h2>{t("legal:terms.changes.heading")}</h2>
      <p>{t("legal:terms.changes.body")}</p>

      <h2>{t("legal:terms.contact.heading")}</h2>
      <p>{t("legal:terms.contact.body")}</p>
    </LegalLayout>
  );
}
