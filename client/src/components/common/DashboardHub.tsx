import { Link } from "react-router";
import type { LucideIcon } from "lucide-react";

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
      <h1 className="mb-2 text-3xl">{title}</h1>
      <p className="mb-8 text-text-secondary">{subtitle}</p>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map((card) => (
          <Link
            key={card.to}
            to={card.to}
            className="group rounded-xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition hover:border-brand-500 hover:shadow-md"
          >
            <div className="mb-3 flex size-10 items-center justify-center rounded-md bg-brand-50 text-brand-500">
              <card.icon aria-hidden className="size-5" />
            </div>
            <h2 className="mb-1 font-semibold text-text-primary">{card.title}</h2>
            <p className="text-sm text-text-secondary">{card.description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
