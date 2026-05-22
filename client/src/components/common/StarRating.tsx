import { Star } from "lucide-react";

export interface StarRatingProps {
  /** The rating to render (0–5). Null renders an all-empty row. */
  value: number | null;
  /** Pixel size of each star (default 14). */
  size?: number;
  /** Accessible label; when omitted the row is marked aria-hidden. */
  ariaLabel?: string;
}

/**
 * Read-only 5-star rating with half-star precision. Mirrors the inline star
 * renderer used on the consultant detail page, promoted to a shared component
 * so the "reviews received" pages render identical stars. RTL-safe: the row is
 * a simple flex of fixed-size boxes, so it mirrors with the document direction.
 */
export function StarRating({ value, size = 14, ariaLabel }: StarRatingProps) {
  const rounded = value == null ? 0 : Math.round(value * 2) / 2;
  return (
    <div
      className="flex items-center gap-0.5"
      role={ariaLabel ? "img" : undefined}
      aria-label={ariaLabel}
      aria-hidden={ariaLabel ? undefined : true}
    >
      {Array.from({ length: 5 }, (_, i) => {
        const pos = i + 1;
        const isFull = rounded >= pos;
        const isHalf = !isFull && rounded >= pos - 0.5;
        return (
          <span key={i} className="relative inline-flex" style={{ width: size, height: size }}>
            <Star size={size} className="text-text-tertiary/40" strokeWidth={1.5} />
            {(isFull || isHalf) && (
              <span
                className="absolute inset-0 overflow-hidden rtl:right-0"
                style={{ width: isHalf ? "50%" : "100%" }}
              >
                <Star size={size} className="fill-amber-400 text-amber-400" strokeWidth={1.5} />
              </span>
            )}
          </span>
        );
      })}
    </div>
  );
}
