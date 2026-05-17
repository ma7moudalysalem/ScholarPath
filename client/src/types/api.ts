/**
 * Shared API response types.
 */

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  /** Total matching rows across all pages — matches the .NET PagedResult record on the wire. */
  totalCount: number;
}
