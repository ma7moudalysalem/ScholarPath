import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { FileText, Plus } from "lucide-react";
import { format } from "date-fns";
import { cn } from "@/lib/utils";
import { useMyApplicationsQuery } from "@/hooks/useApplicationsQuery";
import type { ApplicationStatus } from "@/types/domain";
import type { ApplicationListItem } from "@/services/api/applications";

// ── Types ─────────────────────────────────────────────────────────────────────

type KanbanColumn = {
  status: ApplicationStatus;
  labelKey: string;
  color: string;
  dot: string;
};

const COLUMNS: KanbanColumn[] = [
  { status: "Draft",       labelKey: "applications:status.Draft",       color: "border-t-slate-400",   dot: "bg-slate-400"   },
  { status: "Pending",     labelKey: "applications:status.Pending",     color: "border-t-amber-400",   dot: "bg-amber-400"   },
  { status: "UnderReview", labelKey: "applications:status.UnderReview", color: "border-t-blue-500",    dot: "bg-blue-500"    },
  { status: "Shortlisted", labelKey: "applications:status.Shortlisted", color: "border-t-purple-500",  dot: "bg-purple-500"  },
  { status: "Accepted",    labelKey: "applications:status.Accepted",    color: "border-t-emerald-500", dot: "bg-emerald-500" },
  { status: "Rejected",    labelKey: "applications:status.Rejected",    color: "border-t-rose-500",    dot: "bg-rose-500"    },
  { status: "Withdrawn",   labelKey: "applications:status.Withdrawn",   color: "border-t-slate-300",   dot: "bg-slate-300"   },
];

const EXTERNAL_COLUMNS: KanbanColumn[] = [
  { status: "Intending",     labelKey: "applications:status.Intending",     color: "border-t-slate-400",  dot: "bg-slate-400"  },
  { status: "Applied",       labelKey: "applications:status.Applied",       color: "border-t-amber-400",  dot: "bg-amber-400"  },
  { status: "WaitingResult", labelKey: "applications:status.WaitingResult", color: "border-t-blue-500",   dot: "bg-blue-500"   },
  { status: "Accepted",      labelKey: "applications:status.Accepted",      color: "border-t-emerald-500",dot: "bg-emerald-500"},
  { status: "Rejected",      labelKey: "applications:status.Rejected",      color: "border-t-rose-500",   dot: "bg-rose-500"   },
];

// ── Sub-components ────────────────────────────────────────────────────────────

function ApplicationCard({
  app,
  isRtl,
}: {
  app: ApplicationListItem;
  isRtl: boolean;
}) {
  const { t } = useTranslation(["applications"]);
  const title = isRtl ? app.scholarshipTitleAr : app.scholarshipTitleEn;

  return (
    <Link
      to={`/student/applications/${app.id}`}
      className="block rounded-lg border border-border-subtle bg-bg-canvas p-3 shadow-xs transition hover:border-brand-500/40 hover:shadow-sm"
    >
      <p className="line-clamp-2 text-xs font-semibold text-text-primary">
        {title}
      </p>

      <p className="mt-1.5 text-[11px] text-text-tertiary">
        {t("applications:card.deadline")}
        {" "}
        {format(new Date(app.scholarshipDeadline), "dd MMM yyyy")}
      </p>

      {app.submittedAt && (
        <p className="mt-0.5 text-[11px] text-text-tertiary">
          {t("applications:card.submitted")}
          {" "}
          {format(new Date(app.submittedAt), "dd MMM yyyy")}
        </p>
      )}

      {app.isReadOnly && (
        <span className="mt-2 inline-flex items-center rounded-full bg-bg-subtle px-2 py-0.5 text-[10px] text-text-tertiary">
          {t("applications:card.readOnly")}
        </span>
      )}
    </Link>
  );
}

function KanbanCol({
  column,
  apps,
  isRtl,
}: {
  column: KanbanColumn;
  apps: ApplicationListItem[];
  isRtl: boolean;
}) {
  const { t } = useTranslation(["applications"]);

  return (
    <div
      className={cn(
        "flex min-w-55 flex-1 flex-col rounded-xl border border-border-subtle bg-bg-elevated border-t-2",
        column.color,
      )}
    >
      {/* Column header */}
      <div className="flex items-center gap-2 px-3 py-2.5">
        <span className={cn("size-2 shrink-0 rounded-full", column.dot)} />
        <span className="text-xs font-semibold text-text-primary">
          {t(column.labelKey)}
        </span>
        <span className="ms-auto flex size-5 items-center justify-center rounded-full bg-bg-subtle text-[10px] font-semibold text-text-tertiary">
          {apps.length}
        </span>
      </div>

      {/* Cards */}
      <div className="flex flex-col gap-2 p-2">
        {apps.length === 0 ? (
          <p className="py-4 text-center text-[11px] text-text-tertiary">
            {t("applications:kanban.empty")}
          </p>
        ) : (
          apps.map((app) => (
            <ApplicationCard key={app.id} app={app} isRtl={isRtl} />
          ))
        )}
      </div>
    </div>
  );
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function ApplicationsPage() {
  const { t, i18n } = useTranslation(["applications", "common"]);
  const isRtl        = i18n.dir() === "rtl";

  const { data: apps, isLoading, isError } = useMyApplicationsQuery();

  const inAppApps    = apps?.filter((a) => a.mode === "InApp")    ?? [];
  const externalApps = apps?.filter((a) => a.mode === "External") ?? [];

  const grouped = (
    cols: KanbanColumn[],
    list: ApplicationListItem[],
  ) =>
    cols.map((col) => ({
      column: col,
      apps:   list.filter((a) => a.status === col.status),
    }));

  // ── Loading ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-48 animate-pulse rounded-md bg-bg-elevated" />
        <div className="flex gap-3 overflow-x-auto pb-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              className="h-64 min-w-55 animate-pulse rounded-xl bg-bg-elevated"
            />
          ))}
        </div>
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError) {
    return (
      <div className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-600">
        {t("common:status.error")}
      </div>
    );
  }

  // ── Empty ──────────────────────────────────────────────────────────────────
  if (!apps || apps.length === 0) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
            {t("applications:page.title")}
          </h1>
        </div>
        <div className="flex flex-col items-center justify-center rounded-xl border border-border-subtle bg-bg-elevated py-20 text-center">
          <FileText aria-hidden className="mb-3 size-10 text-text-tertiary" />
          <p className="text-base font-medium text-text-primary">
            {t("applications:empty.title")}
          </p>
          <p className="mt-1 text-sm text-text-secondary">
            {t("applications:empty.body")}
          </p>
          <Link
            to="/student/scholarships"
            className="mt-4 inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600"
          >
            <Plus aria-hidden className="size-4" />
            {t("applications:empty.cta")}
          </Link>
        </div>
      </div>
    );
  }

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-8">

      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("applications:page.title")}
        </h1>
        <span className="text-sm text-text-tertiary">
          {t("applications:page.total", { count: apps.length })}
        </span>
      </div>

      {/* ── In-App Kanban ── */}
      {inAppApps.length > 0 && (
        <section>
          <h2 className="mb-3 text-sm font-semibold text-text-secondary">
            {t("applications:page.inApp")}
          </h2>
          <div className="flex gap-3 overflow-x-auto pb-3">
            {grouped(COLUMNS, inAppApps).map(({ column, apps: colApps }) => (
              <KanbanCol
                key={column.status}
                column={column}
                apps={colApps}
                isRtl={isRtl}
              />
            ))}
          </div>
        </section>
      )}

      {/* ── External Tracker Kanban ── */}
      {externalApps.length > 0 && (
        <section>
          <h2 className="mb-3 text-sm font-semibold text-text-secondary">
            {t("applications:page.external")}
          </h2>
          <div className="flex gap-3 overflow-x-auto pb-3">
            {grouped(EXTERNAL_COLUMNS, externalApps).map(({ column, apps: colApps }) => (
              <KanbanCol
                key={column.status}
                column={column}
                apps={colApps}
                isRtl={isRtl}
              />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
