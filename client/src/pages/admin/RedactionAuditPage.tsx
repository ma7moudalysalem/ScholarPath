import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { ShieldCheck, AlertTriangle } from "lucide-react";
import {
  adminApi,
  type PagedResult,
  type RedactionAuditSampleRow,
  type RedactionVerdict,
} from "@/services/api/admin";

const VERDICTS: RedactionVerdict[] = ["Clean", "MissedEmail", "MissedPhone", "MissedCard"];

const VERDICT_STYLES: Record<RedactionVerdict, string> = {
  Clean: "bg-green-50 text-green-700 border-green-200 dark:bg-green-950/40 dark:text-green-400 dark:border-green-800",
  MissedEmail: "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-800",
  MissedPhone: "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-800",
  MissedCard: "bg-red-50 text-red-700 border-red-200 dark:bg-red-950/40 dark:text-red-400 dark:border-red-800",
};

export function RedactionAuditPage() {
  const { t } = useTranslation(["admin"]);
  const qc = useQueryClient();
  const [pendingOnly, setPendingOnly] = useState(true);
  const [page, setPage] = useState(1);

  const q = useQuery<PagedResult<RedactionAuditSampleRow>>({
    queryKey: ["admin", "redaction-audit", pendingOnly, page],
    queryFn: () => adminApi.getRedactionSamples(pendingOnly, page, 25),
  });

  const verdictMut = useMutation({
    mutationFn: ({ sampleId, verdict }: { sampleId: string; verdict: RedactionVerdict }) =>
      adminApi.setRedactionVerdict(sampleId, verdict),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["admin", "redaction-audit"] });
    },
  });

  const pendingCount = useMemo(
    () => q.data?.items.filter((s) => s.verdict == null).length ?? 0,
    [q.data],
  );

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t("admin:redactionAudit.title", { defaultValue: "PII redaction audit" })}
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">
          {t("admin:redactionAudit.subtitle", {
            defaultValue:
              "Monthly sample of 50 Chatbot prompts after redaction. Flag anything that still contains an email, phone, or card number so we can tighten the regex.",
          })}
        </p>
      </header>

      <div className="flex flex-wrap items-center gap-3">
        <label className="inline-flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={pendingOnly}
            onChange={(e) => {
              setPendingOnly(e.target.checked);
              setPage(1);
            }}
            className="size-4 rounded border-border-subtle"
          />
          {t("admin:redactionAudit.pendingOnly", { defaultValue: "Pending only" })}
        </label>
        {q.data && (
          <span className="text-xs text-text-tertiary">
            {t("admin:redactionAudit.count", {
              defaultValue: "{{pending}} pending on this page · {{total}} total",
              pending: pendingCount,
              total: q.data.total,
            })}
          </span>
        )}
      </div>

      {q.isLoading && (
        <div className="space-y-2">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-28 animate-pulse rounded-lg bg-bg-subtle" />
          ))}
        </div>
      )}

      {q.data && q.data.items.length === 0 && (
        <div className="rounded-lg border border-border-subtle bg-bg-elevated p-8 text-center">
          <ShieldCheck aria-hidden className="mx-auto size-10 text-brand-500" />
          <p className="mt-3 text-sm text-text-secondary">
            {t("admin:redactionAudit.empty", { defaultValue: "No samples to review." })}
          </p>
        </div>
      )}

      {q.data && q.data.items.length > 0 && (
        <ul className="space-y-3">
          {q.data.items.map((s) => (
            <li
              key={s.id}
              className="rounded-lg border border-border-subtle bg-bg-elevated p-5"
            >
              <div className="flex flex-wrap items-baseline justify-between gap-3">
                <div className="text-xs text-text-tertiary">
                  <span className="font-medium text-text-secondary">{s.userEmail ?? s.userId}</span>
                  <span className="mx-2">·</span>
                  <time dateTime={s.sampledAt}>{new Date(s.sampledAt).toLocaleString()}</time>
                </div>
                {s.verdict && (
                  <span
                    className={`inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium ${VERDICT_STYLES[s.verdict]}`}
                  >
                    {s.verdict !== "Clean" && <AlertTriangle aria-hidden className="size-3" />}
                    {s.verdict}
                  </span>
                )}
              </div>

              <pre className="mt-3 max-h-48 overflow-auto whitespace-pre-wrap rounded-md border border-border-subtle bg-bg-canvas p-3 font-mono text-xs text-text-primary">
                {s.redactedPrompt}
              </pre>

              {s.verdict == null && (
                <div className="mt-4 flex flex-wrap gap-2">
                  {VERDICTS.map((v) => (
                    <button
                      key={v}
                      type="button"
                      onClick={() => verdictMut.mutate({ sampleId: s.id, verdict: v })}
                      disabled={verdictMut.isPending}
                      className={`rounded-md border px-3 py-1 text-xs font-medium transition disabled:opacity-50 ${
                        v === "Clean"
                          ? "border-green-200 text-green-700 hover:bg-green-50 dark:border-green-800 dark:text-green-400 dark:hover:bg-green-950/40"
                          : "border-amber-200 text-amber-700 hover:bg-amber-50 dark:border-amber-800 dark:text-amber-400 dark:hover:bg-amber-950/40"
                      }`}
                    >
                      {v}
                    </button>
                  ))}
                </div>
              )}

              {s.verdict != null && s.reviewedAt && (
                <p className="mt-3 text-xs text-text-tertiary">
                  {t("admin:redactionAudit.reviewedAt", { defaultValue: "Reviewed" })}{" "}
                  <time dateTime={s.reviewedAt}>{new Date(s.reviewedAt).toLocaleString()}</time>
                </p>
              )}
            </li>
          ))}
        </ul>
      )}

      {q.data && q.data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-md border border-border-subtle px-3 py-1.5 text-xs font-medium disabled:opacity-50"
          >
            {t("admin:pagination.previous", { defaultValue: "Previous" })}
          </button>
          <span className="text-xs text-text-tertiary">
            {t("admin:pagination.pageOf", {
              defaultValue: "Page {{page}} of {{total}}",
              page,
              total: q.data.totalPages,
            })}
          </span>
          <button
            type="button"
            onClick={() => setPage((p) => Math.min(q.data!.totalPages, p + 1))}
            disabled={page === q.data.totalPages}
            className="rounded-md border border-border-subtle px-3 py-1.5 text-xs font-medium disabled:opacity-50"
          >
            {t("admin:pagination.next", { defaultValue: "Next" })}
          </button>
        </div>
      )}
    </div>
  );
}
