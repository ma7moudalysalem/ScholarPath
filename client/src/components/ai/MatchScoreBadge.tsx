interface Props {
  score: number; // 0..100
}

export function MatchScoreBadge({ score }: Props) {
  const s = Math.max(0, Math.min(100, score));
  const color =
    s >= 80 ? "bg-emerald-500/10 text-emerald-500 ring-emerald-500/20"
    : s >= 50 ? "bg-brand-500/10 text-brand-500 ring-brand-500/20"
    : s >= 20 ? "bg-amber-500/10 text-amber-600 ring-amber-500/20"
    : "bg-rose-500/10 text-rose-500 ring-rose-500/20";

  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 tabular-nums ${color}`}>
      {s}
    </span>
  );
}
