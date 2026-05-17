import {
  getMockAvailability,
  subscribeMockAvailability,
  type MockAvailabilityState,
} from "@/lib/mockAvailabilityStore";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";

type Tone = "green" | "blue" | "amber" | "gray";

type SlotTagKey = "next" | "available" | "popular" | "limited";
type SlotDayKey = "tomorrow" | "saturday" | "sunday" | "monday" | "tuesday";
type BadgeKey =
  | "availableToday"
  | "liveSchedule"
  | "noAvailability"
  | "available"
  | "thisWeek"
  | "weekend";

type SlotCard = {
  id: string;
  dayKey?: SlotDayKey;
  dayLabel: string;
  dateLabel: string;
  timeLabel: string;
  durationLabel: string;
  durationMinutes: number;
  tagKey: SlotTagKey;
  tagTone: Tone;
};

type ConsultantProfile = {
  id: string;
  name: string;
  expertise: string;
  bio: string;
  rating: string;
  sessions: string;
  fee: string;
  baseDuration: string;
  baseDurationMinutes: number;
  badgeKey: BadgeKey;
  tags: Array<{
    key: string;
    bg: string;
    text: string;
  }>;
  staticSlots?: SlotCard[];
};

const consultants: ConsultantProfile[] = [
  {
    id: "1",
    name: "Dr. Sarah Adel",
    expertise: "Scholarship Strategy · Personal Statements · Interview Preparation",
    bio: "Dr. Sarah Adel supports students through scholarship planning, shortlist strategy, essay refinement, and application readiness. Her sessions are focused on practical guidance, stronger positioning, and improving the quality of applications for international opportunities.",
    rating: "4.9 / 5",
    sessions: "124",
    fee: "$35",
    baseDuration: "45 min",
    baseDurationMinutes: 45,
    badgeKey: "liveSchedule",
    tags: [
      {
        key: "uk-admissions",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        key: "essays",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        key: "interview-prep",
        bg: "bg-[#fffbeb]",
        text: "text-[#b45309]",
      },
    ],
  },
  {
    id: "2",
    name: "Ahmed Mostafa",
    expertise: "Visa Guidance · University Shortlisting · Funding Plans",
    bio: "Ahmed Mostafa helps students plan study-abroad pathways, narrow target universities, and understand the practical steps of visa preparation, document readiness, and scholarship-fit decisions.",
    rating: "4.7 / 5",
    sessions: "89",
    fee: "$25",
    baseDuration: "30 min",
    baseDurationMinutes: 30,
    badgeKey: "thisWeek",
    tags: [
      {
        key: "visa-support",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        key: "university-choice",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        key: "funding-plans",
        bg: "bg-[#fffbeb]",
        text: "text-[#b45309]",
      },
    ],
    staticSlots: [
      {
        id: "2-slot-1",
        dayKey: "tomorrow",
        dayLabel: "Tomorrow",
        dateLabel: "26 Apr 2026",
        timeLabel: "4:00 PM",
        durationLabel: "30 min",
        durationMinutes: 30,
        tagKey: "available",
        tagTone: "green",
      },
      {
        id: "2-slot-2",
        dayKey: "saturday",
        dayLabel: "Saturday",
        dateLabel: "27 Apr 2026",
        timeLabel: "12:30 PM",
        durationLabel: "30 min",
        durationMinutes: 30,
        tagKey: "popular",
        tagTone: "amber",
      },
      {
        id: "2-slot-3",
        dayKey: "sunday",
        dayLabel: "Sunday",
        dateLabel: "28 Apr 2026",
        timeLabel: "5:00 PM",
        durationLabel: "30 min",
        durationMinutes: 30,
        tagKey: "available",
        tagTone: "green",
      },
      {
        id: "2-slot-4",
        dayKey: "monday",
        dayLabel: "Monday",
        dateLabel: "29 Apr 2026",
        timeLabel: "6:00 PM",
        durationLabel: "30 min",
        durationMinutes: 30,
        tagKey: "available",
        tagTone: "green",
      },
    ],
  },
  {
    id: "3",
    name: "Nour Elhassan",
    expertise: "Full Scholarship Planning · Application Review · Deadline Strategy",
    bio: "Nour Elhassan focuses on full scholarship planning, structured application review, deadline strategy, and helping students improve readiness for competitive funding opportunities.",
    rating: "4.8 / 5",
    sessions: "102",
    fee: "$40",
    baseDuration: "60 min",
    baseDurationMinutes: 60,
    badgeKey: "weekend",
    tags: [
      {
        key: "full-funding",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        key: "application-review",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        key: "planning",
        bg: "bg-[#fffbeb]",
        text: "text-[#b45309]",
      },
    ],
    staticSlots: [
      {
        id: "3-slot-1",
        dayKey: "saturday",
        dayLabel: "Saturday",
        dateLabel: "27 Apr 2026",
        timeLabel: "11:00 AM",
        durationLabel: "60 min",
        durationMinutes: 60,
        tagKey: "popular",
        tagTone: "amber",
      },
      {
        id: "3-slot-2",
        dayKey: "sunday",
        dayLabel: "Sunday",
        dateLabel: "28 Apr 2026",
        timeLabel: "2:00 PM",
        durationLabel: "60 min",
        durationMinutes: 60,
        tagKey: "available",
        tagTone: "green",
      },
      {
        id: "3-slot-3",
        dayKey: "monday",
        dayLabel: "Monday",
        dateLabel: "29 Apr 2026",
        timeLabel: "8:00 PM",
        durationLabel: "60 min",
        durationMinutes: 60,
        tagKey: "available",
        tagTone: "green",
      },
      {
        id: "3-slot-4",
        dayKey: "tuesday",
        dayLabel: "Tuesday",
        dateLabel: "30 Apr 2026",
        timeLabel: "5:30 PM",
        durationLabel: "60 min",
        durationMinutes: 60,
        tagKey: "limited",
        tagTone: "blue",
      },
    ],
  },
];

const weekdayKeyByIndex = [
  "sunday",
  "monday",
  "tuesday",
  "wednesday",
  "thursday",
  "friday",
  "saturday",
] as const;

function formatTimeLabel(value: string) {
  const [hours, minutes] = value.split(":").map(Number);
  const suffix = hours >= 12 ? "PM" : "AM";
  const normalizedHour = hours % 12 === 0 ? 12 : hours % 12;
  const paddedMinutes = minutes.toString().padStart(2, "0");
  return `${normalizedHour}:${paddedMinutes} ${suffix}`;
}

function formatDateKey(date: Date) {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, "0");
  const day = `${date.getDate()}`.padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDateLabel(date: Date) {
  return date.toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function getToneClasses(tone: Tone) {
  switch (tone) {
    case "green":
      return "bg-[#f0fdf4] text-[#15803d]";
    case "blue":
      return "bg-[#eff6ff] text-[#1d4ed8]";
    case "amber":
      return "bg-[#fffbeb] text-[#b45309]";
    default:
      return "bg-[#f3f4f6] text-[#4b5563]";
  }
}

function getDynamicBadge(availability: MockAvailabilityState): BadgeKey {
  const today = new Date();
  const weekdayKey = weekdayKeyByIndex[today.getDay()];
  const todayKey = formatDateKey(today);

  const blockedSet = new Set(
    availability.blockedDates.filter((item) => item.date).map((item) => item.date),
  );

  const todayConfig = availability.days.find((day) => day.key === weekdayKey);

  if (todayConfig?.isEnabled && !blockedSet.has(todayKey)) {
    return "availableToday";
  }

  const hasEnabledDay = availability.days.some((day) => day.isEnabled);
  return hasEnabledDay ? "liveSchedule" : "noAvailability";
}

function buildDynamicSlots(availability: MockAvailabilityState, limit = 6): SlotCard[] {
  const slots: SlotCard[] = [];
  const bookingWindow = Number(availability.bookingWindow) || 21;

  const enabledDays = new Map(
    availability.days.filter((day) => day.isEnabled).map((day) => [day.key, day]),
  );

  const blockedSet = new Set(
    availability.blockedDates.filter((item) => item.date).map((item) => item.date),
  );

  const startDate = new Date();
  startDate.setHours(0, 0, 0, 0);

  for (let offset = 0; offset <= bookingWindow + 14 && slots.length < limit; offset += 1) {
    const date = new Date(startDate);
    date.setDate(startDate.getDate() + offset);

    const dateKey = formatDateKey(date);

    if (blockedSet.has(dateKey)) {
      continue;
    }

    const weekdayKey = weekdayKeyByIndex[date.getDay()];
    const dayConfig = enabledDays.get(weekdayKey);

    if (!dayConfig) {
      continue;
    }

    const slotTone: Tone =
      slots.length === 0 ? "blue" : dayConfig.slotDuration === "60" ? "amber" : "green";

    const slotTagKey: SlotTagKey =
      slots.length === 0 ? "next" : dayConfig.slotDuration === "60" ? "popular" : "available";

    const sharedData = {
      dayLabel: dayConfig.label,
      dateLabel: formatDateLabel(date),
      durationLabel: `${dayConfig.slotDuration} min`,
      durationMinutes: Number(dayConfig.slotDuration) || 0,
      tagKey: slotTagKey,
      tagTone: slotTone,
    };

    slots.push({
      id: `${weekdayKey}-${dateKey}-start`,
      ...sharedData,
      timeLabel: formatTimeLabel(dayConfig.start),
    });

    if (slots.length < limit) {
      slots.push({
        id: `${weekdayKey}-${dateKey}-end`,
        ...sharedData,
        tagKey: slots.length === limit - 1 ? "limited" : "available",
        tagTone: slots.length === limit - 1 ? "blue" : "green",
        timeLabel: formatTimeLabel(dayConfig.end),
      });
    }
  }

  return slots.slice(0, limit);
}

function buildCheckoutLink(consultantId: string, slot: SlotCard) {
  const params = new URLSearchParams({
    consultantId,
    day: slot.dayLabel,
    date: slot.dateLabel,
    time: slot.timeLabel,
    duration: slot.durationLabel,
  });

  return `/dev/checkout?${params.toString()}`;
}

export function ConsultantDetail() {
  const { t } = useTranslation("consultants");
  const { id } = useParams();
  const [availability, setAvailability] = useState<MockAvailabilityState>(() =>
    getMockAvailability(),
  );

  useEffect(() => {
    return subscribeMockAvailability(() => {
      setAvailability(getMockAvailability());
    });
  }, []);

  const consultant = useMemo(
    () => consultants.find((item) => item.id === (id ?? "1")) ?? consultants[0],
    [id],
  );

  const generatedSlots = useMemo(() => buildDynamicSlots(availability), [availability]);

  const slots = useMemo(() => {
    if (consultant.id === "1") {
      return generatedSlots;
    }

    return consultant.staticSlots ?? [];
  }, [consultant, generatedSlots]);

  const primarySlot = slots[0];

  const availabilityBadgeKey = useMemo<BadgeKey>(() => {
    if (consultant.id === "1") {
      return getDynamicBadge(availability);
    }

    return consultant.badgeKey;
  }, [consultant, availability]);

  const durationMinutes =
    consultant.id === "1" && primarySlot
      ? primarySlot.durationMinutes
      : consultant.baseDurationMinutes;

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">{t("moduleTag")}</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            {t("detail.title")}
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">{t("detail.subtitle")}</p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
            <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
              <div>
                <p className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  {consultant.name}
                </p>
                <p className="mt-2 text-sm leading-6 text-[#4b5563]">
                  {t(`profiles.${consultant.id}.expertise`)}
                </p>

                <div className="mt-4 flex flex-wrap gap-2">
                  {consultant.tags.map((tag) => (
                    <span
                      key={tag.key}
                      className={`rounded-full px-3 py-1 text-xs font-medium ${tag.bg} ${tag.text}`}
                    >
                      {t(`tags.${tag.key}`)}
                    </span>
                  ))}
                </div>
              </div>

              <span className="rounded-full bg-[#f0fdf4] px-3 py-1 text-xs font-medium text-[#15803d]">
                {t(`badge.${availabilityBadgeKey}`)}
              </span>
            </div>

            <p className="mt-6 text-sm leading-7 text-[#4b5563]">
              {t(`profiles.${consultant.id}.bio`)}
            </p>

            <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-3">
              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.rating")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.rating}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.sessions")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.sessions}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  {t("detail.fee")}
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {t("detail.feeValue", {
                    fee: consultant.fee,
                    duration: t("duration.minutes", { count: durationMinutes }),
                  })}
                </p>
              </div>
            </div>
          </section>

          <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
            <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              {t("detail.bookingOverview")}
            </p>

            {primarySlot ? (
              <>
                <div className="mt-5 space-y-4">
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.nextSlot")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">
                      {primarySlot.dayKey ? t(`slotDay.${primarySlot.dayKey}`) : primarySlot.dayLabel}{" "}
                      · {primarySlot.timeLabel}
                    </p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.sessionFormat")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">{t("detail.sessionFormatValue")}</p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("detail.duration")}
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">
                      {t("duration.minutes", { count: primarySlot.durationMinutes })}
                    </p>
                  </div>
                </div>

                <div className="mt-6 flex flex-col gap-3">
                  <Link
                    to={buildCheckoutLink(consultant.id, primarySlot)}
                    className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    {t("detail.bookConsultant")}
                  </Link>

                  <Link
                    to="/dev/consultants"
                    className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    {t("detail.backToConsultants")}
                  </Link>
                </div>
              </>
            ) : (
              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
                <p className="text-sm leading-6 text-[#4b5563]">{t("detail.noSlotsBooking")}</p>
              </div>
            )}
          </aside>
        </div>

        <div className="mt-8 rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div>
              <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("detail.upcomingAvailability")}
              </h2>
              <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                {consultant.id === "1"
                  ? t("detail.upcomingLive")
                  : t("detail.upcomingStatic")}
              </p>
            </div>

            <span className="inline-flex rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
              {t("detail.openSlots", { count: slots.length })}
            </span>
          </div>

          {slots.length > 0 ? (
            <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              {slots.map((slot) => (
                <article
                  key={slot.id}
                  className="rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                        {slot.dayKey ? t(`slotDay.${slot.dayKey}`) : slot.dayLabel}
                      </p>
                      <p className="mt-1 text-sm leading-6 text-[#4b5563]">{slot.dateLabel}</p>
                    </div>

                    <span
                      className={`rounded-full px-3 py-1 text-xs font-medium ${getToneClasses(
                        slot.tagTone,
                      )}`}
                    >
                      {t(`slotTag.${slot.tagKey}`)}
                    </span>
                  </div>

                  <div className="mt-5 grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("detail.slotTime")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{slot.timeLabel}</p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        {t("detail.slotDuration")}
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {t("duration.minutes", { count: slot.durationMinutes })}
                      </p>
                    </div>
                  </div>

                  <Link
                    to={buildCheckoutLink(consultant.id, slot)}
                    className="mt-5 inline-flex h-11 w-full items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    {t("detail.selectSlot")}
                  </Link>
                </article>
              ))}
            </div>
          ) : (
            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
              <p className="text-sm leading-6 text-[#4b5563]">{t("detail.noUpcomingSlots")}</p>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
