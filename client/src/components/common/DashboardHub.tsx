import { Link } from "react-router";
import type { LucideIcon } from "lucide-react";
import { motion } from "motion/react";

export interface DashboardCard {
  icon: LucideIcon;
  title: string;
  description: string;
  to: string;
}

/** A role landing page — a welcome header over a grid of navigation cards. */
export function DashboardHub({
  title,
  subtitle,
  cards,
}: {
  title: string;
  subtitle: string;
  cards: DashboardCard[];
}) {
  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
      >
        <h1 className="mb-1.5 text-3xl">{title}</h1>
        <p className="mb-8 text-text-secondary">{subtitle}</p>
      </motion.div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map((card, idx) => (
          <motion.div
            key={card.to}
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay: idx * 0.05 }}
          >
            <Link
              to={card.to}
              className="group flex h-full flex-col rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition-all duration-200 hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-md"
            >
              <div className="mb-4 flex size-10 items-center justify-center rounded-xl bg-brand-50 text-brand-500 transition-all duration-200 group-hover:bg-brand-500 group-hover:text-white">
                <card.icon aria-hidden className="size-5" />
              </div>
              <h2 className="mb-1 font-semibold text-text-primary transition-colors group-hover:text-brand-500">
                {card.title}
              </h2>
              <p className="text-sm leading-relaxed text-text-secondary">{card.description}</p>
            </Link>
          </motion.div>
        ))}
      </div>
    </div>
  );
}
