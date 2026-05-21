import { motion, useReducedMotion } from "motion/react";
import { Children, isValidElement, type ReactNode } from "react";

interface StaggerProps {
  children: ReactNode;
  /** Delay between each child entrance, in seconds. Default 0.04s. */
  staggerDelay?: number;
  /** Base delay applied to every child, in seconds. Default 0. */
  baseDelay?: number;
  /** Max index to apply stagger to (prevents long lists from dragging on). Default 12. */
  maxIndex?: number;
  /** Tailwind class applied to each wrapper element. */
  itemClassName?: string;
}

/**
 * Renders each immediate child wrapped in a `motion.div` that fades in + slides
 * up with a staggered delay. Useful for lists, card grids, and dashboard rows.
 *
 * Respects `prefers-reduced-motion` — falls back to no animation when the user
 * has expressed a preference.
 */
export function Stagger({
  children,
  staggerDelay = 0.04,
  baseDelay = 0,
  maxIndex = 12,
  itemClassName,
}: StaggerProps) {
  const reduceMotion = useReducedMotion();

  return (
    <>
      {Children.map(children, (child, idx) => {
        if (!isValidElement(child)) return child;
        const delay = baseDelay + Math.min(idx, maxIndex) * staggerDelay;
        return (
          <motion.div
            className={itemClassName}
            initial={reduceMotion ? false : { opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{
              duration: reduceMotion ? 0 : 0.35,
              ease: [0.22, 1, 0.36, 1],
              delay: reduceMotion ? 0 : delay,
            }}
          >
            {child}
          </motion.div>
        );
      })}
    </>
  );
}
