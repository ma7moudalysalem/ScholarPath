import { useState, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ApiError } from "@/services/api/client";
import {
  financialConfigApi,
  type CreateFinancialRuleBody,
  type FeeKind,
  type FinancialCalculationPreviewDto,
  type FinancialConfigRuleDto,
  type FinancialRuleStatus,
  type PreviewParams,
} from "@/services/api/financialConfig";
import type { PaymentType } from "@/services/api/payments";

const PAYMENT_TYPES: PaymentType[] = ["ConsultantBooking", "CompanyReview"];
const FEE_KINDS: FeeKind[] = ["Percentage", "FixedAmount"];
const STATUSES: FinancialRuleStatus[] = ["Draft", "Active", "Archived"];

const INPUT_CLS =
  "mt-1 h-10 w-full rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm focus:border-brand-500 focus:outline-none";

const fmtMoney = (cents: number) =>
  `$${(cents / 100).toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
const fmtPct = (fraction: number) => `${(fraction * 100).toFixed(2)}%`;
const dateOnly = (iso: string) => iso.slice(0, 10);
const todayInput = () => new Date().toISOString().slice(0, 10);

function apiErr(e: unknown, fallback: string): string {
  return e instanceof ApiError ? (e.payload.detail ?? e.payload.title) : fallback;
}

// ─────────────────────────────────────────────────────────────────────────────

export function AdminFinancialConfig() {
  const { t } = useTranslation(["payments", "common"]);
  const qc = useQueryClient();

  const [filterType, setFilterType] = useState<PaymentType | "">("");
  const [filterStatus, setFilterStatus] = useState<FinancialRuleStatus | "">("");
  const [form, setForm] = useState<
    { mode: "create" } | { mode: "edit"; rule: FinancialConfigRuleDto } | null
  >(null);

  const rulesQuery = useQuery({
    queryKey: ["admin", "financial-config", filterType, filterStatus],
    queryFn: () =>
      financialConfigApi.list({
        paymentType: filterType || undefined,
        status: filterStatus || undefined,
      }),
  });

  const lifecycle = useMutation({
    mutationFn: ({ id, action }: { id: string; action: "activate" | "deactivate" | "archive" }) =>
      financialConfigApi[action](id),
    onSuccess: (_data, { action }) => {
      toast.success(t(`payments:financialConfig.toast.${action}d`));
      void qc.invalidateQueries({ queryKey: ["admin", "financial-config"] });
    },
    onError: (e) => toast.error(apiErr(e, t("payments:financialConfig.toast.error"))),
  });

  const onLifecycle = (id: string, action: "activate" | "deactivate" | "archive") => {
    if (!window.confirm(t(`payments:financialConfig.confirm.${action}`))) return;
    lifecycle.mutate({ id, action });
  };

  const rules = rulesQuery.data ?? [];

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            {t("payments:financialConfig.title")}
          </h1>
          <p className="mt-1 text-sm text-text-secondary">
            {t("payments:financialConfig.subtitle")}
          </p>
        </div>
        {!form && (
          <button
            type="button"
            onClick={() => setForm({ mode: "create" })}
            className="inline-flex h-10 items-center rounded-md bg-brand-500 px-4 text-sm font-medium text-white hover:opacity-90"
          >
            {t("payments:financialConfig.newRule")}
          </button>
        )}
      </div>

      {form && (
        <RuleForm
          key={form.mode === "edit" ? form.rule.id : "create"}
          rule={form.mode === "edit" ? form.rule : undefined}
          onClose={() => setForm(null)}
        />
      )}

      <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
        <div className="mb-4 flex flex-wrap gap-3">
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value as PaymentType | "")}
            className="h-9 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          >
            <option value="">{t("payments:financialConfig.filterType")}</option>
            {PAYMENT_TYPES.map((pt) => (
              <option key={pt} value={pt}>
                {t(`payments:paymentType.${pt}`)}
              </option>
            ))}
          </select>
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value as FinancialRuleStatus | "")}
            className="h-9 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm"
          >
            <option value="">{t("payments:financialConfig.filterStatus")}</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {t(`payments:financialConfig.status.${s}`)}
              </option>
            ))}
          </select>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-text-tertiary">
              <tr>
                <th className="px-3 py-2 text-start">{t("payments:financialConfig.table.type")}</th>
                <th className="px-3 py-2 text-start">{t("payments:financialConfig.table.fee")}</th>
                <th className="px-3 py-2 text-start">
                  {t("payments:financialConfig.table.profitShare")}
                </th>
                <th className="px-3 py-2 text-start">
                  {t("payments:financialConfig.table.status")}
                </th>
                <th className="px-3 py-2 text-start">{t("payments:financialConfig.table.from")}</th>
                <th className="px-3 py-2 text-start">{t("payments:financialConfig.table.to")}</th>
                <th className="px-3 py-2 text-start">
                  {t("payments:financialConfig.table.notes")}
                </th>
                <th className="px-3 py-2 text-end">
                  {t("payments:financialConfig.table.actions")}
                </th>
              </tr>
            </thead>
            <tbody>
              {rulesQuery.isLoading && (
                <tr>
                  <td colSpan={8} className="px-3 py-6 text-center text-text-tertiary">
                    {t("payments:common.loading")}
                  </td>
                </tr>
              )}
              {rulesQuery.isError && (
                <tr>
                  <td colSpan={8} className="px-3 py-6 text-center text-text-tertiary">
                    {t("payments:financialConfig.loadError")}
                  </td>
                </tr>
              )}
              {!rulesQuery.isLoading && !rulesQuery.isError && rules.length === 0 && (
                <tr>
                  <td colSpan={8} className="px-3 py-6 text-center text-text-tertiary">
                    {t("payments:financialConfig.empty")}
                  </td>
                </tr>
              )}
              {rules.map((r) => (
                <tr key={r.id} className="border-t border-border-subtle">
                  <td className="px-3 py-2">{t(`payments:paymentType.${r.paymentType}`)}</td>
                  <td className="px-3 py-2 font-medium">
                    {r.feeKind === "Percentage"
                      ? fmtPct(r.feePercentage ?? 0)
                      : fmtMoney(r.feeAmountCents ?? 0)}
                  </td>
                  <td className="px-3 py-2 font-medium">{fmtPct(r.profitSharePercentage)}</td>
                  <td className="px-3 py-2">
                    <StatusBadge status={r.status} />
                  </td>
                  <td className="px-3 py-2 text-xs text-text-tertiary">
                    {dateOnly(r.effectiveFrom)}
                  </td>
                  <td className="px-3 py-2 text-xs text-text-tertiary">
                    {r.effectiveTo ? dateOnly(r.effectiveTo) : "—"}
                  </td>
                  <td className="px-3 py-2 text-text-secondary">{r.notes ?? "—"}</td>
                  <td className="px-3 py-2">
                    <div className="flex flex-wrap justify-end gap-1.5">
                      {r.status === "Draft" && (
                        <>
                          <ActionBtn onClick={() => setForm({ mode: "edit", rule: r })}>
                            {t("payments:financialConfig.actions.edit")}
                          </ActionBtn>
                          <ActionBtn
                            variant="primary"
                            disabled={lifecycle.isPending}
                            onClick={() => onLifecycle(r.id, "activate")}
                          >
                            {t("payments:financialConfig.actions.activate")}
                          </ActionBtn>
                          <ActionBtn
                            disabled={lifecycle.isPending}
                            onClick={() => onLifecycle(r.id, "archive")}
                          >
                            {t("payments:financialConfig.actions.archive")}
                          </ActionBtn>
                        </>
                      )}
                      {r.status === "Active" && (
                        <>
                          <ActionBtn
                            disabled={lifecycle.isPending}
                            onClick={() => onLifecycle(r.id, "deactivate")}
                          >
                            {t("payments:financialConfig.actions.deactivate")}
                          </ActionBtn>
                          <ActionBtn
                            disabled={lifecycle.isPending}
                            onClick={() => onLifecycle(r.id, "archive")}
                          >
                            {t("payments:financialConfig.actions.archive")}
                          </ActionBtn>
                        </>
                      )}
                      {r.status === "Archived" && (
                        <span className="text-xs text-text-tertiary">—</span>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <Simulator rules={rules} />
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: FinancialRuleStatus }) {
  const { t } = useTranslation("payments");
  const cls =
    status === "Active"
      ? "bg-success-100 text-success-600"
      : status === "Draft"
        ? "bg-brand-500/10 text-brand-500"
        : "bg-bg-subtle text-text-tertiary";
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
      {t(`financialConfig.status.${status}`)}
    </span>
  );
}

function ActionBtn({
  onClick,
  variant,
  disabled,
  children,
}: {
  onClick: () => void;
  variant?: "primary";
  disabled?: boolean;
  children: ReactNode;
}) {
  const cls =
    variant === "primary"
      ? "bg-brand-500 text-white hover:opacity-90"
      : "border border-border-subtle text-text-secondary hover:bg-bg-subtle";
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={`rounded-md px-2.5 py-1 text-xs font-medium disabled:opacity-50 ${cls}`}
    >
      {children}
    </button>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="block text-sm">
      <span className="text-text-secondary">{label}</span>
      {children}
    </label>
  );
}

// ─────────────────────────────────────────────────────────────────────────────

function RuleForm({ rule, onClose }: { rule?: FinancialConfigRuleDto; onClose: () => void }) {
  const { t } = useTranslation(["payments", "common"]);
  const qc = useQueryClient();
  const isEdit = !!rule;

  const [paymentType, setPaymentType] = useState<PaymentType>(
    rule?.paymentType ?? "ConsultantBooking",
  );
  const [feeKind, setFeeKind] = useState<FeeKind>(rule?.feeKind ?? "Percentage");
  const [feePct, setFeePct] = useState(
    rule?.feePercentage != null ? String(rule.feePercentage * 100) : "",
  );
  const [feeAmount, setFeeAmount] = useState(
    rule?.feeAmountCents != null ? String(rule.feeAmountCents / 100) : "",
  );
  const [profitShare, setProfitShare] = useState(
    rule ? String(rule.profitSharePercentage * 100) : "",
  );
  const [effectiveFrom, setEffectiveFrom] = useState(
    rule ? dateOnly(rule.effectiveFrom) : todayInput(),
  );
  const [effectiveTo, setEffectiveTo] = useState(rule?.effectiveTo ? dateOnly(rule.effectiveTo) : "");
  const [notes, setNotes] = useState(rule?.notes ?? "");

  const mut = useMutation({
    mutationFn: async (body: CreateFinancialRuleBody) => {
      if (rule) await financialConfigApi.update(rule.id, body);
      else await financialConfigApi.create(body);
    },
    onSuccess: () => {
      toast.success(
        t(isEdit ? "payments:financialConfig.toast.updated" : "payments:financialConfig.toast.created"),
      );
      void qc.invalidateQueries({ queryKey: ["admin", "financial-config"] });
      onClose();
    },
    onError: (e) => toast.error(apiErr(e, t("payments:financialConfig.toast.error"))),
  });

  const submit = () => {
    const ps = Number(profitShare);
    if (!Number.isFinite(ps) || ps < 0 || ps > 100) {
      toast.error(t("payments:financialConfig.validation.profitShare"));
      return;
    }

    let feePercentage: number | null = null;
    let feeAmountCents: number | null = null;
    if (feeKind === "Percentage") {
      const fp = Number(feePct);
      if (!Number.isFinite(fp) || fp < 0 || fp > 100) {
        toast.error(t("payments:financialConfig.validation.feePercentage"));
        return;
      }
      feePercentage = fp / 100;
    } else {
      const fa = Number(feeAmount);
      if (!Number.isFinite(fa) || fa <= 0) {
        toast.error(t("payments:financialConfig.validation.feeAmount"));
        return;
      }
      feeAmountCents = Math.round(fa * 100);
    }

    if (!effectiveFrom) {
      toast.error(t("payments:financialConfig.validation.effectiveFrom"));
      return;
    }

    mut.mutate({
      paymentType,
      feeKind,
      feePercentage,
      feeAmountCents,
      profitSharePercentage: ps / 100,
      effectiveFrom: new Date(effectiveFrom).toISOString(),
      effectiveTo: effectiveTo ? new Date(effectiveTo).toISOString() : null,
      notes: notes.trim() || null,
    });
  };

  return (
    <section className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <h2 className="text-lg font-semibold">
        {t(
          isEdit
            ? "payments:financialConfig.form.editTitle"
            : "payments:financialConfig.form.createTitle",
        )}
      </h2>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={t("payments:financialConfig.form.paymentType")}>
          <select
            value={paymentType}
            disabled={isEdit}
            onChange={(e) => setPaymentType(e.target.value as PaymentType)}
            className={`${INPUT_CLS} disabled:opacity-60`}
          >
            {PAYMENT_TYPES.map((pt) => (
              <option key={pt} value={pt}>
                {t(`payments:paymentType.${pt}`)}
              </option>
            ))}
          </select>
        </Field>

        <Field label={t("payments:financialConfig.form.feeKind")}>
          <select
            value={feeKind}
            onChange={(e) => setFeeKind(e.target.value as FeeKind)}
            className={INPUT_CLS}
          >
            {FEE_KINDS.map((fk) => (
              <option key={fk} value={fk}>
                {t(`payments:financialConfig.feeKind.${fk}`)}
              </option>
            ))}
          </select>
        </Field>

        {feeKind === "Percentage" ? (
          <Field label={t("payments:financialConfig.form.feePercentage")}>
            <input
              type="number"
              min={0}
              max={100}
              step={0.1}
              value={feePct}
              onChange={(e) => setFeePct(e.target.value)}
              className={INPUT_CLS}
            />
          </Field>
        ) : (
          <Field label={t("payments:financialConfig.form.feeAmount")}>
            <input
              type="number"
              min={0}
              step={0.01}
              value={feeAmount}
              onChange={(e) => setFeeAmount(e.target.value)}
              className={INPUT_CLS}
            />
          </Field>
        )}

        <Field label={t("payments:financialConfig.form.profitShare")}>
          <input
            type="number"
            min={0}
            max={100}
            step={0.1}
            value={profitShare}
            onChange={(e) => setProfitShare(e.target.value)}
            className={INPUT_CLS}
          />
        </Field>

        <Field label={t("payments:financialConfig.form.effectiveFrom")}>
          <input
            type="date"
            value={effectiveFrom}
            onChange={(e) => setEffectiveFrom(e.target.value)}
            className={INPUT_CLS}
          />
        </Field>

        <Field label={t("payments:financialConfig.form.effectiveTo")}>
          <input
            type="date"
            value={effectiveTo}
            onChange={(e) => setEffectiveTo(e.target.value)}
            className={INPUT_CLS}
          />
        </Field>
      </div>

      <Field label={t("payments:financialConfig.form.notes")}>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          placeholder={t("payments:financialConfig.form.notesPlaceholder")}
          className="mt-1 w-full rounded-md border border-border-subtle bg-bg-elevated px-3 py-2 text-sm focus:border-brand-500 focus:outline-none"
        />
      </Field>

      <div className="flex gap-2">
        <button
          type="button"
          onClick={submit}
          disabled={mut.isPending}
          className="inline-flex items-center justify-center rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
        >
          {mut.isPending
            ? t("payments:financialConfig.form.saving")
            : t("payments:financialConfig.form.save")}
        </button>
        <button
          type="button"
          onClick={onClose}
          className="inline-flex items-center justify-center rounded-md border border-border-subtle px-4 py-2 text-sm font-medium text-text-secondary hover:bg-bg-subtle"
        >
          {t("payments:financialConfig.form.cancel")}
        </button>
      </div>
    </section>
  );
}

// ─────────────────────────────────────────────────────────────────────────────

function Simulator({ rules }: { rules: FinancialConfigRuleDto[] }) {
  const { t } = useTranslation(["payments", "common"]);
  const [gross, setGross] = useState("");
  const [target, setTarget] = useState("type:ConsultantBooking");
  const [result, setResult] = useState<FinancialCalculationPreviewDto | null>(null);

  const mut = useMutation({
    mutationFn: (params: PreviewParams) => financialConfigApi.preview(params),
    onSuccess: (data) => setResult(data),
    onError: (e) => toast.error(apiErr(e, t("payments:financialConfig.toast.error"))),
  });

  const previewable = rules.filter((r) => r.status !== "Archived");

  const ruleLabel = (r: FinancialConfigRuleDto) => {
    const fee =
      r.feeKind === "Percentage" ? fmtPct(r.feePercentage ?? 0) : fmtMoney(r.feeAmountCents ?? 0);
    return `${t(`payments:paymentType.${r.paymentType}`)} · ${t(
      `payments:financialConfig.status.${r.status}`,
    )} · ${fee} + ${fmtPct(r.profitSharePercentage)}`;
  };

  const run = () => {
    const g = Number(gross);
    if (!Number.isFinite(g) || g <= 0) {
      toast.error(t("payments:financialConfig.validation.grossAmount"));
      return;
    }
    const grossAmountCents = Math.round(g * 100);
    if (target.startsWith("type:")) {
      mut.mutate({ grossAmountCents, paymentType: target.slice(5) as PaymentType });
    } else {
      mut.mutate({ grossAmountCents, ruleId: target.slice(5) });
    }
  };

  return (
    <section className="rounded-lg border border-border-subtle bg-bg-elevated p-5">
      <h2 className="text-lg font-semibold">{t("payments:financialConfig.simulator.title")}</h2>
      <p className="mt-1 text-sm text-text-secondary">
        {t("payments:financialConfig.simulator.subtitle")}
      </p>

      <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Field label={t("payments:financialConfig.simulator.grossAmount")}>
          <input
            type="number"
            min={0}
            step={0.01}
            value={gross}
            onChange={(e) => setGross(e.target.value)}
            className={INPUT_CLS}
          />
        </Field>

        <Field label={t("payments:financialConfig.simulator.target")}>
          <select
            value={target}
            onChange={(e) => setTarget(e.target.value)}
            className={INPUT_CLS}
          >
            {PAYMENT_TYPES.map((pt) => (
              <option key={pt} value={`type:${pt}`}>
                {t("payments:financialConfig.simulator.activeFor", {
                  type: t(`payments:paymentType.${pt}`),
                })}
              </option>
            ))}
            {previewable.map((r) => (
              <option key={r.id} value={`rule:${r.id}`}>
                {ruleLabel(r)}
              </option>
            ))}
          </select>
        </Field>

        <div className="flex items-end">
          <button
            type="button"
            onClick={run}
            disabled={mut.isPending || gross === ""}
            className="inline-flex h-10 items-center justify-center rounded-md bg-brand-500 px-4 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
          >
            {t("payments:financialConfig.simulator.run")}
          </button>
        </div>
      </div>

      {result && (
        <div className="mt-5 space-y-3 border-t border-border-subtle pt-4">
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Stat
              label={t("payments:financialConfig.simulator.fee")}
              value={fmtMoney(result.feeCents)}
            />
            <Stat
              label={t("payments:financialConfig.simulator.profitShare")}
              value={fmtMoney(result.profitShareCents)}
            />
            <Stat
              label={t("payments:financialConfig.simulator.platformTotal")}
              value={fmtMoney(result.platformTotalCents)}
            />
            <Stat
              label={t("payments:financialConfig.simulator.payeeNet")}
              value={fmtMoney(result.payeeNetCents)}
              highlight={!result.isViable}
            />
          </div>
          <p className="text-xs text-text-tertiary">
            {t("payments:financialConfig.simulator.effectiveRate")}:{" "}
            <span className="font-medium text-text-secondary">
              {fmtPct(result.effectiveFeeRate)}
            </span>
          </p>
          {result.usedFallback && (
            <Notice tone="info">{t("payments:financialConfig.simulator.fallbackNote")}</Notice>
          )}
          {!result.isViable && (
            <Notice tone="warn">{t("payments:financialConfig.simulator.notViableNote")}</Notice>
          )}
        </div>
      )}
    </section>
  );
}

function Stat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="rounded-md border border-border-subtle bg-bg-subtle p-3">
      <p className="text-xs text-text-tertiary">{label}</p>
      <p
        className={`mt-1 text-lg font-semibold ${
          highlight ? "text-danger-500" : "text-text-primary"
        }`}
      >
        {value}
      </p>
    </div>
  );
}

function Notice({ tone, children }: { tone: "info" | "warn"; children: ReactNode }) {
  const cls =
    tone === "warn"
      ? "border-danger-200 bg-danger-50 text-danger-500"
      : "border-border-subtle bg-bg-subtle text-text-secondary";
  return <p className={`rounded-md border px-3 py-2 text-xs ${cls}`}>{children}</p>;
}
