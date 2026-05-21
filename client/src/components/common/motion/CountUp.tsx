import { animate, useMotionValue, useReducedMotion } from "motion/react";
import { useEffect, useState } from "react";

interface CountUpProps {
  /** Final number to count up to. */
  to: number;
  /** Starting number. Default 0. */
  from?: number;
  /** Animation duration, in seconds. Default 1.1. */
  duration?: number;
  /** Number of decimal places to show. Default 0. */
  decimals?: number;
  /** Optional prefix (e.g. "$"). */
  prefix?: string;
  /** Optional suffix (e.g. "%"). */
  suffix?: string;
  /** Locale for number formatting. Default browser locale. */
  locale?: string;
  /** Optional className applied to the wrapping span. */
  className?: string;
}

/**
 * Animates a numeric value from `from` (default 0) to `to` using
 * `motion`'s `animate` + `useMotionValue`. Drop on any stat card.
 *
 * Respects `prefers-reduced-motion` — renders the final value
 * immediately without animation when the user opts out.
 */
export function CountUp({
  to,
  from = 0,
  duration = 1.1,
  decimals = 0,
  prefix,
  suffix,
  locale,
  className,
}: CountUpProps) {
  const reduceMotion = useReducedMotion();
  const mv = useMotionValue(reduceMotion ? to : from);
  const [display, setDisplay] = useState<number>(reduceMotion ? to : from);

  useEffect(() => {
    if (reduceMotion) {
      setDisplay(to);
      return;
    }
    const controls = animate(mv, to, {
      duration,
      ease: [0.22, 1, 0.36, 1],
      onUpdate: (latest) => setDisplay(latest),
    });
    return () => controls.stop();
  }, [to, duration, reduceMotion, mv]);

  const formatted = display.toLocaleString(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });

  return (
    <span className={className}>
      {prefix}
      {formatted}
      {suffix}
    </span>
  );
}
