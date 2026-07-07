import { cn } from "@/lib/utils";

export interface SegmentedFilterOption<T> {
  value: T;
  label: string;
}

interface SegmentedFilterProps<T> {
  options: SegmentedFilterOption<T>[];
  value: T;
  onChange: (value: T) => void;
  ariaLabel?: string;
  className?: string;
}

/**
 * The one canonical status/segment filter for admin list pages. It replaces the
 * three different idioms that had drifted apart (a pill row here, a native
 * <select> there, a tab strip elsewhere) for the same "filter by a small fixed
 * set" job. Use this for short, mutually-exclusive option sets; keep a native
 * <select> only for genuinely long/open-ended lists.
 */
export function SegmentedFilter<T extends string | number | null>({
  options,
  value,
  onChange,
  ariaLabel,
  className,
}: SegmentedFilterProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className={cn(
        "inline-flex flex-wrap gap-0.5 rounded-md border border-border-subtle bg-bg-elevated p-0.5",
        className,
      )}
    >
      {options.map((opt) => {
        const active = opt.value === value;
        return (
          <button
            key={String(opt.value)}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() => onChange(opt.value)}
            className={cn(
              "rounded px-3 py-1 text-xs font-medium transition",
              active
                ? "bg-brand-500 text-text-on-brand"
                : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
            )}
          >
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}
