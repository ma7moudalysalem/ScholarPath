import { motion, useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

interface FadeInProps {
  children: ReactNode;
  /** Vertical offset (px) the element starts at. Default 0. */
  y?: number;
  /** Horizontal offset (px) the element starts at. Default 0. */
  x?: number;
  /** Delay before animation starts, in seconds. Default 0. */
  delay?: number;
  /** Animation duration, in seconds. Default 0.32. */
  duration?: number;
  className?: string;
  style?: CSSProperties;
  /** Render as inline-block or block. Default "block". */
  as?: "block" | "inline-block";
}

/**
 * Simple fade-in wrapper. Respects `prefers-reduced-motion`.
 */
export function FadeIn({
  children,
  y = 0,
  x = 0,
  delay = 0,
  duration = 0.32,
  className,
  style,
  as = "block",
}: FadeInProps) {
  const reduceMotion = useReducedMotion();

  return (
    <motion.div
      className={className}
      style={{ display: as === "inline-block" ? "inline-block" : undefined, ...style }}
      initial={reduceMotion ? false : { opacity: 0, y, x }}
      animate={{ opacity: 1, y: 0, x: 0 }}
      transition={{
        duration: reduceMotion ? 0 : duration,
        ease: [0.22, 1, 0.36, 1],
        delay: reduceMotion ? 0 : delay,
      }}
    >
      {children}
    </motion.div>
  );
}
