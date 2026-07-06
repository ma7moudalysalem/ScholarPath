import { useEffect, useState } from "react";

/**
 * Returns a debounced copy of `value` that only updates after `delayMs` of no
 * changes. Use it to keep a fast-changing input (e.g. a search box) from firing
 * a network request on every keystroke — feed the debounced value into the
 * query key while the input stays bound to the raw state for instant feedback.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(id);
  }, [value, delayMs]);
  return debounced;
}
