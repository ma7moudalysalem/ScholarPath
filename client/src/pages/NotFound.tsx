import { Link } from "react-router";
import { useTranslation } from "react-i18next";

export function NotFound() {
  const { t } = useTranslation(["errors", "common"]);
  return (
    <section className="mx-auto flex min-h-[calc(100vh-8rem)] max-w-xl flex-col items-center justify-center px-4 text-center">
      <h1 className="mb-3 text-6xl font-bold text-brand-500">404</h1>
      <p className="mb-8 text-lg text-text-secondary">{t("errors:notFound")}</p>
      <Link
        to="/"
        className="cta-pill bg-text-primary px-6 py-3 text-base text-text-inverse hover:bg-text-primary/90 dark:bg-brand-500 dark:text-text-on-brand"
      >
        {t("common:cta.goHome")}
      </Link>
    </section>
  );
}
