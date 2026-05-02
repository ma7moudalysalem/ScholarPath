import {
  getMockAvailability,
  subscribeMockAvailability,
  type MockAvailabilityState,
} from "@/lib/mockAvailabilityStore";
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";

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

type ConsultantCard = {
  id: string;
  name: string;
  expertise: string;
  description: string;
  rating: string;
  ratingValue: number;
  sessions: string;
  fee: string;
  feeValue: number;
  duration: string;
  durationValue: number;
  expertiseKeys: string[];
  tags: Array<{
    label: string;
    bg: string;
    text: string;
  }>;
  staticBadge?: {
    label: string;
    className: string;
  };
  staticSlots?: SlotCard[];
};

type BrowseFilter = "all" | "live" | "availableToday" | "unavailable";

type AvailabilitySelectFilter = "all" | "live" | "availableToday" | "unavailable";
type ExpertiseSelectFilter =
  | "all"
  | "scholarship-strategy"
  | "visa-support"
  | "application-review"
  | "interview-prep"
  | "funding-plans";
type PriceSelectFilter = "any" | "under30" | "30to35" | "above35";
type RatingSelectFilter = "any" | "4plus" | "4_5plus" | "4_8plus";

type SearchFormState = {
  query: string;
  expertise: ExpertiseSelectFilter;
  price: PriceSelectFilter;
  rating: RatingSelectFilter;
  availability: AvailabilitySelectFilter;
};

type BrowseConsultant = ConsultantCard & {
  badge: {
    label: string;
    className: string;
  };
  slots: SlotCard[];
  nextSlot: string | null;
  isLive: boolean;
  isAvailableToday: boolean;
  hasAvailability: boolean;
};

const defaultSearchForm: SearchFormState = {
  query: "",
  expertise: "all",
  price: "any",
  rating: "any",
  availability: "all",
};

const consultants: ConsultantCard[] = [
  {
    id: "1",
    name: "Dr. Sarah Adel",
    expertise: "Scholarship Strategy · Personal Statements · Interview Preparation",
    description:
      "Supports students through shortlist strategy, essay refinement, and application readiness for international scholarship opportunities.",
    rating: "4.9 / 5",
    ratingValue: 4.9,
    sessions: "124",
    fee: "$35",
    feeValue: 35,
    duration: "45 min",
    durationValue: 45,
    expertiseKeys: ["scholarship-strategy", "application-review", "interview-prep"],
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
    description:
      "Helps students plan study-abroad pathways, narrow target universities, and prepare documents for visa and scholarship-fit decisions.",
    rating: "4.7 / 5",
    ratingValue: 4.7,
    sessions: "89",
    fee: "$25",
    feeValue: 25,
    duration: "30 min",
    durationValue: 30,
    expertiseKeys: ["visa-support", "funding-plans"],
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
    staticBadge: {
      label: "This week",
      className: "bg-[#fffbeb] text-[#b45309]",
    },
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
    ],
  },
  {
    id: "3",
    name: "Nour Elhassan",
    expertise: "Full Scholarship Planning · Application Review · Deadline Strategy",
    description:
      "Focuses on full scholarship planning, structured application review, and helping students improve readiness for competitive funding opportunities.",
    rating: "4.8 / 5",
    ratingValue: 4.8,
    sessions: "102",
    fee: "$40",
    feeValue: 40,
    duration: "60 min",
    durationValue: 60,
    expertiseKeys: ["funding-plans", "application-review", "scholarship-strategy"],
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
    staticBadge: {
      label: "Weekend",
      className: "bg-[#eff6ff] text-[#1d4ed8]",
    },
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

function buildDynamicBadge(
  availability: MockAvailabilityState,
  slots: SlotCard[],
): {
  label: string;
  className: string;
  isLive: boolean;
  isAvailableToday: boolean;
  hasAvailability: boolean;
} {
  const today = new Date();
  const weekdayKey = weekdayKeyByIndex[today.getDay()];
  const todayKey = formatDateKey(today);

  const blockedSet = new Set(
    availability.blockedDates.filter((item) => item.date).map((item) => item.date),
  );

  const todayConfig = availability.days.find((day) => day.key === weekdayKey);
  const hasEnabledDay = availability.days.some((day) => day.isEnabled);
  const hasAvailability = slots.length > 0;
  const isAvailableToday = Boolean(
    todayConfig?.isEnabled && !blockedSet.has(todayKey) && hasAvailability,
  );

  if (isAvailableToday) {
    return {
      label: "Available today",
      className: "bg-[#f0fdf4] text-[#15803d]",
      isLive: true,
      isAvailableToday: true,
      hasAvailability: true,
    };
  }

  if (hasEnabledDay && hasAvailability) {
    return {
      label: "Live schedule",
      className: "bg-[#f0fdf4] text-[#15803d]",
      isLive: true,
      isAvailableToday: false,
      hasAvailability: true,
    };
  }

  return {
    label: "No availability",
    className: "bg-[#f3f4f6] text-[#4b5563]",
    isLive: false,
    isAvailableToday: false,
    hasAvailability: false,
  };
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
    if (blockedSet.has(dateKey)) continue;

    const weekdayKey = weekdayKeyByIndex[date.getDay()];
    const dayConfig = enabledDays.get(weekdayKey);
    if (!dayConfig) continue;

    const slotTone: Tone =
      slots.length === 0 ? "blue" : dayConfig.slotDuration === "60" ? "amber" : "green";

    const slotLabel =
      slots.length === 0 ? "Next" : dayConfig.slotDuration === "60" ? "Popular" : "Available";

    slots.push({
      id: `${weekdayKey}-${dateKey}-${slots.length}`,
      dayLabel: dayConfig.label,
      dateLabel: formatDateLabel(date),
      timeLabel: formatTimeLabel(dayConfig.start),
      durationLabel: `${dayConfig.slotDuration} min`,
      tagLabel: slotLabel,
      tagTone: slotTone,
    });
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

function buildNextSlotLabel(slot?: SlotCard) {
  if (!slot) return "No open slots";
  return `${slot.dayLabel} · ${slot.timeLabel}`;
}

function matchesPriceFilter(consultant: BrowseConsultant, value: PriceSelectFilter) {
  if (value === "any") return true;
  if (value === "under30") return consultant.feeValue < 30;
  if (value === "30to35") return consultant.feeValue >= 30 && consultant.feeValue <= 35;
  return consultant.feeValue > 35;
}

function matchesRatingFilter(consultant: BrowseConsultant, value: RatingSelectFilter) {
  if (value === "any") return true;
  if (value === "4plus") return consultant.ratingValue >= 4;
  if (value === "4_5plus") return consultant.ratingValue >= 4.5;
  return consultant.ratingValue >= 4.8;
}

function matchesAvailabilitySelect(consultant: BrowseConsultant, value: AvailabilitySelectFilter) {
  if (value === "all") return true;
  if (value === "live") return consultant.isLive;
  if (value === "availableToday") return consultant.isAvailableToday;
  return !consultant.hasAvailability;
}

export function ConsultantsBrowse() {
  const [availability, setAvailability] = useState<MockAvailabilityState>(() =>
    getMockAvailability(),
  );
  const [quickFilter, setQuickFilter] = useState<BrowseFilter>("all");
  const [searchForm, setSearchForm] = useState<SearchFormState>(defaultSearchForm);
  const [appliedSearch, setAppliedSearch] = useState<SearchFormState>(defaultSearchForm);

  useEffect(() => {
    setAvailability(getMockAvailability());

    return subscribeMockAvailability(() => {
      setAvailability(getMockAvailability());
    });
  }, []);

  const consultantOneSlots = useMemo(() => buildDynamicSlots(availability, 6), [availability]);

  const consultantOneBadge = useMemo(
    () => buildDynamicBadge(availability, consultantOneSlots),
    [availability, consultantOneSlots],
  );

  const consultantCards = useMemo<BrowseConsultant[]>(() => {
    return consultants.map((consultant) => {
      if (consultant.id === "1") {
        return {
          ...consultant,
          badge: {
            label: consultantOneBadge.label,
            className: consultantOneBadge.className,
          },
          slots: consultantOneSlots,
          nextSlot: buildNextSlotLabel(consultantOneSlots[0]),
          isLive: consultantOneBadge.isLive,
          isAvailableToday: consultantOneBadge.isAvailableToday,
          hasAvailability: consultantOneBadge.hasAvailability,
        };
      }

      const slots = consultant.staticSlots ?? [];

      return {
        ...consultant,
        badge: consultant.staticBadge ?? {
          label: "Available",
          className: "bg-[#f0fdf4] text-[#15803d]",
        },
        slots,
        nextSlot: buildNextSlotLabel(slots[0]),
        isLive: true,
        isAvailableToday: false,
        hasAvailability: slots.length > 0,
      };
    });
  }, [consultantOneBadge, consultantOneSlots]);

  const searchedConsultants = useMemo(() => {
    const normalizedQuery = appliedSearch.query.trim().toLowerCase();

    return consultantCards.filter((consultant) => {
      const searchableText = [
        consultant.name,
        consultant.expertise,
        consultant.description,
        ...consultant.tags.map((tag) => tag.label),
      ]
        .join(" ")
        .toLowerCase();

      const queryMatch = normalizedQuery.length === 0 || searchableText.includes(normalizedQuery);

      const expertiseMatch =
        appliedSearch.expertise === "all" ||
        consultant.expertiseKeys.includes(appliedSearch.expertise);

      const priceMatch = matchesPriceFilter(consultant, appliedSearch.price);
      const ratingMatch = matchesRatingFilter(consultant, appliedSearch.rating);
      const availabilityMatch = matchesAvailabilitySelect(consultant, appliedSearch.availability);

      return queryMatch && expertiseMatch && priceMatch && ratingMatch && availabilityMatch;
    });
  }, [appliedSearch, consultantCards]);

  const filteredConsultants = useMemo(() => {
    return searchedConsultants.filter((consultant) => {
      switch (quickFilter) {
        case "live":
          return consultant.isLive;
        case "availableToday":
          return consultant.isAvailableToday;
        case "unavailable":
          return !consultant.hasAvailability;
        default:
          return true;
      }
    });
  }, [quickFilter, searchedConsultants]);

  const totalConsultants = consultantCards.length;
  const liveCount = consultantCards.filter((consultant) => consultant.isLive).length;
  const availableTodayCount = consultantCards.filter(
    (consultant) => consultant.isAvailableToday,
  ).length;
  const unavailableCount = consultantCards.filter(
    (consultant) => !consultant.hasAvailability,
  ).length;

  const handleSearchSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setAppliedSearch(searchForm);
  };

  const handleResetFilters = () => {
    setSearchForm(defaultSearchForm);
    setAppliedSearch(defaultSearchForm);
    setQuickFilter("all");
  };

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
            Browse consultants
          </h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            Explore consultant profiles, compare expertise areas, and review availability before
            selecting a session.
          </p>
        </div>

        <form
          onSubmit={handleSearchSubmit}
          className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm"
        >
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-12">
            <div className="xl:col-span-5">
              <label
                htmlFor="consultant-search"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
              >
                Search
              </label>
              <input
                id="consultant-search"
                type="text"
                value={searchForm.query}
                onChange={(event) =>
                  setSearchForm((current) => ({ ...current, query: event.target.value }))
                }
                placeholder="Search by consultant name, expertise, or keyword"
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none placeholder:text-[#9ca3af] focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              />
            </div>

            <div className="xl:col-span-2">
              <label
                htmlFor="expertise-filter"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
              >
                Expertise
              </label>
              <select
                id="expertise-filter"
                value={searchForm.expertise}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    expertise: event.target.value as ExpertiseSelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              >
                <option value="all">All</option>
                <option value="scholarship-strategy">Scholarship Strategy</option>
                <option value="visa-support">Visa Support</option>
                <option value="application-review">Application Review</option>
                <option value="interview-prep">Interview Prep</option>
                <option value="funding-plans">Funding Plans</option>
              </select>
            </div>

            <div className="xl:col-span-2">
              <label
                htmlFor="price-filter"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
              >
                Price
              </label>
              <select
                id="price-filter"
                value={searchForm.price}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    price: event.target.value as PriceSelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              >
                <option value="any">Any</option>
                <option value="under30">Under $30</option>
                <option value="30to35">$30 - $35</option>
                <option value="above35">Above $35</option>
              </select>
            </div>

            <div className="xl:col-span-1">
              <label
                htmlFor="rating-filter"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
              >
                Rating
              </label>
              <select
                id="rating-filter"
                value={searchForm.rating}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    rating: event.target.value as RatingSelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              >
                <option value="any">Any</option>
                <option value="4plus">4.0+</option>
                <option value="4_5plus">4.5+</option>
                <option value="4_8plus">4.8+</option>
              </select>
            </div>

            <div className="xl:col-span-2">
              <label
                htmlFor="availability-filter"
                className="mb-2 block text-xs font-medium text-[#6b7280]"
              >
                Availability
              </label>
              <select
                id="availability-filter"
                value={searchForm.availability}
                onChange={(event) =>
                  setSearchForm((current) => ({
                    ...current,
                    availability: event.target.value as AvailabilitySelectFilter,
                  }))
                }
                className="h-12 w-full rounded-xl border border-[#d1d5db] bg-white px-4 text-sm text-[#1d1d1f] transition outline-none focus:border-[#93c5fd] focus:ring-2 focus:ring-[#dbeafe]"
              >
                <option value="all">All</option>
                <option value="live">Live schedule</option>
                <option value="availableToday">Available today</option>
                <option value="unavailable">No availability</option>
              </select>
            </div>
          </div>

          <div className="mt-5 flex flex-wrap gap-3">
            <button
              type="submit"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
            >
              Search consultants
            </button>

            <button
              type="button"
              onClick={handleResetFilters}
              className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
            >
              Reset filters
            </button>
          </div>
        </form>

        <div className="mt-6 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => setQuickFilter("all")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  quickFilter === "all"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                All consultants
              </button>

              <button
                type="button"
                onClick={() => setQuickFilter("live")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  quickFilter === "live"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Live schedule
              </button>

              <button
                type="button"
                onClick={() => setQuickFilter("availableToday")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  quickFilter === "availableToday"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Available today
              </button>

              <button
                type="button"
                onClick={() => setQuickFilter("unavailable")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  quickFilter === "unavailable"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                No availability
              </button>
            </div>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Total
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{totalConsultants}</p>
              </div>

              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Live
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{liveCount}</p>
              </div>

              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Today
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{availableTodayCount}</p>
              </div>

              <div className="rounded-xl bg-[#f9fafb] px-4 py-3">
                <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                  Unavailable
                </p>
                <p className="mt-1 text-lg font-semibold text-[#1d1d1f]">{unavailableCount}</p>
              </div>
            </div>
          </div>
        </div>

        <div className="mt-8 flex items-end justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
              Available consultants
            </h2>
            <p className="mt-2 text-sm leading-6 text-[#4b5563]">
              Explore verified profiles and choose the consultant that best matches your needs.
            </p>
          </div>

          <p className="shrink-0 text-sm font-medium text-[#2563eb]">
            {filteredConsultants.length} result{filteredConsultants.length === 1 ? "" : "s"}
          </p>
        </div>

        <div className="mt-6 grid gap-6 lg:grid-cols-2 xl:grid-cols-3">
          {filteredConsultants.length > 0 ? (
            filteredConsultants.map((consultant) => (
              <article
                key={consultant.id}
                className="flex h-full min-h-[930px] flex-col rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                      {consultant.name}
                    </h3>
                    <p className="mt-2 text-sm leading-6 text-[#4b5563]">{consultant.expertise}</p>
                  </div>

                  <span
                    className={`shrink-0 rounded-full px-3 py-1 text-xs font-medium ${consultant.badge.className}`}
                  >
                    {consultant.badge.label}
                  </span>
                </div>

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

                <p className="mt-5 text-sm leading-7 text-[#4b5563]">{consultant.description}</p>

                <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-4 sm:grid-cols-2">
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
                    <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.fee}</p>
                  </div>

                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      Base duration
                    </p>
                    <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{consultant.duration}</p>
                  </div>
                </div>

                <div className="mt-6 flex min-h-0 flex-1 flex-col rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
                  <div className="flex items-center justify-between gap-3">
                    <h4 className="text-base font-semibold text-[#1d1d1f]">Quick availability</h4>
                    <span className="rounded-full bg-[#eff6ff] px-3 py-1 text-xs font-medium text-[#1d4ed8]">
                      {consultant.slots.length} open slot{consultant.slots.length === 1 ? "" : "s"}
                    </span>
                  </div>

                  <p className="mt-3 text-sm text-[#4b5563]">
                    Next slot:{" "}
                    <span className="font-medium text-[#1d1d1f]">{consultant.nextSlot}</span>
                  </p>

                  {consultant.slots.length > 0 ? (
                    <div className="mt-4 min-h-0 flex-1 overflow-hidden rounded-lg">
                      <div className="scrollbar-soft max-h-[172px] space-y-3 overflow-y-auto pr-1">
                        {consultant.slots.map((slot) => (
                          <Link
                            key={slot.id}
                            to={buildCheckoutLink(consultant.id, slot)}
                            className="block rounded-lg border border-[#e5e7eb] bg-white p-4 transition hover:border-[#bfdbfe] hover:bg-[#f8fbff] hover:shadow-sm"
                          >
                            <div className="flex items-start justify-between gap-3">
                              <div>
                                <p className="text-sm font-medium text-[#1d1d1f]">
                                  {slot.dayLabel} · {slot.timeLabel}
                                </p>
                                <p className="mt-1 text-sm text-[#4b5563]">{slot.dateLabel}</p>
                              </div>

                              <span
                                className={`rounded-full px-3 py-1 text-xs font-medium ${getToneClasses(
                                  slot.tagTone,
                                )}`}
                              >
                                {slot.tagLabel}
                              </span>
                            </div>

                            <div className="mt-3 flex items-center justify-between">
                              <p className="text-sm text-[#4b5563]">{slot.durationLabel}</p>
                              <span className="text-sm font-medium text-[#2563eb]">
                                Select slot
                              </span>
                            </div>
                          </Link>
                        ))}
                      </div>
                    </div>
                  ) : (
                    <div className="mt-4 rounded-lg border border-[#e5e7eb] bg-white p-4">
                      <p className="text-sm leading-6 text-[#4b5563]">
                        No slots are visible right now for this consultant.
                      </p>
                    </div>
                  )}

                  <div className="mt-4 flex flex-col gap-3">
                    <Link
                      to={`/dev/consultants/${consultant.id}`}
                      className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                    >
                      View consultant
                    </Link>

                    {consultant.id === "1" ? (
                      <Link
                        to="/dev/consultant/availability"
                        className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                      >
                        Open availability source
                      </Link>
                    ) : consultant.slots[0] ? (
                      <Link
                        to={buildCheckoutLink(consultant.id, consultant.slots[0])}
                        className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                      >
                        Book this consultant
                      </Link>
                    ) : (
                      <span className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#d1d5db] bg-transparent px-5 text-sm font-medium text-[#9ca3af]">
                        No slots available
                      </span>
                    )}
                  </div>
                </div>
              </article>
            ))
          ) : (
            <div className="lg:col-span-2 xl:col-span-3">
              <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
                <h3 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  No consultants matched your search
                </h3>
                <p className="mt-3 max-w-2xl text-sm leading-7 text-[#4b5563]">
                  Try broadening the keyword, changing the filter values, or reset the filters to
                  view all available consultants again.
                </p>

                <div className="mt-6">
                  <button
                    type="button"
                    onClick={handleResetFilters}
                    className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    Reset filters
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
