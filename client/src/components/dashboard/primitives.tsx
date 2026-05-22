import { Link } from "react-router";
import { motion } from "motion/react";
import { ArrowDownRight, ArrowUpRight, type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

// ────────────────────────────────────────────────────────────────────────────
// Welcome banner — premium gradient/orb header used at the top of every page
// ────────────────────────────────────────────────────────────────────────────

interface WelcomeBannerProps {
  eyebrow: string;
  title: React.ReactNode;
  subtitle?: string;
  actions?: React.ReactNode;
}

export function WelcomeBanner({ eyebrow, title, subtitle, actions }: WelcomeBannerProps) {
  return (
    <motion.section
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
      className="relative overflow-hidden rounded-3xl border border-border-subtle bg-bg-elevated p-6 sm:p-8"
    >
      {/* Decorative animated orbs */}
      <div className="orb orb-brand orb-animated -end-24 -top-24 size-72 opacity-40" />
      <div className="orb orb-aurora -start-32 -bottom-32 size-80 opacity-20" />

      <div className="relative z-10">
        <p className="text-sm font-medium text-text-secondary">{eyebrow}</p>
        <h1 className="mt-1 text-3xl font-bold tracking-tight sm:text-4xl">
          {title}
        </h1>
        {subtitle && (
          <p className="mt-2 max-w-2xl text-sm text-text-secondary sm:text-base">{subtitle}</p>
        )}
        {actions && <div className="mt-5 flex flex-wrap gap-2">{actions}</div>}
      </div>
    </motion.section>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Stat card with mini sparkline + delta indicator
// ────────────────────────────────────────────────────────────────────────────

export type StatAccent = "brand" | "warning" | "success" | "danger" | "neutral";

interface StatCardProps {
  label: string;
  value: number | string;
  to?: string;
  icon: LucideIcon;
  accent?: StatAccent;
  delta?: { value: number; label: string } | null;
  /**
   * Optional REAL sparkline points (e.g. a per-day series). Omit it and no
   * sparkline is drawn — we never fabricate a trend curve, so a card without
   * real time-series data simply shows none.
   */
  trend?: number[];
  delay?: number;
}

function buildSparklinePath(values: number[], w = 60, h = 16): string {
  if (values.length < 2) return "";
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);
  const step = w / (values.length - 1);

  // Smooth quadratic bezier through midpoints for a soft curve.
  const points = values.map((v, i) => {
    const x = i * step;
    const y = h - ((v - min) / range) * (h - 2) - 1;
    return [x, y] as const;
  });

  let d = `M ${points[0][0].toFixed(2)} ${points[0][1].toFixed(2)}`;
  for (let i = 1; i < points.length; i++) {
    const [px, py] = points[i - 1];
    const [x, y] = points[i];
    const cx = (px + x) / 2;
    const cy = (py + y) / 2;
    d += ` Q ${px.toFixed(2)} ${py.toFixed(2)} ${cx.toFixed(2)} ${cy.toFixed(2)}`;
    if (i === points.length - 1) {
      d += ` T ${x.toFixed(2)} ${y.toFixed(2)}`;
    }
  }
  return d;
}

const ACCENT_TEXT: Record<StatAccent, string> = {
  brand: "text-brand-500",
  warning: "text-warning-500",
  success: "text-success-500",
  danger: "text-danger-500",
  neutral: "text-text-primary",
};

const ACCENT_BG: Record<StatAccent, string> = {
  brand: "bg-brand-50 text-brand-600",
  warning: "bg-warning-50 text-warning-600",
  success: "bg-success-50 text-success-600",
  danger: "bg-danger-50 text-danger-500",
  neutral: "bg-bg-subtle text-text-secondary",
};

const ACCENT_STRIP: Record<StatAccent, string> = {
  brand: "from-brand-500/40",
  warning: "from-warning-500/40",
  success: "from-success-500/40",
  danger: "from-danger-500/40",
  neutral: "from-border-strong/30",
};

const ACCENT_SPARK: Record<StatAccent, string> = {
  brand: "text-brand-500",
  warning: "text-warning-500",
  success: "text-success-500",
  danger: "text-danger-500",
  neutral: "text-text-tertiary",
};

export function StatCard({
  label,
  value,
  to,
  icon: Icon,
  accent = "brand",
  delta,
  trend,
  delay = 0,
}: StatCardProps) {
  // Only draw a sparkline when the caller supplies real series data.
  const path = trend && trend.length >= 2 ? buildSparklinePath(trend) : "";
  const positive = delta ? delta.value >= 0 : true;
  const deltaColor = positive ? "text-success-600 bg-success-50" : "text-danger-500 bg-danger-50";
  const DeltaIcon = positive ? ArrowUpRight : ArrowDownRight;

  const inner = (
    <>
      {/* Top gradient accent strip */}
      <div
        className={cn(
          "absolute inset-x-0 top-0 h-0.5 bg-gradient-to-r to-transparent",
          ACCENT_STRIP[accent],
        )}
      />

      <div className="flex items-start justify-between gap-3">
        <div className={cn("flex size-9 items-center justify-center rounded-xl", ACCENT_BG[accent])}>
          <Icon aria-hidden className="size-4" />
        </div>

        {/* Mini sparkline */}
        {path && (
          <svg
            viewBox="0 0 60 16"
            preserveAspectRatio="none"
            aria-hidden
            className={cn("h-4 w-[60px] shrink-0", ACCENT_SPARK[accent])}
          >
            <defs>
              <linearGradient id={`sparkfill-${accent}`} x1="0" x2="0" y1="0" y2="1">
                <stop offset="0%" stopColor="currentColor" stopOpacity="0.18" />
                <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
              </linearGradient>
            </defs>
            <path d={`${path} L 60 16 L 0 16 Z`} fill={`url(#sparkfill-${accent})`} />
            <path
              d={path}
              fill="none"
              stroke="currentColor"
              strokeWidth="1.25"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        )}
      </div>

      <div className="mt-4 flex items-baseline gap-2">
        <span className={cn("text-3xl font-bold tabular-nums tracking-tight", ACCENT_TEXT[accent])}>
          {value}
        </span>
        {delta && (
          <span
            className={cn(
              "inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[10px] font-semibold",
              deltaColor,
            )}
          >
            <DeltaIcon className="size-3" aria-hidden />
            {Math.abs(delta.value)}%
          </span>
        )}
      </div>

      <p className="mt-1 text-xs font-medium text-text-secondary">{label}</p>
      {delta && (
        <p className="mt-0.5 text-[11px] text-text-tertiary">{delta.label}</p>
      )}
    </>
  );

  const className =
    "relative flex flex-col overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition-all duration-200 hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400";

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay }}
    >
      {to ? (
        <Link to={to} className={className}>
          {inner}
        </Link>
      ) : (
        <div className={className}>{inner}</div>
      )}
    </motion.div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Quick action tile (icon + label)
// ────────────────────────────────────────────────────────────────────────────

export interface QuickAction {
  icon: LucideIcon;
  label: string;
  to?: string;
  onClick?: () => void;
  accent?: StatAccent;
}

export function QuickActions({ title, actions }: { title: string; actions: QuickAction[] }) {
  return (
    <section className="card-premium p-5 sm:p-6">
      <h2 className="mb-4 text-sm font-semibold text-text-primary">{title}</h2>
      <div className="grid grid-cols-2 gap-2.5">
        {actions.map(({ icon: Icon, label, to, onClick, accent = "brand" }) => {
          const className =
            "group flex flex-col items-start gap-2 rounded-xl border border-border-subtle bg-bg-elevated p-3 text-left transition-all duration-150 hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400";
          const inner = (
            <>
              <span
                className={cn(
                  "flex size-8 items-center justify-center rounded-lg transition-colors",
                  ACCENT_BG[accent],
                  "group-hover:saturate-150",
                )}
              >
                <Icon aria-hidden className="size-4" />
              </span>
              <span className="text-xs font-medium text-text-primary leading-tight">{label}</span>
            </>
          );
          return to ? (
            <Link key={label} to={to} className={className}>
              {inner}
            </Link>
          ) : (
            <button key={label} type="button" onClick={onClick} className={className}>
              {inner}
            </button>
          );
        })}
      </div>
    </section>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Activity feed (consumes a small list of activities)
// ────────────────────────────────────────────────────────────────────────────

export interface ActivityItem {
  id: string;
  title: string;
  timeAgo: string;
  icon: LucideIcon;
  accent?: StatAccent;
  to?: string;
}

interface ActivityFeedProps {
  title: string;
  viewAllLabel: string;
  viewAllTo?: string;
  emptyTitle: string;
  emptyBody: string;
  items: ActivityItem[];
  isLoading?: boolean;
}

export function ActivityFeed({
  title,
  viewAllLabel,
  viewAllTo,
  emptyTitle,
  emptyBody,
  items,
  isLoading,
}: ActivityFeedProps) {
  return (
    <section className="card-premium p-5 sm:p-6">
      <header className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-text-primary">{title}</h2>
        {viewAllTo && items.length > 0 && (
          <Link
            to={viewAllTo}
            className="text-xs font-medium text-brand-600 transition-colors hover:text-brand-700 hover:underline"
          >
            {viewAllLabel}
          </Link>
        )}
      </header>

      {isLoading ? (
        <ul className="space-y-3">
          {[1, 2, 3].map((i) => (
            <li key={i} className="flex gap-3">
              <div className="size-8 shrink-0 animate-pulse rounded-full bg-bg-subtle" />
              <div className="flex-1 space-y-2 pt-1">
                <div className="h-3 w-3/4 animate-pulse rounded bg-bg-subtle" />
                <div className="h-2.5 w-1/3 animate-pulse rounded bg-bg-subtle" />
              </div>
            </li>
          ))}
        </ul>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border-subtle bg-bg-subtle/30 p-6 text-center">
          <p className="text-sm font-medium text-text-primary">{emptyTitle}</p>
          <p className="mt-1 text-xs text-text-tertiary">{emptyBody}</p>
        </div>
      ) : (
        <ul className="space-y-3">
          {items.map((a) => {
            const Icon = a.icon;
            const accent = a.accent ?? "brand";
            const row = (
              <>
                <div
                  className={cn(
                    "flex size-8 shrink-0 items-center justify-center rounded-full",
                    ACCENT_BG[accent],
                  )}
                >
                  <Icon aria-hidden className="size-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="line-clamp-2 text-sm text-text-primary">{a.title}</p>
                  <p className="mt-0.5 text-xs text-text-tertiary">{a.timeAgo}</p>
                </div>
              </>
            );
            return (
              <li key={a.id}>
                {a.to ? (
                  <Link
                    to={a.to}
                    className="flex gap-3 rounded-lg p-1 -m-1 transition-colors hover:bg-bg-subtle/60"
                  >
                    {row}
                  </Link>
                ) : (
                  <div className="flex gap-3">{row}</div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Time-range pill tabs (analytics pages)
// ────────────────────────────────────────────────────────────────────────────

export interface TimeRangeOption<T extends string | number> {
  value: T;
  label: string;
}

interface TimeRangeTabsProps<T extends string | number> {
  value: T;
  options: TimeRangeOption<T>[];
  onChange: (v: T) => void;
  ariaLabel?: string;
}

export function TimeRangeTabs<T extends string | number>({
  value,
  options,
  onChange,
  ariaLabel,
}: TimeRangeTabsProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className="inline-flex items-center gap-1 rounded-full border border-border-subtle bg-bg-elevated p-1 shadow-xs"
    >
      {options.map((o) => {
        const active = o.value === value;
        return (
          <button
            key={String(o.value)}
            role="tab"
            aria-selected={active}
            type="button"
            onClick={() => onChange(o.value)}
            className={cn(
              "rounded-full px-3 py-1.5 text-xs font-semibold transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400",
              active
                ? "bg-brand-500 text-white shadow-xs"
                : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
            )}
          >
            {o.label}
          </button>
        );
      })}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Smooth area / line chart (used in analytics pages)
// ────────────────────────────────────────────────────────────────────────────

interface AreaChartProps {
  values: number[];
  labels?: string[];
  height?: number;
  /** CSS color via currentColor — wrap the chart in a text-* class. */
  ariaLabel?: string;
}

export function SmoothAreaChart({ values, labels, height = 200, ariaLabel }: AreaChartProps) {
  if (values.length < 2) {
    return <div style={{ height }} className="rounded-lg bg-bg-subtle/40" />;
  }
  const w = 800;
  const h = height;
  const padX = 8;
  const padY = 12;
  const innerW = w - padX * 2;
  const innerH = h - padY * 2;
  const max = Math.max(...values);
  const min = Math.min(...values);
  const range = Math.max(max - min, 1);
  const step = innerW / (values.length - 1);

  const points = values.map((v, i) => {
    const x = padX + i * step;
    const y = padY + innerH - ((v - min) / range) * innerH;
    return [x, y] as const;
  });

  // Smooth path with mid-point quadratic interpolation
  let d = `M ${points[0][0].toFixed(2)} ${points[0][1].toFixed(2)}`;
  for (let i = 1; i < points.length; i++) {
    const [px, py] = points[i - 1];
    const [x, y] = points[i];
    const cx = (px + x) / 2;
    const cy = (py + y) / 2;
    d += ` Q ${px.toFixed(2)} ${py.toFixed(2)} ${cx.toFixed(2)} ${cy.toFixed(2)}`;
    if (i === points.length - 1) d += ` T ${x.toFixed(2)} ${y.toFixed(2)}`;
  }
  const areaD = `${d} L ${(padX + innerW).toFixed(2)} ${(padY + innerH).toFixed(2)} L ${padX.toFixed(2)} ${(padY + innerH).toFixed(2)} Z`;

  // Gridlines (4 horizontal)
  const grid = [0.25, 0.5, 0.75, 1].map((r) => padY + innerH * r);

  return (
    <svg
      viewBox={`0 0 ${w} ${h}`}
      preserveAspectRatio="none"
      role="img"
      aria-label={ariaLabel}
      className="h-48 w-full text-brand-500 sm:h-56"
    >
      <defs>
        <linearGradient id="area-fill" x1="0" x2="0" y1="0" y2="1">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.28" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </linearGradient>
        <filter id="area-glow" x="-10%" y="-30%" width="120%" height="160%">
          <feGaussianBlur stdDeviation="2" />
        </filter>
      </defs>
      {grid.map((y, i) => (
        <line
          key={i}
          x1={padX}
          y1={y}
          x2={padX + innerW}
          y2={y}
          stroke="currentColor"
          strokeOpacity="0.08"
          strokeWidth="0.5"
        />
      ))}
      <path d={areaD} fill="url(#area-fill)" />
      <path d={d} fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeOpacity="0.3" filter="url(#area-glow)" />
      <path d={d} fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" />
      {/* Endpoint dot */}
      <circle
        cx={points[points.length - 1][0]}
        cy={points[points.length - 1][1]}
        r="3.5"
        fill="currentColor"
      />
      <circle
        cx={points[points.length - 1][0]}
        cy={points[points.length - 1][1]}
        r="6"
        fill="currentColor"
        fillOpacity="0.18"
      />
      {labels && labels.length === values.length && (
        <g>
          {labels.map((label, i) => {
            if (i % Math.ceil(labels.length / 6) !== 0 && i !== labels.length - 1) return null;
            const x = padX + i * step;
            return (
              <text
                key={i}
                x={x}
                y={h - 2}
                fontSize="9"
                textAnchor="middle"
                fill="currentColor"
                fillOpacity="0.55"
              >
                {label}
              </text>
            );
          })}
        </g>
      )}
    </svg>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Chart card wrapper (title + subtitle + chart)
// ────────────────────────────────────────────────────────────────────────────

interface ChartCardProps {
  title: string;
  subtitle?: string;
  trailing?: React.ReactNode;
  children: React.ReactNode;
}

export function ChartCard({ title, subtitle, trailing, children }: ChartCardProps) {
  return (
    <section className="card-premium p-5 sm:p-6">
      <header className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-text-primary">{title}</h2>
          {subtitle && <p className="mt-0.5 text-xs text-text-tertiary">{subtitle}</p>}
        </div>
        {trailing}
      </header>
      {children}
    </section>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Horizontal category bars (status / breakdown distributions). A reusable
// generalization of the admin application-funnel bars — feed it any labelled
// counts and it renders proportional bars with a value column.
// ────────────────────────────────────────────────────────────────────────────

export interface CategoryBar {
  label: string;
  count: number;
}

const CATEGORY_BAR_COLORS = [
  "bg-brand-500",
  "bg-success-500",
  "bg-warning-500",
  "bg-danger-500",
  "bg-text-tertiary",
];

export function CategoryBars({
  items,
  emptyLabel,
}: {
  items: CategoryBar[];
  emptyLabel?: string;
}) {
  const total = items.reduce((sum, it) => sum + it.count, 0);
  if (items.length === 0 || total === 0) {
    return (
      <p className="py-8 text-center text-sm text-text-tertiary">{emptyLabel}</p>
    );
  }
  const max = Math.max(...items.map((it) => it.count), 1);
  return (
    <div className="space-y-3">
      {items.map((it, idx) => (
        <div key={it.label} className="flex items-center gap-3">
          <span className="w-32 shrink-0 truncate text-xs font-medium text-text-secondary">
            {it.label}
          </span>
          <div className="relative h-2 flex-1 overflow-hidden rounded-full bg-bg-subtle">
            <div
              className={cn(
                "h-full rounded-full transition-all",
                CATEGORY_BAR_COLORS[idx % CATEGORY_BAR_COLORS.length],
              )}
              style={{ width: `${(it.count / max) * 100}%` }}
            />
          </div>
          <span className="w-10 shrink-0 text-end text-xs font-semibold tabular-nums text-text-primary">
            {it.count}
          </span>
        </div>
      ))}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Hub card (smaller, used for module navigation grids)
// ────────────────────────────────────────────────────────────────────────────

interface HubCardProps {
  icon: LucideIcon;
  title: string;
  description: string;
  to: string;
  delay?: number;
  accent?: StatAccent;
}

export function HubCard({ icon: Icon, title, description, to, delay = 0, accent = "brand" }: HubCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1], delay }}
    >
      <Link
        to={to}
        className="group flex h-full flex-col rounded-2xl border border-border-subtle bg-bg-elevated p-5 shadow-xs transition-all duration-200 hover:-translate-y-0.5 hover:border-brand-200 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        <div
          className={cn(
            "mb-4 flex size-10 items-center justify-center rounded-xl transition-all duration-200",
            ACCENT_BG[accent],
            "group-hover:scale-105",
          )}
        >
          <Icon aria-hidden className="size-5" />
        </div>
        <h2 className="font-semibold text-text-primary transition-colors group-hover:text-brand-600">
          {title}
        </h2>
        <p className="mt-1 text-sm leading-relaxed text-text-secondary">{description}</p>
      </Link>
    </motion.div>
  );
}

// ────────────────────────────────────────────────────────────────────────────
// Legend pill row (small colored dots + label, used under chart subtitles)
// ────────────────────────────────────────────────────────────────────────────

export interface LegendItem {
  label: string;
  /** Tailwind text color class, e.g. "text-brand-500". */
  colorClass: string;
}

export function LegendRow({ items }: { items: LegendItem[] }) {
  return (
    <ul className="flex flex-wrap gap-2">
      {items.map((it) => (
        <li
          key={it.label}
          className="inline-flex items-center gap-1.5 rounded-full border border-border-subtle bg-bg-subtle/60 px-2.5 py-1 text-[11px] font-medium text-text-secondary"
        >
          <span className={cn("size-2 rounded-full bg-current", it.colorClass)} aria-hidden />
          {it.label}
        </li>
      ))}
    </ul>
  );
}

// formatRelativeTime moved to ./utils.ts to keep this file purely React components
// (so fast-refresh stays clean).
