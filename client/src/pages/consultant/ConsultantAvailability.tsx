import {
  getMockAvailability,
  resetMockAvailability,
  saveMockAvailability,
  subscribeMockAvailability,
  type AvailabilityDayConfig,
  type AvailabilityDayKey,
  type BlockedDate,
  type MockAvailabilityState,
} from "@/lib/mockAvailabilityStore";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";

const timezoneOptions = ["Africa/Cairo", "Asia/Riyadh", "Asia/Dubai", "Europe/London", "UTC"];

const bookingWindowOptions = ["7", "14", "21", "30"];

const durationOptions = ["30", "45", "60", "90"];

const bufferOptions = ["0", "10", "15", "30"];

function formatTimeLabel(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  const suffix = hours >= 12 ? "PM" : "AM";
  const normalizedHour = hours % 12 === 0 ? 12 : hours % 12;
  const paddedMinutes = minutes.toString().padStart(2, "0");
  return `${normalizedHour}:${paddedMinutes} ${suffix}`;
}

type TFunction = ReturnType<typeof useTranslation>["t"];

function buildPreviewSlots(day: AvailabilityDayConfig, t: TFunction) {
  if (!day.isEnabled) {
    return [];
  }

  const dayLabel = t(`weekdays.${day.key}`);

  return [
    `${dayLabel} · ${formatTimeLabel(day.start)}`,
    `${dayLabel} · ${formatTimeLabel(day.end)}`,
  ];
}

export function ConsultantAvailability() {
  const { t } = useTranslation("consultantPortal");
  const [availability, setAvailability] = useState<MockAvailabilityState>(() =>
    getMockAvailability(),
  );
  const [bannerKey, setBannerKey] = useState("");

  useEffect(() => {
    return subscribeMockAvailability(() => {
      setAvailability(getMockAvailability());
    });
  }, []);

  const availableDaysCount = useMemo(
    () => availability.days.filter((day) => day.isEnabled).length,
    [availability.days],
  );

  const nextOpenSlot = useMemo(() => {
    const firstEnabledDay = availability.days.find((day) => day.isEnabled);

    if (!firstEnabledDay) {
      return t("availability.stats.noActiveAvailability");
    }

    return `${t(`weekdays.${firstEnabledDay.key}`)} · ${formatTimeLabel(firstEnabledDay.start)}`;
  }, [availability.days, t]);

  const previewSlots = useMemo(
    () => availability.days.flatMap((day) => buildPreviewSlots(day, t)).slice(0, 6),
    [availability.days, t],
  );

  const handleTimezoneChange = (value: string) => {
    setAvailability((current) => ({
      ...current,
      timezone: value,
    }));
  };

  const handleBookingWindowChange = (value: string) => {
    setAvailability((current) => ({
      ...current,
      bookingWindow: value,
    }));
  };

  const handleDayEnabledChange = (key: AvailabilityDayKey) => {
    setAvailability((current) => ({
      ...current,
      days: current.days.map((day) =>
        day.key === key ? { ...day, isEnabled: !day.isEnabled } : day,
      ),
    }));
  };

  const handleDayFieldChange = (
    key: AvailabilityDayKey,
    field: "start" | "end" | "slotDuration" | "buffer",
    value: string,
  ) => {
    setAvailability((current) => ({
      ...current,
      days: current.days.map((day) => (day.key === key ? { ...day, [field]: value } : day)),
    }));
  };

  const handleBlockedDateFieldChange = (id: string, field: "date" | "reason", value: string) => {
    setAvailability((current) => ({
      ...current,
      blockedDates: current.blockedDates.map((item) =>
        item.id === id ? { ...item, [field]: value } : item,
      ),
    }));
  };

  const handleAddBlockedDate = () => {
    const newItem: BlockedDate = {
      id: `blocked-${Date.now()}`,
      date: "",
      reason: "",
    };

    setAvailability((current) => ({
      ...current,
      blockedDates: [...current.blockedDates, newItem],
    }));
  };

  const handleRemoveBlockedDate = (id: string) => {
    setAvailability((current) => ({
      ...current,
      blockedDates: current.blockedDates.filter((item) => item.id !== id),
    }));
  };

  const handleSave = () => {
    saveMockAvailability(availability);
    setBannerKey("availability.banner.saved");
  };

  const handleReset = () => {
    const resetState = resetMockAvailability();
    setAvailability(resetState);
    setBannerKey("availability.banner.reset");
  };

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">{t("moduleTag")}</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("availability.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            {t("availability.subtitle")}
          </p>
        </div>

        {bannerKey ? (
          <div className="mt-8 rounded-xl border border-[#bbf7d0] bg-[#f0fdf4] p-5">
            <p className="text-sm font-medium text-[#166534]">{t(bannerKey)}</p>
          </div>
        ) : null}

        <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <div className="inline-flex rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
              {t("availability.stats.activeDays")}
            </div>
            <p className="mt-4 text-3xl font-semibold tracking-[-0.02em] text-[#1d1d1f]">
              {availableDaysCount}
            </p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <div className="inline-flex rounded-full bg-[#fef2f2] px-3 py-1 text-xs font-medium text-[#dc2626]">
              {t("availability.stats.blockedDates")}
            </div>
            <p className="mt-4 text-3xl font-semibold tracking-[-0.02em] text-[#1d1d1f]">
              {availability.blockedDates.length}
            </p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <div className="inline-flex rounded-full bg-[#f0fdf4] px-3 py-1 text-xs font-medium text-[#15803d]">
              {t("availability.stats.nextOpenSlot")}
            </div>
            <p className="mt-4 text-sm font-semibold text-[#1d1d1f]">{nextOpenSlot}</p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <div className="inline-flex rounded-full bg-[#f3f4f6] px-3 py-1 text-xs font-medium text-[#4b5563]">
              {t("availability.stats.bookingWindow")}
            </div>
            <p className="mt-4 text-3xl font-semibold tracking-[-0.02em] text-[#1d1d1f]">
              {availability.bookingWindow}
            </p>
            <p className="mt-1 text-sm text-[#4b5563]">{t("availability.stats.daysAhead")}</p>
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
                {availability.days.map((day) => (
                  <article
                    key={day.key}
                    className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5"
                  >
                    <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                      <div className="space-y-3">
                        <div className="flex flex-wrap items-center gap-3">
                          <p className="text-xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                            {t(`weekdays.${day.key}`)}
                          </p>

                          <span
                            className={`rounded-full px-3 py-1 text-xs font-medium ${
                              day.isEnabled
                                ? "bg-[#f0fdf4] text-[#15803d]"
                                : "bg-[#f3f4f6] text-[#4b5563]"
                            }`}
                          >
                            {day.isEnabled
                              ? t("availability.weeklySchedule.available")
                              : t("availability.weeklySchedule.off")}
                          </span>
                        </div>

                        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                          <label className="block">
                            <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("availability.weeklySchedule.startTime")}
                            </span>
                            <input
                              type="time"
                              value={day.start}
                              onChange={(event) =>
                                handleDayFieldChange(day.key, "start", event.target.value)
                              }
                              className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                            />
                          </label>

                          <label className="block">
                            <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("availability.weeklySchedule.endTime")}
                            </span>
                            <input
                              type="time"
                              value={day.end}
                              onChange={(event) =>
                                handleDayFieldChange(day.key, "end", event.target.value)
                              }
                              className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                            />
                          </label>

                          <label className="block">
                            <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("availability.weeklySchedule.slotDuration")}
                            </span>
                            <select
                              value={day.slotDuration}
                              onChange={(event) =>
                                handleDayFieldChange(day.key, "slotDuration", event.target.value)
                              }
                              className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                            >
                              {durationOptions.map((option) => (
                                <option key={option} value={option}>
                                  {t(`durationOptions.${option}`)}
                                </option>
                              ))}
                            </select>
                          </label>

                          <label className="block">
                            <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                              {t("availability.weeklySchedule.buffer")}
                            </span>
                            <select
                              value={day.buffer}
                              onChange={(event) =>
                                handleDayFieldChange(day.key, "buffer", event.target.value)
                              }
                              className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                            >
                              {bufferOptions.map((option) => (
                                <option key={option} value={option}>
                                  {t(`bufferOptions.${option}`)}
                                </option>
                              ))}
                            </select>
                          </label>
                        </div>
                      </div>

                      <button
                        type="button"
                        onClick={() => handleDayEnabledChange(day.key)}
                        className={`inline-flex h-11 items-center justify-center rounded-lg px-4 text-sm font-medium transition lg:w-[170px] ${
                          day.isEnabled
                            ? "border border-[#dc2626] bg-white text-[#dc2626] hover:bg-[#fef2f2]"
                            : "bg-[#2563eb] text-white hover:bg-[#1d4ed8]"
                        }`}
                      >
                        {day.isEnabled
                          ? t("availability.weeklySchedule.turnOffDay")
                          : t("availability.weeklySchedule.enableDay")}
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            </div>

            <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                <div>
                  <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("availability.bookingRules.title")}
                  </h2>
                  <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                    {t("availability.bookingRules.description")}
                  </p>
                </div>
              </div>

              <div className="mt-6 grid gap-4 sm:grid-cols-2">
                <label className="block">
                  <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("availability.bookingRules.timezone")}
                  </span>
                  <select
                    value={availability.timezone}
                    onChange={(event) => handleTimezoneChange(event.target.value)}
                    className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                  >
                    {timezoneOptions.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="block">
                  <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("availability.bookingRules.bookingWindow")}
                  </span>
                  <select
                    value={availability.bookingWindow}
                    onChange={(event) => handleBookingWindowChange(event.target.value)}
                    className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                  >
                    {bookingWindowOptions.map((option) => (
                      <option key={option} value={option}>
                        {t(`bookingWindowOptions.${option}`)}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>

            <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
                <div>
                  <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                    {t("availability.blockedDates.title")}
                  </h2>
                  <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                    {t("availability.blockedDates.description")}
                  </p>
                </div>

                <button
                  type="button"
                  onClick={handleAddBlockedDate}
                  className="inline-flex h-11 items-center justify-center rounded-lg bg-[#2563eb] px-4 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("availability.blockedDates.add")}
                </button>
              </div>

              <div className="mt-6 space-y-4">
                {availability.blockedDates.map((item) => (
                  <article
                    key={item.id}
                    className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5"
                  >
                    <div className="grid gap-4 lg:grid-cols-[1fr_1.5fr_160px]">
                      <label className="block">
                        <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                          {t("availability.blockedDates.date")}
                        </span>
                        <input
                          type="date"
                          value={item.date}
                          onChange={(event) =>
                            handleBlockedDateFieldChange(item.id, "date", event.target.value)
                          }
                          className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                        />
                      </label>

                      <label className="block">
                        <span className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                          {t("availability.blockedDates.reason")}
                        </span>
                        <input
                          type="text"
                          value={item.reason}
                          onChange={(event) =>
                            handleBlockedDateFieldChange(item.id, "reason", event.target.value)
                          }
                          placeholder={t("availability.blockedDates.reasonPlaceholder")}
                          className="mt-2 h-11 w-full rounded-lg border border-[#d1d5db] bg-white px-3 text-sm text-[#1d1d1f] outline-none focus:border-[#2563eb]"
                        />
                      </label>

                      <div className="flex items-end">
                        <button
                          type="button"
                          onClick={() => handleRemoveBlockedDate(item.id)}
                          className="inline-flex h-11 w-full items-center justify-center rounded-lg border border-[#dc2626] bg-white px-4 text-sm font-medium text-[#dc2626] transition hover:bg-[#fef2f2]"
                        >
                          {t("availability.blockedDates.remove")}
                        </button>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            </div>
          </section>

          <aside className="space-y-6 xl:col-span-4">
            <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("availability.preview.title")}
              </p>

              <p className="mt-3 text-sm leading-6 text-[#4b5563]">
                {t("availability.preview.description")}
              </p>

              <div className="mt-5 space-y-3">
                {previewSlots.length > 0 ? (
                  previewSlots.map((slot) => (
                    <div
                      key={slot}
                      className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] px-4 py-3 text-sm font-medium text-[#1d1d1f]"
                    >
                      {slot}
                    </div>
                  ))
                ) : (
                  <div className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] px-4 py-3 text-sm text-[#4b5563]">
                    {t("availability.preview.empty")}
                  </div>
                )}
              </div>
            </div>

            <div className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("availability.quickActions.title")}
              </p>

              <div className="mt-5 flex flex-col gap-3">
                <button
                  type="button"
                  onClick={handleSave}
                  className="inline-flex h-11 items-center justify-center rounded-lg bg-[#2563eb] px-4 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("availability.quickActions.save")}
                </button>

                <button
                  type="button"
                  onClick={handleReset}
                  className="inline-flex h-11 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("availability.quickActions.reset")}
                </button>

                <Link
                  to="/dev/consultant/bookings"
                  className="inline-flex h-11 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("availability.quickActions.openBookings")}
                </Link>

                <Link
                  to="/dev/consultants"
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
                <p>{t("availability.notes.line3")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
