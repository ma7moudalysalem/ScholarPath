import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { toast } from "sonner";
import {
  useMyAvailabilityQuery,
  useUpdateAvailabilityMutation,
} from "@/hooks/useBookingsQuery";
import type {
  AvailabilityInput,
  AvailabilityRule,
  DayOfWeek,
} from "@/services/api/bookings";
import { formatDate, formatTime } from "@/lib/bookingFormat";

// ── Weekly schedule editor state ──────────────────────────────────────────────
//
// The backend stores availability as a flat list of rules — recurring
// (dayOfWeek + start/end time) or ad-hoc (specific start/end instants). This
// page edits the recurring weekly schedule: one editable row per weekday.

const WEEKDAYS: { key: string; day: DayOfWeek }[] = [
  { key: "sunday", day: "Sunday" },
  { key: "monday", day: "Monday" },
  { key: "tuesday", day: "Tuesday" },
  { key: "wednesday", day: "Wednesday" },
  { key: "thursday", day: "Thursday" },
  { key: "friday", day: "Friday" },
  { key: "saturday", day: "Saturday" },
];

const timezoneOptions = ["Africa/Cairo", "Asia/Riyadh", "Asia/Dubai", "Europe/London", "UTC"];

type DayRow = {
  key: string;
  day: DayOfWeek;
  isEnabled: boolean;
  start: string;
  end: string;
};

/** Trims a server `"HH:mm:ss"` time to the `"HH:mm"` an `<input type="time">` wants. */
function toInputTime(value: string | null | undefined, fallback: string): string {
  if (!value) return fallback;
  return value.slice(0, 5);
}

/** Expands an `"HH:mm"` input value to the `"HH:mm:ss"` the API expects. */
function toApiTime(value: string): string {
  return value.length === 5 ? `${value}:00` : value;
}

/** Builds the editable weekday rows from the consultant's saved rules. */
function buildRows(rules: AvailabilityRule[]): DayRow[] {
  return WEEKDAYS.map(({ key, day }) => {
    const rule = rules.find(
      (r) => r.isRecurring && r.dayOfWeek === day && r.isActive,
    );
    return {
      key,
      day,
      isEnabled: Boolean(rule),
      start: toInputTime(rule?.startTime, "16:00"),
      end: toInputTime(rule?.endTime, "20:00"),
    };
  });
}

/** Picks the timezone to seed the editor with from the saved rules. */
function pickTimezone(rules: AvailabilityRule[]): string {
  return rules.find((rule) => rule.timezone)?.timezone ?? "Africa/Cairo";
}

export function ConsultantAvailability() {
  const { t, i18n } = useTranslation("consultantPortal");
  const lang = i18n.language;

  const { data, isLoading, isError } = useMyAvailabilityQuery();
  const updateMutation = useUpdateAvailabilityMutation();

  const [rows, setRows] = useState<DayRow[]>(() => buildRows([]));
  const [timezone, setTimezone] = useState("Africa/Cairo");

  // Seed the editor from the saved rules without an effect — reset the local
  // form state during render whenever a new `data` snapshot arrives (React's
  // documented "adjusting state when a prop changes" pattern). User edits then
  // persist until the next server refresh.
  const [seededFrom, setSeededFrom] = useState<AvailabilityRule[] | null>(null);
  if (data && data !== seededFrom) {
    setSeededFrom(data);
    setRows(buildRows(data));
    setTimezone(pickTimezone(data));
  }

  // The ad-hoc (one-off) rules are shown read-only — the editor manages the
  // weekly recurring schedule; ad-hoc slots come from elsewhere.
  const adHocRules = useMemo(
    () => (data ?? []).filter((rule) => !rule.isRecurring && rule.isActive),
    [data],
  );

  const activeDaysCount = rows.filter((row) => row.isEnabled).length;

  const handleToggleDay = (key: string) => {
    setRows((current) =>
      current.map((row) =>
        row.key === key ? { ...row, isEnabled: !row.isEnabled } : row,
      ),
    );
  };

  const handleTimeChange = (key: string, field: "start" | "end", value: string) => {
    setRows((current) =>
      current.map((row) => (row.key === key ? { ...row, [field]: value } : row)),
    );
  };

  const handleSave = () => {
    const recurringSlots: AvailabilityInput[] = rows
      .filter((row) => row.isEnabled)
      .map((row) => ({
        isRecurring: true,
        dayOfWeek: row.day,
        startTime: toApiTime(row.start),
        endTime: toApiTime(row.end),
        specificStartAt: null,
        specificEndAt: null,
        timezone,
        isActive: true,
      }));

    // Preserve the consultant's existing ad-hoc rules — the weekly editor only
    // owns the recurring rules, but `ReplaceExisting` rewrites the whole set.
    const adHocSlots: AvailabilityInput[] = adHocRules.map((rule) => ({
      isRecurring: false,
      dayOfWeek: null,
      startTime: null,
      endTime: null,
      specificStartAt: rule.specificStartAt ?? null,
      specificEndAt: rule.specificEndAt ?? null,
      timezone: rule.timezone,
      isActive: rule.isActive,
    }));

    const slots = [...recurringSlots, ...adHocSlots];

    if (slots.length === 0) {
      toast.error(t("availability.errors.noSlots"));
      return;
    }

    updateMutation.mutate(
      { slots, replaceExisting: true },
      {
        onSuccess: () => toast.success(t("availability.banner.saved")),
        onError: () => toast.error(t("states.error")),
      },
    );
  };

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("availability.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            {t("availability.subtitle")}
          </p>
        </div>

        {isError ? (
          <div className="mt-8 rounded-2xl border border-[#fecaca] bg-[#fef2f2] p-6 text-sm font-medium text-[#dc2626]">
            {t("states.error")}
          </div>
        ) : isLoading ? (
          <div className="mt-8 space-y-4">
            <div className="h-24 animate-pulse rounded-xl border border-[#e5e7eb] bg-white shadow-sm" />
            <div className="h-96 animate-pulse rounded-xl border border-[#e5e7eb] bg-white shadow-sm" />
          </div>
        ) : (
          <>
            <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <div className="inline-flex rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
                  {t("availability.stats.activeDays")}
                </div>
                <p className="mt-4 text-3xl font-semibold tracking-[-0.02em] text-[#1d1d1f]">
                  {activeDaysCount}
                </p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <div className="inline-flex rounded-full bg-[#f0fdf4] px-3 py-1 text-xs font-medium text-[#15803d]">
                  {t("availability.stats.adHocSlots")}
                </div>
                <p className="mt-4 text-3xl font-semibold tracking-[-0.02em] text-[#1d1d1f]">
                  {adHocRules.length}
                </p>
              </div>

              <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
                <div className="inline-flex rounded-full bg-[#f3f4f6] px-3 py-1 text-xs font-medium text-[#4b5563]">
                  {t("availability.stats.timezone")}
                </div>
                <p className="mt-4 text-sm font-semibold text-[#1d1d1f]">{timezone}</p>
              </div>
            </div>

            <div className="mt-8 grid gap-6 xl:grid-cols-12">
              <section className="space-y-6 xl:col-span-8">
                <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                  <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                    <div>
                      <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                        {t("availability.weeklySchedule.title")}
                      </h2>
                      <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                        {t("availability.weeklySchedule.description")}
                      </p>
                    </div>
                  </div>

                  <div className="mt-6 space-y-4">
                    {rows.map((row) => (
                      <article
                        key={row.key}
                        className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5"
                      >
                        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                          <div className="space-y-3">
                            <div className="flex flex-wrap items-center gap-3">
                              <p className="text-xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                                {t(`weekdays.${row.key}`)}
                              </p>

                              <span
                                className={`rounded-full px-3 py-1 text-xs font-medium ${
                                  row.isEnabled
                                    ? "bg-[#f0fdf4] text-[#15803d]"
                                    : "bg-[#f3f4f6] text-[#4b5563]"
                                }`}
                              >
                                {row.isEnabled
                                  ? t("availability.weeklySchedule.available")
                                  : t("availability.weeklySchedule.off")}
                              </span>
                            </div>

                            <div className="grid gap-4 sm:grid-cols-2">
                              <label className="block">
                                <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                                  {t("availability.weeklySchedule.startTime")}
                                </span>
                                <input
                                  type="time"
                                  value={row.start}
                                  disabled={!row.isEnabled}
                                  onChange={(event) =>
                                    handleTimeChange(row.key, "start", event.target.value)
                                  }
                                  className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb] disabled:cursor-not-allowed disabled:bg-[#f3f4f6]"
                                />
                              </label>

                              <label className="block">
                                <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                                  {t("availability.weeklySchedule.endTime")}
                                </span>
                                <input
                                  type="time"
                                  value={row.end}
                                  disabled={!row.isEnabled}
                                  onChange={(event) =>
                                    handleTimeChange(row.key, "end", event.target.value)
                                  }
                                  className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb] disabled:cursor-not-allowed disabled:bg-[#f3f4f6]"
                                />
                              </label>
                            </div>
                          </div>

                          <button
                            type="button"
                            onClick={() => handleToggleDay(row.key)}
                            className={`inline-flex h-11 items-center justify-center rounded-lg px-4 text-sm font-medium transition lg:w-[170px] ${
                              row.isEnabled
                                ? "border border-[#dc2626] bg-white text-[#dc2626] hover:bg-[#fef2f2]"
                                : "bg-[#2563eb] text-white hover:bg-[#1d4ed8]"
                            }`}
                          >
                            {row.isEnabled
                              ? t("availability.weeklySchedule.turnOffDay")
                              : t("availability.weeklySchedule.enableDay")}
                          </button>
                        </div>
                      </article>
                    ))}
                  </div>
                </div>

                <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                  <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("availability.bookingRules.title")}
                  </h2>
                  <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                    {t("availability.bookingRules.description")}
                  </p>

                  <div className="mt-6 grid gap-4 sm:grid-cols-2">
                    <label className="block">
                      <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("availability.bookingRules.timezone")}
                      </span>
                      <select
                        value={timezone}
                        onChange={(event) => setTimezone(event.target.value)}
                        className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                      >
                        {timezoneOptions.map((option) => (
                          <option key={option} value={option}>
                            {option}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>
                </div>

                {adHocRules.length > 0 ? (
                  <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                    <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                      {t("availability.adHoc.title")}
                    </h2>
                    <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                      {t("availability.adHoc.description")}
                    </p>

                    <div className="mt-6 space-y-3">
                      {adHocRules.map((rule) => (
                        <div
                          key={rule.id}
                          className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] px-4 py-3 text-sm font-medium text-[#1d1d1f]"
                        >
                          {rule.specificStartAt
                            ? `${formatDate(rule.specificStartAt, lang)} · ${formatTime(
                                rule.specificStartAt,
                                lang,
                              )}`
                            : t("availability.adHoc.untimed")}
                          {rule.specificEndAt
                            ? ` – ${formatTime(rule.specificEndAt, lang)}`
                            : ""}
                        </div>
                      ))}
                    </div>
                  </div>
                ) : null}
              </section>

              <aside className="space-y-6 xl:col-span-4">
                <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                  <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("availability.quickActions.title")}
                  </p>

                  <div className="mt-5 flex flex-col gap-3">
                    <button
                      type="button"
                      onClick={handleSave}
                      disabled={updateMutation.isPending}
                      className="inline-flex h-11 items-center justify-center rounded-lg bg-[#2563eb] px-4 text-sm font-medium text-white transition hover:bg-[#1d4ed8] disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {updateMutation.isPending
                        ? t("states.submitting")
                        : t("availability.quickActions.save")}
                    </button>

                    <Link
                      to="/consultant/bookings"
                      className="inline-flex h-11 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                    >
                      {t("availability.quickActions.openBookings")}
                    </Link>

                    <Link
                      to="/student/consultants"
                      className="inline-flex h-11 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                    >
                      {t("availability.quickActions.openMarketplace")}
                    </Link>
                  </div>
                </div>

                <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                  <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("availability.notes.title")}
                  </p>

                  <div className="mt-5 space-y-3 text-sm leading-6 text-[#4b5563]">
                    <p>{t("availability.notes.line1")}</p>
                    <p>{t("availability.notes.line2")}</p>
                  </div>
                </div>
              </aside>
            </div>
          </>
        )}
      </section>
    </main>
  );
}
