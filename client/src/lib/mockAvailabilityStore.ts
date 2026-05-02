export type AvailabilityDayKey =
  | "monday"
  | "tuesday"
  | "wednesday"
  | "thursday"
  | "friday"
  | "saturday"
  | "sunday";

export type AvailabilityDayConfig = {
  key: AvailabilityDayKey;
  label: string;
  isEnabled: boolean;
  start: string;
  end: string;
  slotDuration: string;
  buffer: string;
};

export type BlockedDate = {
  id: string;
  date: string;
  reason: string;
};

export type MockAvailabilityState = {
  timezone: string;
  bookingWindow: string;
  days: AvailabilityDayConfig[];
  blockedDates: BlockedDate[];
};

const STORAGE_KEY = "scholarpath-mock-consultant-availability-v1";
const AVAILABILITY_EVENT = "scholarpath-mock-consultant-availability-updated";

const DEFAULT_AVAILABILITY: MockAvailabilityState = {
  timezone: "Africa/Cairo",
  bookingWindow: "21",
  days: [
    {
      key: "monday",
      label: "Monday",
      isEnabled: true,
      start: "16:00",
      end: "20:00",
      slotDuration: "45",
      buffer: "15",
    },
    {
      key: "tuesday",
      label: "Tuesday",
      isEnabled: true,
      start: "17:30",
      end: "21:00",
      slotDuration: "45",
      buffer: "15",
    },
    {
      key: "wednesday",
      label: "Wednesday",
      isEnabled: false,
      start: "16:00",
      end: "20:00",
      slotDuration: "45",
      buffer: "15",
    },
    {
      key: "thursday",
      label: "Thursday",
      isEnabled: true,
      start: "16:00",
      end: "19:00",
      slotDuration: "45",
      buffer: "15",
    },
    {
      key: "friday",
      label: "Friday",
      isEnabled: false,
      start: "12:00",
      end: "15:00",
      slotDuration: "45",
      buffer: "15",
    },
    {
      key: "saturday",
      label: "Saturday",
      isEnabled: true,
      start: "11:00",
      end: "15:00",
      slotDuration: "60",
      buffer: "15",
    },
    {
      key: "sunday",
      label: "Sunday",
      isEnabled: true,
      start: "13:00",
      end: "17:00",
      slotDuration: "45",
      buffer: "15",
    },
  ],
  blockedDates: [
    {
      id: "blocked-1",
      date: "2026-05-01",
      reason: "Public holiday",
    },
    {
      id: "blocked-2",
      date: "2026-05-10",
      reason: "Conference day",
    },
  ],
};

function canUseStorage() {
  return typeof window !== "undefined" && typeof window.localStorage !== "undefined";
}

function cloneDefaultAvailability(): MockAvailabilityState {
  return {
    timezone: DEFAULT_AVAILABILITY.timezone,
    bookingWindow: DEFAULT_AVAILABILITY.bookingWindow,
    days: DEFAULT_AVAILABILITY.days.map((day) => ({ ...day })),
    blockedDates: DEFAULT_AVAILABILITY.blockedDates.map((item) => ({ ...item })),
  };
}

function notifyAvailabilityUpdated() {
  if (!canUseStorage()) {
    return;
  }

  window.dispatchEvent(new CustomEvent(AVAILABILITY_EVENT));
}

export function getMockAvailability(): MockAvailabilityState {
  if (!canUseStorage()) {
    return cloneDefaultAvailability();
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return cloneDefaultAvailability();
    }

    const parsed = JSON.parse(raw) as MockAvailabilityState;

    if (!parsed || !Array.isArray(parsed.days) || !Array.isArray(parsed.blockedDates)) {
      return cloneDefaultAvailability();
    }

    return parsed;
  } catch {
    return cloneDefaultAvailability();
  }
}

export function saveMockAvailability(nextState: MockAvailabilityState) {
  if (!canUseStorage()) {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextState));
  notifyAvailabilityUpdated();
}

export function resetMockAvailability() {
  const resetState = cloneDefaultAvailability();
  saveMockAvailability(resetState);
  return resetState;
}

export function subscribeMockAvailability(listener: () => void) {
  if (!canUseStorage()) {
    return () => undefined;
  }

  const handleLocalUpdate = () => {
    listener();
  };

  const handleStorage = (event: StorageEvent) => {
    if (event.key === STORAGE_KEY) {
      listener();
    }
  };

  const handleVisibilityChange = () => {
    if (document.visibilityState === "visible") {
      listener();
    }
  };

  window.addEventListener(AVAILABILITY_EVENT, handleLocalUpdate);
  window.addEventListener("storage", handleStorage);
  window.addEventListener("focus", handleLocalUpdate);
  document.addEventListener("visibilitychange", handleVisibilityChange);

  return () => {
    window.removeEventListener(AVAILABILITY_EVENT, handleLocalUpdate);
    window.removeEventListener("storage", handleStorage);
    window.removeEventListener("focus", handleLocalUpdate);
    document.removeEventListener("visibilitychange", handleVisibilityChange);
  };
}
