import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
}

/**
 * Wraps a route's content with a subtle fade + slide-up entrance.
 * Respects `prefers-reduced-motion` by collapsing to a no-op transition.
 */
export function AnimatedRoute({ children }: Props) {
  const reduceMotion = useReducedMotion();

  return (
    <motion.div
      initial={reduceMotion ? false : { opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{
        duration: reduceMotion ? 0 : 0.32,
        ease: [0.22, 1, 0.36, 1],
      }}
      className="min-h-full"
    >
      {children}
    </motion.div>
  );
}
