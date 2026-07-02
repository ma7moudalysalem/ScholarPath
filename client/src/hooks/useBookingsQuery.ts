import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/lib/queryClient";
import {
  bookingsApi,
  type AvailabilityInput,
  type AvailabilityRule,
  type BookingDetail,
  type BookingListItem,
  type RequestBookingInput,
  type RescheduleBookingInput,
  type SubmitRatingInput,
} from "@/services/api/bookings";

// ── Queries ───────────────────────────────────────────────────────────────────

/** The authenticated student's consultant bookings, newest first. */
export function useMyBookingsQuery() {
  return useQuery<BookingListItem[]>({
    queryKey: queryKeys.bookings.mine,
    queryFn: () => bookingsApi.getMine(),
    staleTime: 30_000,
  });
}

/** The authenticated consultant's incoming bookings, newest first. */
export function useConsultantBookingsQuery() {
  return useQuery<BookingListItem[]>({
    queryKey: queryKeys.bookings.consultant,
    queryFn: () => bookingsApi.getForConsultant(),
    staleTime: 30_000,
  });
}

/** All bookings platform-wide — admin only. */
export function useAllBookingsQuery() {
  return useQuery<BookingListItem[]>({
    queryKey: queryKeys.bookings.admin,
    queryFn: () => bookingsApi.getAll(),
    staleTime: 30_000,
  });
}

/** One booking's full detail. */
export function useBookingDetailQuery(id: string | undefined) {
  return useQuery<BookingDetail>({
    queryKey: queryKeys.bookings.detail(id ?? ""),
    queryFn: () => bookingsApi.getById(id ?? ""),
    enabled: !!id,
  });
}

/** The authenticated consultant's own active availability rules. */
export function useMyAvailabilityQuery() {
  return useQuery<AvailabilityRule[]>({
    queryKey: queryKeys.bookings.myAvailability,
    queryFn: () => bookingsApi.getMyAvailability(),
    staleTime: 30_000,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

/** Creates a booking request from the checkout flow. */
export function useRequestBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: RequestBookingInput) => bookingsApi.requestBooking(input),
    onSuccess: () => {
      // A new booking touches both bookings lists and consultant availability.
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.consultants.all });
    },
  });
}

/** Consultant accepts a requested booking — the video room is auto-provisioned. */
export function useAcceptBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bookingsApi.accept(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
    },
  });
}

/** Consultant rejects a requested booking. */
export function useRejectBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bookingsApi.reject(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
    },
  });
}

/** Cancels a requested or confirmed booking. */
export function useCancelBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bookingsApi.cancel(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.consultants.all });
    },
  });
}

/** Reschedules a booking to a new slot (FR-229). */
export function useRescheduleBookingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: RescheduleBookingInput }) =>
      bookingsApi.reschedule(id, input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
      void queryClient.invalidateQueries({ queryKey: queryKeys.consultants.all });
    },
  });
}

/** Marks the no-show party on a confirmed booking. */
export function useMarkNoShowMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => bookingsApi.markNoShow(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.all });
    },
  });
}

/** Replaces / merges the consultant's own availability rules. */
export function useUpdateAvailabilityMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      slots,
      replaceExisting,
    }: {
      slots: AvailabilityInput[];
      replaceExisting?: boolean;
    }) => bookingsApi.updateAvailability(slots, replaceExisting),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.myAvailability });
      // Availability changes change the consultant's public bookable slots.
      void queryClient.invalidateQueries({ queryKey: queryKeys.consultants.all });
    },
  });
}

/** Submits the student's rating for a completed booking. */
export function useSubmitRatingMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: SubmitRatingInput }) =>
      bookingsApi.submitRating(id, input),
    onSuccess: (_data, { id }) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookings.detail(id) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.consultants.all });
    },
  });
}
