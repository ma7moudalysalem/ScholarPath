import {
  getMockAvailability,
  subscribeMockAvailability,
  type MockAvailabilityState,
} from "@/lib/mockAvailabilityStore";
import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router";

type Tone = "green" | "blue" | "amber" | "gray";

type SlotCard = {
  id: string;
  dayLabel: string;
  dateLabel: string;
  timeLabel: string;
  durationLabel: string;
  tagLabel: string;
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
  badge: string;
  tags: Array<{
    label: string;
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
    badge: "Live schedule",
    tags: [
      {
        label: "UK Admissions",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        label: "Essays",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        label: "Interview Prep",
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
    badge: "This week",
    tags: [
      {
        label: "Visa Support",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        label: "University Choice",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        label: "Funding Plans",
        bg: "bg-[#fffbeb]",
        text: "text-[#b45309]",
      },
    ],
    staticSlots: [
      {
        id: "2-slot-1",
        dayLabel: "Tomorrow",
        dateLabel: "26 Apr 2026",
        timeLabel: "4:00 PM",
        durationLabel: "30 min",
        tagLabel: "Available",
        tagTone: "green",
      },
      {
        id: "2-slot-2",
        dayLabel: "Saturday",
        dateLabel: "27 Apr 2026",
        timeLabel: "12:30 PM",
        durationLabel: "30 min",
        tagLabel: "Popular",
        tagTone: "amber",
      },
      {
        id: "2-slot-3",
        dayLabel: "Sunday",
        dateLabel: "28 Apr 2026",
        timeLabel: "5:00 PM",
        durationLabel: "30 min",
        tagLabel: "Available",
        tagTone: "green",
      },
      {
        id: "2-slot-4",
        dayLabel: "Monday",
        dateLabel: "29 Apr 2026",
        timeLabel: "6:00 PM",
        durationLabel: "30 min",
        tagLabel: "Available",
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
    badge: "Weekend",
    tags: [
      {
        label: "Full Funding",
        bg: "bg-[#eef2ff]",
        text: "text-[#4338ca]",
      },
      {
        label: "Application Review",
        bg: "bg-[#eff6ff]",
        text: "text-[#1d4ed8]",
      },
      {
        label: "Planning",
        bg: "bg-[#fffbeb]",
        text: "text-[#b45309]",
      },
    ],
    staticSlots: [
      {
        id: "3-slot-1",
        dayLabel: "Saturday",
        dateLabel: "27 Apr 2026",
        timeLabel: "11:00 AM",
        durationLabel: "60 min",
        tagLabel: "Popular",
        tagTone: "amber",
      },
      {
        id: "3-slot-2",
        dayLabel: "Sunday",
        dateLabel: "28 Apr 2026",
        timeLabel: "2:00 PM",
        durationLabel: "60 min",
        tagLabel: "Available",
        tagTone: "green",
      },
      {
        id: "3-slot-3",
        dayLabel: "Monday",
        dateLabel: "29 Apr 2026",
        timeLabel: "8:00 PM",
        durationLabel: "60 min",
        tagLabel: "Available",
        tagTone: "green",
      },
      {
        id: "3-slot-4",
        dayLabel: "Tuesday",
        dateLabel: "30 Apr 2026",
        timeLabel: "5:30 PM",
        durationLabel: "60 min",
        tagLabel: "Limited",
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

function getDynamicBadge(availability: MockAvailabilityState) {
  const today = new Date();
  const weekdayKey = weekdayKeyByIndex[today.getDay()];
  const todayKey = formatDateKey(today);

  const blockedSet = new Set(
    availability.blockedDates.filter((item) => item.date).map((item) => item.date),
  );

  const todayConfig = availability.days.find((day) => day.key === weekdayKey);

  if (todayConfig?.isEnabled && !blockedSet.has(todayKey)) {
    return "Available today";
  }

  const hasEnabledDay = availability.days.some((day) => day.isEnabled);
  return hasEnabledDay ? "Live schedule" : "No availability";
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

    const slotLabel =
      slots.length === 0 ? "Next" : dayConfig.slotDuration === "60" ? "Popular" : "Available";

    const sharedData = {
      dayLabel: dayConfig.label,
      dateLabel: formatDateLabel(date),
      durationLabel: `${dayConfig.slotDuration} min`,
      tagLabel: slotLabel,
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
        tagLabel: slots.length === limit - 1 ? "Limited" : "Available",
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
  const { id } = useParams();
  const [availability, setAvailability] = useState<MockAvailabilityState>(() =>
    getMockAvailability(),
  );

  useEffect(() => {
    setAvailability(getMockAvailability());

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

  const availabilityBadge = useMemo(() => {
    if (consultant.id === "1") {
      return getDynamicBadge(availability);
    }

    return consultant.badge;
  }, [consultant, availability]);

  const durationDisplay =
    consultant.id === "1" && primarySlot ? primarySlot.durationLabel : consultant.baseDuration;

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            Consultant profile
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            Review the consultant&apos;s expertise, credentials, ratings, fee, and upcoming
            availability before booking.
          </p>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-8">
            <div className="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
              <div>
                <p className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  {consultant.name}
                </p>
                <p className="mt-2 text-sm leading-6 text-[#4b5563]">{consultant.expertise}</p>

                <div className="mt-4 flex flex-wrap gap-2">
                  {consultant.tags.map((tag) => (
                    <span
                      key={tag.label}
                      className={`rounded-full px-3 py-1 text-xs font-medium ${tag.bg} ${tag.text}`}
                    >
                      {tag.label}
                    </span>
                  ))}
                </div>
              </div>

              <span className="rounded-full bg-[#f0fdf4] px-3 py-1 text-xs font-medium text-[#15803d]">
                {availabilityBadge}
              </span>
            </div>

            <p className="mt-6 text-sm leading-7 text-[#4b5563]">{consultant.bio}</p>

            <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-3">
              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Rating
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.rating}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Sessions
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.sessions}</p>
              </div>

              <div>
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Fee
                </p>
                <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                  {consultant.fee} / {durationDisplay}
                </p>
              </div>
            </div>
          </section>

          <aside className="rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm lg:col-span-4">
            <p className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              Booking overview
            </p>

            {primarySlot ? (
              <>
                <div className="mt-5 space-y-4">
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      Next slot
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">
                      {primarySlot.dayLabel} · {primarySlot.timeLabel}
                    </p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      Session format
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">1:1 online consultation</p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      Duration
                    </p>
                    <p className="mt-1 text-sm text-[#1d1d1f]">{primarySlot.durationLabel}</p>
                  </div>
                </div>

                <div className="mt-6 flex flex-col gap-3">
                  <Link
                    to={buildCheckoutLink(consultant.id, primarySlot)}
                    className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    Book this consultant
                  </Link>

                  <Link
                    to="/dev/consultants"
                    className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    Back to consultants
                  </Link>
                </div>
              </>
            ) : (
              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
                <p className="text-sm leading-6 text-[#4b5563]">
                  No open slots are visible right now. Update consultant availability to make
                  sessions bookable again.
                </p>
              </div>
            )}
          </aside>
        </div>

        <div className="mt-8 rounded-xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div>
              <h2 className="text-[1.75rem] font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Upcoming availability
              </h2>
              <p className="mt-2 max-w-3xl text-sm leading-6 text-[#4b5563]">
                {consultant.id === "1"
                  ? "These slot cards now read directly from the consultant availability store."
                  : "These sample slot cards show upcoming booking options for this consultant."}
              </p>
            </div>

            <span className="inline-flex rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
              {slots.length} open slots
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
                        {slot.dayLabel}
                      </p>
                      <p className="mt-1 text-sm leading-6 text-[#4b5563]">{slot.dateLabel}</p>
                    </div>

                    <span
                      className={`rounded-full px-3 py-1 text-xs font-medium ${getToneClasses(
                        slot.tagTone,
                      )}`}
                    >
                      {slot.tagLabel}
                    </span>
                  </div>

                  <div className="mt-5 grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        Time
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{slot.timeLabel}</p>
                    </div>

                    <div>
                      <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                        Duration
                      </p>
                      <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                        {slot.durationLabel}
                      </p>
                    </div>
                  </div>

                  <Link
                    to={buildCheckoutLink(consultant.id, slot)}
                    className="mt-5 inline-flex h-11 w-full items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-4 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                  >
                    Select this slot
                  </Link>
                </article>
              ))}
            </div>
          ) : (
            <div className="mt-6 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
              <p className="text-sm leading-6 text-[#4b5563]">
                No upcoming slots are currently available for this consultant.
              </p>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
