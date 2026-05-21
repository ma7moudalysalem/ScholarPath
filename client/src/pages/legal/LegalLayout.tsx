import type { ReactNode } from "react";
import { motion } from "motion/react";

/**
 * Shared chrome for the static informational pages — Privacy, Terms, Help,
 * About, Contact. Keeps headings consistent and gives every page a tasteful
 * fade-in so the navigation between them feels deliberate.
 */
export function LegalLayout({
  eyebrow,
  title,
  subtitle,
  updatedLabel,
  children,
}: {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  updatedLabel?: string;
  children: ReactNode;
}) {
  return (
    <div className="mx-auto max-w-3xl px-4 py-16 sm:px-6 lg:py-20">
      <motion.header
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="mb-10"
      >
        {eyebrow && (
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-brand-600">
            {eyebrow}
          </p>
        )}
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-text-primary sm:text-5xl">
          {title}
        </h1>
        {subtitle && (
          <p className="mt-4 max-w-2xl text-base leading-relaxed text-text-secondary">
            {subtitle}
          </p>
        )}
        {updatedLabel && (
          <p className="mt-4 text-xs text-text-tertiary">{updatedLabel}</p>
        )}
      </motion.header>

      <motion.article
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1], delay: 0.08 }}
        className="prose prose-slate max-w-none text-text-secondary
          prose-headings:text-text-primary prose-headings:tracking-tight
          prose-h2:mt-10 prose-h2:text-2xl prose-h2:font-semibold
          prose-h3:mt-6 prose-h3:text-lg prose-h3:font-semibold
          prose-p:leading-relaxed prose-li:leading-relaxed
          prose-a:text-brand-600 prose-a:no-underline hover:prose-a:underline
          dark:prose-invert"
      >
        {children}
      </motion.article>
    </div>
  );
}
