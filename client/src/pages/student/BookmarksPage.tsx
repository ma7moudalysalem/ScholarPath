import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Bookmark, Search } from "lucide-react";

// ── Page ──────────────────────────────────────────────────────────────────────
//
// NOTE: the backend currently exposes only the bookmark *toggle*
// (`POST /api/scholarships/{id}/bookmark`) — there is no endpoint that lists
// a student's saved scholarships. Until that endpoint exists this page shows
// its empty state rather than calling a route that 404s. See the backend
// follow-up to add `GET /api/scholarships/bookmarks`.

export function BookmarksPage() {
  const { t } = useTranslation(["scholarships", "common"]);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
        {t("scholarships:bookmarks.title")}
      </h1>
      <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-20 text-center">
        <Bookmark aria-hidden className="mb-3 size-10 text-text-tertiary" />
        <p className="text-base font-medium text-text-primary">
          {t("scholarships:bookmarks.empty.title")}
        </p>
        <p className="mt-1 text-sm text-text-secondary">
          {t("scholarships:bookmarks.empty.body")}
        </p>
        <Link
          to="/student/scholarships"
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
        >
          <Search aria-hidden className="size-4" />
          {t("scholarships:bookmarks.browse")}
        </Link>
      </div>
    </div>
  );
}
