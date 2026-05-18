import { cn } from "@/lib/utils";

interface SkeletonProps {
  className?: string;
}

/** Single shimmer block. Compose multiples to build any skeleton layout. */
export function Skeleton({ className }: SkeletonProps) {
  return <div aria-hidden className={cn("skeleton", className)} />;
}

/** Skeleton for a standard card grid (scholarships, bookmarks, etc.) */
export function SkeletonCardGrid({ count = 6 }: { count?: number }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: count }).map((_, i) => (
        <div
          key={i}
          className="flex flex-col gap-3 rounded-2xl border border-border-subtle bg-bg-elevated p-5"
        >
          <Skeleton className="h-5 w-2/3" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-4/5" />
          <div className="mt-2 flex items-center justify-between">
            <Skeleton className="h-5 w-20 rounded-full" />
            <Skeleton className="h-4 w-16" />
          </div>
        </div>
      ))}
    </div>
  );
}

/** Skeleton for a single detail page hero card */
export function SkeletonDetailCard() {
  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6">
        <Skeleton className="mb-3 h-6 w-3/4" />
        <Skeleton className="mb-1 h-4 w-1/3" />
        <Skeleton className="mt-4 h-4 w-full" />
        <Skeleton className="mt-2 h-4 w-5/6" />
        <Skeleton className="mt-2 h-4 w-2/3" />
      </div>
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex items-center justify-between border-b border-border-subtle py-3 last:border-0">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-5 w-28 rounded-full" />
          </div>
        ))}
      </div>
    </div>
  );
}

/** Skeleton for a table row */
export function SkeletonTableRows({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, i) => (
        <tr key={i}>
          {Array.from({ length: cols }).map((_, j) => (
            <td key={j} className="px-4 py-3">
              <Skeleton className={cn("h-4", j === 0 ? "w-32" : j === cols - 1 ? "w-16" : "w-24")} />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

/** Skeleton for a list of booking/consultation cards */
export function SkeletonListCards({ count = 4 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <div key={i} className="flex items-center gap-4 rounded-xl border border-border-subtle bg-bg-elevated p-4">
          <Skeleton className="size-10 shrink-0 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-4 w-1/3" />
            <Skeleton className="h-3 w-1/2" />
          </div>
          <Skeleton className="h-6 w-20 rounded-full" />
        </div>
      ))}
    </div>
  );
}
