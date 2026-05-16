import { useState } from "react";
import { useParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import {
  ArrowLeft,
  ArrowRight,
  Clock,
  CheckCircle,
  XCircle,
  AlertCircle,
  ExternalLink,
} from "lucide-react";
import { format } from "date-fns";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import {
  useApplicationDetailQuery,
  useSubmitApplicationMutation,
  useWithdrawApplicationMutation,
  useUpdateExternalStatusMutation,
} from "@/hooks/useApplicationsQuery";
import type { ApplicationStatus } from "@/types/domain";
import type { UpdateExternalStatusRequest } from "@/services/api/applications";

// ── Helpers ───────────────────────────────────────────────────────────────────

const STATUS_ICON: Record<ApplicationStatus, React.ReactNode> = {
  Draft:         <Clock      className="size-4 text-slate-400"   />,
  Pending:       <Clock      className="size-4 text-amber-400"   />,
  UnderReview:   <AlertCircle className="size-4 text-blue-500"   />,
  Shortlisted:   <AlertCircle className="size-4 text-purple-500" />,
  Accepted:      <CheckCircle className="size-4 text-emerald-500"/>,
  Rejected:      <XCircle     className="size-4 text-rose-500"   />,
  Withdrawn:     <XCircle     className="size-4 text-slate-400"  />,
  Intending:     <Clock       className="size-4 text-slate-400"  />,
  Applied:       <Clock       className="size-4 text-amber-400"  />,
  WaitingResult: <AlertCircle className="size-4 text-blue-500"   />,
};

const STATUS_BADGE: Record<ApplicationStatus, string> = {
  Draft:         "bg-slate-100 text-slate-600",
  Pending:       "bg-amber-100 text-amber-700",
  UnderReview:   "bg-blue-100 text-blue-700",
  Shortlisted:   "bg-purple-100 text-purple-700",
  Accepted:      "bg-emerald-100 text-emerald-700",
  Rejected:      "bg-rose-100 text-rose-700",
  Withdrawn:     "bg-slate-100 text-slate-500",
  Intending:     "bg-slate-100 text-slate-600",
  Applied:       "bg-amber-100 text-amber-700",
  WaitingResult: "bg-blue-100 text-blue-700",
};

const EXTERNAL_STATUSES: UpdateExternalStatusRequest["status"][] = [
  "Intending",
  "Applied",
  "WaitingResult",
  "Accepted",
  "Rejected",
];

// ── Page ──────────────────────────────────────────────────────────────────────

export function ApplicationDetail() {
  const { id }       = useParams<{ id: string }>();
  const { t, i18n } = useTranslation(["applications", "common"]);
  const isRtl        = i18n.dir() === "rtl";
  const BackIcon     = isRtl ? ArrowRight : ArrowLeft;

  const [withdrawReason, setWithdrawReason] = useState("");
  const [showWithdrawDialog, setShowWithdrawDialog] = useState(false);
  const [externalStatus, setExternalStatus] =
    useState<UpdateExternalStatusRequest["status"]>("Intending");
  const [externalNotes, setExternalNotes] = useState("");

  const { data, isLoading, isError } = useApplicationDetailQuery(id);
  const submitMut   = useSubmitApplicationMutation();
  const withdrawMut = useWithdrawApplicationMutation();
  const extMut      = useUpdateExternalStatusMutation();

  // ── Handlers ──────────────────────────────────────────────────────────────

  const handleSubmit = () => {
    if (!id) return;
    submitMut.mutate(id, {
      onSuccess: () => toast.success(t("applications:detail.submitSuccess")),
      onError:   () => toast.error(t("common:status.error")),
    });
  };

  const handleWithdraw = () => {
    if (!id) return;
    withdrawMut.mutate(
      { id, req: { reason: withdrawReason || undefined } },
      {
        onSuccess: () => {
          toast.success(t("applications:detail.withdrawSuccess"));
          setShowWithdrawDialog(false);
        },
        onError: () => toast.error(t("common:status.error")),
      },
    );
  };

  const handleUpdateExternal = () => {
    if (!id) return;
    extMut.mutate(
      { id, req: { status: externalStatus, notes: externalNotes || undefined } },
      {
        onSuccess: () => toast.success(t("applications:detail.externalUpdated")),
        onError:   () => toast.error(t("common:status.error")),
      },
    );
  };

  // ── Loading ────────────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="mx-auto max-w-3xl space-y-4">
        <div className="h-8 w-48 animate-pulse rounded-md bg-bg-elevated" />
        <div className="h-48 animate-pulse rounded-xl bg-bg-elevated" />
        <div className="h-64 animate-pulse rounded-xl bg-bg-elevated" />
      </div>
    );
  }

  // ── Error ──────────────────────────────────────────────────────────────────
  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl">
        <div className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-sm text-rose-600">
          {t("common:status.error")}
        </div>
      </div>
    );
  }

  const title       = isRtl ? data.scholarshipTitleAr : data.scholarshipTitleEn;
  const isInApp     = data.mode === "InApp";
  const canSubmit   = isInApp && data.status === "Draft"    && !data.isReadOnly;
  const canWithdraw = !data.isReadOnly && ["Draft", "Pending", "UnderReview"].includes(data.status);

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="mx-auto max-w-3xl space-y-6">

      {/* ── Back link ── */}
      <Link
        to="/student/applications"
        className="inline-flex items-center gap-1.5 text-sm text-text-secondary hover:text-text-primary"
      >
        <BackIcon aria-hidden className="size-4" />
        {t("applications:detail.back")}
      </Link>

      {/* ── Hero card ── */}
      <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1">
            <h1 className="text-xl font-bold text-text-primary">{title}</h1>
            <p className="mt-1 text-sm text-text-secondary">
              {t("applications:detail.deadline")}
              {" "}
              {format(new Date(data.scholarshipDeadline), "dd MMMM yyyy")}
            </p>
          </div>

          {/* Status badge */}
          <span
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
              STATUS_BADGE[data.status],
            )}
          >
            {STATUS_ICON[data.status]}
            {t(`applications:status.${data.status}`)}
          </span>
        </div>

        {data.personalNotes && (
          <p className="mt-4 rounded-lg bg-bg-canvas p-3 text-sm text-text-secondary italic">
            {data.personalNotes}
          </p>
        )}

        {data.decisionReason && (
          <div className="mt-4 rounded-lg border border-border-subtle bg-bg-canvas p-3">
            <p className="text-xs font-semibold text-text-tertiary">
              {t("applications:detail.decisionReason")}
            </p>
            <p className="mt-1 text-sm text-text-secondary">
              {data.decisionReason}
            </p>
          </div>
        )}
      </div>

      {/* ── External tracker ── */}
      {!isInApp && !data.isReadOnly && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
          <h2 className="mb-4 text-sm font-semibold text-text-primary">
            {t("applications:detail.updateExternal")}
          </h2>

          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-xs font-medium text-text-secondary">
                {t("applications:detail.externalStatus")}
              </label>
              <select
                value={externalStatus}
                onChange={(e) =>
                  setExternalStatus(
                    e.target.value as UpdateExternalStatusRequest["status"],
                  )
                }
                className="h-9 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
              >
                {EXTERNAL_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {t(`applications:status.${s}`)}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="mb-1 block text-xs font-medium text-text-secondary">
                {t("applications:detail.notes")}
              </label>
              <textarea
                value={externalNotes}
                onChange={(e) => setExternalNotes(e.target.value)}
                rows={3}
                className="w-full rounded-md border border-border-subtle bg-bg-canvas px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                placeholder={t("applications:detail.notesPlaceholder")}
              />
            </div>

            <button
              type="button"
              onClick={handleUpdateExternal}
              disabled={extMut.isPending}
              className="inline-flex h-9 items-center gap-2 rounded-lg bg-brand-500 px-4 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
            >
              {t("applications:detail.updateExternalBtn")}
            </button>
          </div>

          {data.externalTrackingUrl && (
            <a
              href={data.externalTrackingUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-3 inline-flex items-center gap-1.5 text-sm text-brand-500 hover:underline"
            >
              <ExternalLink aria-hidden className="size-3.5" />
              {t("applications:detail.viewExternal")}
            </a>
          )}
        </div>
      )}

      {/* ── Status Timeline ── */}
      {data.statusHistory.length > 0 && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-xs">
          <h2 className="mb-4 text-sm font-semibold text-text-primary">
            {t("applications:detail.timeline")}
          </h2>
          <ol className="relative border-s border-border-subtle ps-4 space-y-4">
            {data.statusHistory.map((item) => (
              <li key={item.id} className="relative">
                <span className="absolute -start-2.25 top-0.5 flex size-4 items-center justify-center rounded-full border border-border-subtle bg-bg-elevated">
                  <span className="size-1.5 rounded-full bg-brand-500" />
                </span>
                <div className="flex items-center gap-2">
                  <span
                    className={cn(
                      "inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium",
                      STATUS_BADGE[item.toStatus],
                    )}
                  >
                    {t(`applications:status.${item.toStatus}`)}
                  </span>
                  <span className="text-[11px] text-text-tertiary">
                    {format(new Date(item.changedAt), "dd MMM yyyy HH:mm")}
                  </span>
                </div>
                {item.note && (
                  <p className="mt-1 text-xs text-text-secondary">{item.note}</p>
                )}
              </li>
            ))}
          </ol>
        </div>
      )}

      {/* ── CTA buttons ── */}
      <div className="flex flex-wrap gap-3">
        {canSubmit && (
          <button
            type="button"
            onClick={handleSubmit}
            disabled={submitMut.isPending}
            className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-5 py-2.5 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
          >
            {t("applications:detail.submit")}
          </button>
        )}

        {canWithdraw && (
          <button
            type="button"
            onClick={() => setShowWithdrawDialog(true)}
            className="inline-flex items-center gap-2 rounded-lg border border-rose-300 px-5 py-2.5 text-sm font-medium text-rose-600 transition hover:bg-rose-50"
          >
            {t("applications:detail.withdraw")}
          </button>
        )}

        <Link
          to="/student/applications"
          className="inline-flex items-center gap-2 rounded-lg border border-border-subtle bg-bg-elevated px-5 py-2.5 text-sm font-medium text-text-secondary transition hover:border-border-default"
        >
          {t("applications:detail.back")}
        </Link>
      </div>

      {/* ── Withdraw dialog ── */}
      {showWithdrawDialog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-lg">
            <h3 className="text-base font-semibold text-text-primary">
              {t("applications:withdraw.title")}
            </h3>
            <p className="mt-1 text-sm text-text-secondary">
              {t("applications:withdraw.body")}
            </p>

            <div className="mt-4">
              <label className="mb-1 block text-xs font-medium text-text-secondary">
                {t("applications:withdraw.reason")}
              </label>
              <textarea
                value={withdrawReason}
                onChange={(e) => setWithdrawReason(e.target.value)}
                rows={3}
                className="w-full rounded-md border border-border-subtle bg-bg-canvas px-3 py-2 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
                placeholder={t("applications:withdraw.reasonPlaceholder")}
              />
            </div>

            <div className="mt-4 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setShowWithdrawDialog(false)}
                className="rounded-lg border border-border-subtle px-4 py-2 text-sm font-medium text-text-secondary hover:border-border-default"
              >
                {t("common:actions.cancel")}
              </button>
              <button
                type="button"
                onClick={handleWithdraw}
                disabled={withdrawMut.isPending}
                className="rounded-lg bg-rose-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-rose-600 disabled:opacity-50"
              >
                {t("applications:withdraw.confirm")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
