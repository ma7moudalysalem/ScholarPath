import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

interface PulseProps {
  children: ReactNode;
  /** Pulse cycle duration in seconds. Default 1.8. */
  duration?: number;
  /** Peak scale at the apex of the pulse. Default 1.08. */
  scale?: number;
  className?: string;
}

/**
 * Wraps children in a soft, looping scale pulse. Good for "new" indicators,
 * live status dots, and small attention-grabbing badges.
 *
 * Respects `prefers-reduced-motion` by rendering the children with no
 * animation.
 */
export function Pulse({
  children,
  duration = 1.8,
  scale = 1.08,
  className,
}: PulseProps) {
  const reduceMotion = useReducedMotion();

  if (reduceMotion) {
    return <span className={className}>{children}</span>;
  }

  return (
    <motion.span
      className={className}
      style={{ display: "inline-flex" }}
      animate={{ scale: [1, scale, 1] }}
      transition={{
        duration,
        ease: "easeInOut",
        repeat: Infinity,
      }}
    >
      {children}
    </motion.span>
  );
}
