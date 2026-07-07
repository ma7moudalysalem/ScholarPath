import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Compass, Home, ArrowLeft, Search } from "lucide-react";
import { motion } from "motion/react";
import { useAuthStore } from "@/stores/authStore";
import { postAuthPath } from "@/services/api/auth";

export function NotFound() {
  const { t } = useTranslation(["errors", "common"]);
  const user = useAuthStore((s) => s.user);
  // Signed-in users return to their dashboard; visitors to the landing page.
  const homePath = user ? postAuthPath(user) : "/";
  // The suggestion cards deep-link into Student-only routes (/student/*), so only
  // offer them to students (or signed-out visitors → landing). A signed-in
  // consultant / provider / admin would just bounce off the RequireRole gate.
  const isStudent =
    user?.activeRole === "Student" || (user?.roles?.includes("Student") ?? false);

  return (
    <section className="relative mx-auto flex min-h-[calc(100vh-8rem)] max-w-2xl flex-col items-center justify-center px-4 py-12 text-center overflow-hidden">
      {/* Decorative orbs */}
      <div aria-hidden className="bg-mesh-hero pointer-events-none absolute inset-0 opacity-50" />
      <div aria-hidden className="orb orb-brand orb-animated absolute top-1/4 -start-32 size-64" />
      <div aria-hidden className="orb orb-aurora orb-animated absolute bottom-1/4 -end-32 size-72" style={{ animationDelay: "3s" }} />

      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="relative z-[1] flex flex-col items-center"
      >
        {/* Illustration placeholder — compass with floating 404 */}
        <div className="relative mb-8">
          <div className="flex size-28 items-center justify-center rounded-3xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-lg">
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ duration: 12, repeat: Infinity, ease: "linear" }}
            >
              <Compass className="size-14" aria-hidden />
            </motion.div>
          </div>
          <div aria-hidden className="absolute inset-0 rounded-3xl bg-brand-500/30 blur-3xl -z-10" />
        </div>

        <h1 className="mb-3 text-8xl font-bold text-gradient tracking-tight leading-none">404</h1>
        <p className="mb-2 text-2xl font-bold text-text-primary tracking-tight">
          {t("errors:notFound")}
        </p>
        <p className="mb-8 max-w-md text-base text-text-secondary leading-relaxed">
          {t("errors:notFoundBody")}
        </p>

        <div className="flex flex-wrap items-center justify-center gap-3">
          <Link to={homePath} className="btn btn-primary btn-lg">
            <Home size={16} aria-hidden />
            {t("common:cta.goHome")}
          </Link>
          <button
            type="button"
            onClick={() => window.history.back()}
            className="btn btn-secondary btn-lg"
          >
            <ArrowLeft size={16} aria-hidden className="rtl:rotate-180" />
            {t("common:dialog.cancel", "Back")}
          </button>
        </div>

        {/* Suggested links — Student-only routes, so only for students or visitors. */}
        {(!user || isStudent) && (
          <div className="mt-12 grid grid-cols-2 gap-3 w-full max-w-md">
            <Link
              to={user ? "/student/scholarships" : "/"}
              className="card-premium p-4 text-start group hover:border-brand-300"
            >
              <Search size={16} className="text-brand-500 mb-2" aria-hidden />
              <p className="text-sm font-bold text-text-primary group-hover:text-brand-600 transition-colors">
                {t("errors:notFoundLinks.scholarships.title")}
              </p>
              <p className="text-xs text-text-tertiary mt-0.5">
                {t("errors:notFoundLinks.scholarships.subtitle")}
              </p>
            </Link>
            <Link
              to={user ? "/student/ai" : "/"}
              className="card-premium p-4 text-start group hover:border-brand-300"
            >
              <Compass size={16} className="text-brand-500 mb-2" aria-hidden />
              <p className="text-sm font-bold text-text-primary group-hover:text-brand-600 transition-colors">
                {t("errors:notFoundLinks.ai.title")}
              </p>
              <p className="text-xs text-text-tertiary mt-0.5">
                {t("errors:notFoundLinks.ai.subtitle")}
              </p>
            </Link>
          </div>
        )}
      </motion.div>
    </section>
  );
}
