import { apiClient } from "@/services/api/client";
import type { PaymentStatus } from "@/services/api/payments";

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
  paymentStatus?: PaymentStatus | null;
  refundedAmountCents?: number | null;
  refundReason?: string | null;
  isNoShowStudent: boolean;
  isNoShowConsultant: boolean;
  noShowMarkedAt?: string | null;
  /**
   * Optional free-text note the student attached at booking time — surfaced to
   * the consultant on their details page so they can prep. Null when none was
   * left.
   */
  studentNotes?: string | null;
  /**
   * True when the student has already submitted a rating for this booking. The
   * student-side UI uses it to hide the "Rate consultant" CTA after refresh so
   * a duplicate submission cannot be attempted.
   */
  hasStudentReview: boolean;
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

/** Result of `POST /api/consultants/{id}/book` (`RequestBookingResult`). */
export interface RequestBookingResult {
  bookingId: string;
  /**
   * True for free consultations (consultant fee = 0) — no Stripe widget,
   * the booking is straight-into Requested awaiting consultant accept/reject.
   */
  isFree: boolean;
  /** Stripe client secret of the booking's PaymentIntent — confirm THIS one. */
  clientSecret: string | null;
  paymentIntentId: string | null;
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

  /** All bookings platform-wide — admin / company only. */
  async getAll(): Promise<BookingListItem[]> {
    const { data } = await apiClient.get<BookingListItem[]>("/api/bookings");
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
  async requestBooking(input: RequestBookingInput): Promise<RequestBookingResult> {
    const { data } = await apiClient.post<RequestBookingResult>(
      `/api/consultants/${input.consultantId}/book`,
      input,
    );
    return data;
  },

  /** Consultant accepts a requested booking — the video room is auto-provisioned. */
  async accept(id: string): Promise<void> {
    await apiClient.post(`/api/bookings/${id}/accept`, { bookingId: id });
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
