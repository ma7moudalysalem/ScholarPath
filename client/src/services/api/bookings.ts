import { apiClient } from "@/services/api/client";

// ── Domain enums (camelCase JSON serialises these enums as their string name) ──

/** Server `BookingStatus` — the full consultant-booking workflow. */
export type BookingStatus =
  | "Requested"
  | "Confirmed"
  | "Rejected"
  | "Expired"
  | "Cancelled"
  | "Completed"
  | "NoShowStudent"
  | "NoShowConsultant";

/** Server `DayOfWeek` — .NET serialises this enum as the day name. */
export type DayOfWeek =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

/** Server `CancellationReason`. */
export type CancellationReason = string;

// ── View models (what the pages consume) ──────────────────────────────────────
//
// The booking DTOs are already flat and camelCase, so the view models mirror
// the wire shape almost 1:1 — the service layer exists mainly to keep a single
// typed seam between the API and the pages (mirrors `scholarships.ts`).

/** One row in a bookings list — `GET /api/bookings/me` / `/consultant`. */
export interface BookingListItem {
  id: string;
  studentId: string;
  studentName: string;
  studentEmail?: string | null;
  consultantId: string;
  consultantName: string;
  consultantPhotoUrl?: string | null;
  status: BookingStatus;
  scheduledStartAt: string;
  scheduledEndAt: string;
  durationMinutes: number;
  priceUsd: number;
  meetingUrl?: string | null;
  requestedAt?: string | null;
  confirmedAt?: string | null;
  createdAt: string;
}

/** Full single-booking detail — `GET /api/bookings/{id}`. */
export interface BookingDetail extends BookingListItem {
  studentPhotoUrl?: string | null;
  consultantEmail?: string | null;
  availabilityId?: string | null;
  rejectedAt?: string | null;
  expiredAt?: string | null;
  cancelledAt?: string | null;
  completedAt?: string | null;
  cancellationReason?: CancellationReason | null;
  cancelledByUserId?: string | null;
  paymentId?: string | null;
  stripePaymentIntentId?: string | null;
  isNoShowStudent: boolean;
  isNoShowConsultant: boolean;
  noShowMarkedAt?: string | null;
}

/** A single saved availability rule — `GET /api/bookings/me/availability`. */
export interface AvailabilityRule {
  id: string;
  consultantId: string;
  isRecurring: boolean;
  dayOfWeek?: DayOfWeek | null;
  startTime?: string | null;
  endTime?: string | null;
  specificStartAt?: string | null;
  specificEndAt?: string | null;
  timezone: string;
  isActive: boolean;
}

// ── Write request bodies (mirror the server command records) ──────────────────

/** Body of `POST /api/consultants/{id}/book` (`RequestBookingCommand`). */
export interface RequestBookingInput {
  consultantId: string;
  availabilityId?: string | null;
  scheduledStartAt: string;
  scheduledEndAt: string;
  timezone: string;
  notes?: string | null;
}

/** Body of `POST /api/bookings/{id}/reschedule` (`RescheduleBookingCommand`). */
export interface RescheduleBookingInput {
  availabilityId?: string | null;
  scheduledStartAt: string;
  scheduledEndAt: string;
}

/** One slot in a `PATCH /api/bookings/me/availability` body. */
export interface AvailabilityInput {
  isRecurring: boolean;
  dayOfWeek?: DayOfWeek | null;
  startTime?: string | null;
  endTime?: string | null;
  specificStartAt?: string | null;
  specificEndAt?: string | null;
  timezone: string;
  isActive: boolean;
}

/** Body of `POST /api/bookings/{id}/rating` (`SubmitConsultantRatingCommand`). */
export interface SubmitRatingInput {
  rating: number;
  comment?: string | null;
}

// ── API ───────────────────────────────────────────────────────────────────────

export const bookingsApi = {
  /** The authenticated student's consultant bookings, newest first. */
  async getMine(): Promise<BookingListItem[]> {
    const { data } = await apiClient.get<BookingListItem[]>("/api/bookings/me");
    return data;
  },

  /** The authenticated consultant's incoming bookings, newest first. */
  async getForConsultant(): Promise<BookingListItem[]> {
    const { data } = await apiClient.get<BookingListItem[]>("/api/bookings/consultant");
    return data;
  },

  /** One booking's full detail — student / consultant / admin only. */
  async getById(id: string): Promise<BookingDetail> {
    const { data } = await apiClient.get<BookingDetail>(`/api/bookings/${id}`);
    return data;
  },

  /** The authenticated consultant's own active availability rules. */
  async getMyAvailability(): Promise<AvailabilityRule[]> {
    const { data } = await apiClient.get<AvailabilityRule[]>("/api/bookings/me/availability");
    return data;
  },

  /**
   * Creates a booking request — `POST /api/consultants/{id}/book`. The route
   * consultant id must match the body, so the id is taken from `input`.
   */
  async requestBooking(input: RequestBookingInput): Promise<{ bookingId: string }> {
    const { data } = await apiClient.post<{ bookingId: string }>(
      `/api/consultants/${input.consultantId}/book`,
      input,
    );
    return data;
  },

  /** Consultant accepts a requested booking, supplying the meeting URL. */
  async accept(id: string, meetingUrl: string): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/accept`, { bookingId: id, meetingUrl });
  },

  /** Consultant rejects a requested booking. */
  async reject(id: string): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/reject`, { bookingId: id });
  },

  /** Cancels a requested or confirmed booking (student or consultant). */
  async cancel(id: string): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/cancel`, { bookingId: id });
  },

  /** FR-229 — reschedules a booking to a new slot; no new payment is taken. */
  async reschedule(id: string, input: RescheduleBookingInput): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/reschedule`, { bookingId: id, ...input });
  },

  /** Marks the no-show party on a confirmed booking. */
  async markNoShow(id: string): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/no-show`, { bookingId: id });
  },

  /** Replaces / merges the consultant's own availability rules. */
  async updateAvailability(
    slots: AvailabilityInput[],
    replaceExisting = true,
  ): Promise<void> {
    await apiClient.patch("/api/bookings/me/availability", { replaceExisting, slots });
  },

  /** Submits the student's rating for a completed booking. */
  async submitRating(id: string, input: SubmitRatingInput): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/rating`, { bookingId: id, ...input });
  },
};
