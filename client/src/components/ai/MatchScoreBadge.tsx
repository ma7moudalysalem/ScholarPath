interface Props {
  score: number; // 0..100
}

export function MatchScoreBadge({ score }: Props) {
  const s = Math.max(0, Math.min(100, score));
  const color =
    s >= 80 ? "bg-success-100 text-success-600 ring-success-500/20"
    : s >= 50 ? "bg-brand-500/10 text-brand-500 ring-brand-500/20"
    : s >= 20 ? "bg-warning-50 text-warning-600 ring-warning-500/20"
    : "bg-danger-50 text-danger-500 ring-danger-400/20";

  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 tabular-nums ${color}`}>
      {s}
    </span>
  );
}
