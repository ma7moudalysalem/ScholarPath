import {
  getMockBookings,
  resetMockBookings,
  setMockBookingStatus,
  subscribeMockBookings,
  type BookingWorkflowStatus,
  type MockBookingRecord,
} from "@/lib/mockBookingStore";
import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router";

type TimelineStep = {
  title: string;
  description: string;
  isDone: boolean;
};

function getConsultantStatusMeta(status: BookingWorkflowStatus) {
  switch (status) {
    case "pending":
      return {
        label: "New request",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        summary:
          "This consultation request is waiting for your decision. The student has already completed the checkout and hold authorization step.",
        note: "You can now review the request and either accept or reject it. The payment remains on hold until a decision is made.",
      };
    case "confirmed":
      return {
        label: "Confirmed",
        badgeClassName: "bg-[#eff6ff] text-[#1d4ed8]",
        holdStatus: "Ready for capture",
        summary:
          "You accepted this request. The session is now confirmed and ready for consultation delivery.",
        note: "The consultation is scheduled. After delivery, this request can be marked as completed so payment capture is reflected.",
      };
    case "completed":
      return {
        label: "Completed",
        badgeClassName: "bg-[#f0fdf4] text-[#15803d]",
        holdStatus: "Payment captured",
        summary:
          "This consultation was completed successfully and the booking is now closed from the consultant side.",
        note: "The consultation session was delivered successfully and the booking moved into a completed state.",
      };
    case "rejected":
      return {
        label: "Rejected",
        badgeClassName: "bg-[#fef2f2] text-[#dc2626]",
        holdStatus: "Hold released",
        summary: "You rejected this request, so the consultation did not proceed.",
        note: "The booking is now closed and the authorization hold should be reversed automatically.",
      };
    case "cancelled":
      return {
        label: "Cancelled",
        badgeClassName: "bg-[#f3f4f6] text-[#4b5563]",
        holdStatus: "No active hold",
        summary:
          "This consultation request was cancelled before final completion and remains visible for history and audit purposes.",
        note: "No session is active for this request and no further consultant action is needed.",
      };
    default:
      return {
        label: "New request",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        summary: "This request is waiting for consultant review.",
        note: "Waiting for consultant action.",
      };
  }
}

function buildTimeline(status: BookingWorkflowStatus): TimelineStep[] {
  switch (status) {
    case "pending":
      return [
        {
          title: "Student submitted request",
          description: "The student selected a slot and completed the booking checkout flow.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The system created a payment hold before consultant review.",
          isDone: true,
        },
        {
          title: "Consultant decision pending",
          description: "This request still needs to be accepted or rejected.",
          isDone: false,
        },
        {
          title: "Session confirmation",
          description: "This step will complete after the consultant accepts the request.",
          isDone: false,
        },
      ];
    case "confirmed":
      return [
        {
          title: "Student submitted request",
          description: "The consultation request was created successfully.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The payment-hold step completed before consultant action.",
          isDone: true,
        },
        {
          title: "Consultant accepted request",
          description: "The request was approved and the session was confirmed.",
          isDone: true,
        },
        {
          title: "Session confirmed",
          description: "The booking is scheduled and ready for delivery.",
          isDone: true,
        },
      ];
    case "completed":
      return [
        {
          title: "Student submitted request",
          description: "The consultation request entered the workflow successfully.",
          isDone: true,
        },
        {
          title: "Consultant accepted request",
          description: "The booking was confirmed and reserved.",
          isDone: true,
        },
        {
          title: "Consultation delivered",
          description: "The session took place successfully.",
          isDone: true,
        },
        {
          title: "Booking completed",
          description: "The booking was closed after successful delivery.",
          isDone: true,
        },
      ];
    case "rejected":
      return [
        {
          title: "Student submitted request",
          description: "The student created the consultation request successfully.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The payment hold was created before consultant review.",
          isDone: true,
        },
        {
          title: "Consultant rejected request",
          description: "The request was declined and the consultation will not proceed.",
          isDone: true,
        },
        {
          title: "Booking closed",
          description: "The booking is closed and the hold should be released.",
          isDone: true,
        },
      ];
    case "cancelled":
      return [
        {
          title: "Booking request entered workflow",
          description: "The consultation request was created successfully.",
          isDone: true,
        },
        {
          title: "Cancellation recorded",
          description: "The booking was cancelled before final completion.",
          isDone: true,
        },
        {
          title: "Payment flow closed",
          description: "No active hold remains for this request.",
          isDone: true,
        },
        {
          title: "Booking archived",
          description: "The cancelled request remains visible in the consultant history.",
          isDone: true,
        },
      ];
    default:
      return [];
  }
}

export function ConsultantBookingDetails() {
  const { id } = useParams();
  const bookingId = id ?? "1";

  const [bookingsSnapshot, setBookingsSnapshot] = useState<MockBookingRecord[]>(() =>
    getMockBookings(),
  );

  const [bannerByBooking, setBannerByBooking] = useState<Record<string, string>>({});

  useEffect(() => {
    return subscribeMockBookings(() => {
      setBookingsSnapshot(getMockBookings());
    });
  }, []);

  const booking = useMemo(() => {
    return bookingsSnapshot.find((item) => item.id === bookingId) ?? bookingsSnapshot[0] ?? null;
  }, [bookingsSnapshot, bookingId]);

  const banner = bannerByBooking[bookingId] ?? "";

  const statusMeta = useMemo(
    () => getConsultantStatusMeta(booking?.status ?? "pending"),
    [booking?.status],
  );

  const timeline = useMemo(() => buildTimeline(booking?.status ?? "pending"), [booking?.status]);

  if (!booking) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
            <h1 className="text-2xl font-semibold text-[#1d1d1f]">Booking not found</h1>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">
              The selected booking could not be found in the current mock store.
            </p>
            <div className="mt-6">
              <Link
                to="/dev/consultant/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                Back to consultant bookings
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  const handleAccept = () => {
    if (booking.status !== "pending") return;

    setMockBookingStatus(booking.id, "confirmed");
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "Request accepted. The student and consultant views are now synced.",
    }));
  };

  const handleReject = () => {
    if (booking.status !== "pending") return;

    setMockBookingStatus(booking.id, "rejected");
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "Request rejected. The status update now appears across both views.",
    }));
  };

  const handleComplete = () => {
    if (booking.status !== "confirmed") return;

    setMockBookingStatus(booking.id, "completed");
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "Session marked as completed. The workflow is now closed successfully.",
    }));
  };

  const handleReset = () => {
    resetMockBookings();
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "Demo bookings were reset to their original mock states.",
    }));
  };

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
                Consultant request details
              </h1>

              <p className="mt-3 max-w-3xl text-base leading-7 text-[#4b5563]">
                Review the booking request, student context, payment-hold status, and consultant
                decision path.
              </p>
            </div>

            <span
              className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${statusMeta.badgeClassName}`}
            >
              {statusMeta.label}
            </span>
          </div>
        </div>

        {banner ? (
          <div className="mt-8 rounded-xl border border-[#bbf7d0] bg-[#f0fdf4] p-5">
            <p className="text-sm font-medium text-[#166534]">{banner}</p>
          </div>
        ) : null}

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Request summary
              </h2>

              <p className="mt-3 text-sm leading-7 text-[#4b5563]">{statusMeta.summary}</p>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Booking reference
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.reference}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Student name
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentName}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Student email
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentEmail}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Session topic
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.topic}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Session type
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.sessionType}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Student stage
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentStage}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Selected date
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.date}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Selected time
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.time}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Duration
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.duration}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Fee
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.fee}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    Hold status
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{statusMeta.holdStatus}</p>
                </div>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Consultant guidance
              </h2>

              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                <p className="text-sm leading-7 text-[#4b5563]">{statusMeta.note}</p>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Request timeline
              </h2>

              <div className="mt-5 space-y-4">
                {timeline.map((step, index) => (
                  <div key={step.title} className="flex gap-4">
                    <div className="flex flex-col items-center">
                      <div
                        className={[
                          "mt-1 h-3.5 w-3.5 rounded-full",
                          step.isDone ? "bg-[#2563eb]" : "bg-[#d1d5db]",
                        ].join(" ")}
                      />
                      {index < timeline.length - 1 ? (
                        <div className="mt-2 h-full min-h-[48px] w-px bg-[#e5e7eb]" />
                      ) : null}
                    </div>

                    <div className="pb-4">
                      <p className="text-sm font-medium text-[#1d1d1f]">{step.title}</p>
                      <p className="mt-1 text-sm leading-6 text-[#4b5563]">{step.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>

          <aside className="space-y-6 lg:col-span-4">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Consultant actions
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                <button
                  type="button"
                  onClick={handleAccept}
                  disabled={booking.status !== "pending"}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    booking.status === "pending"
                      ? "bg-[#2563eb] text-white hover:bg-[#1d4ed8]"
                      : "cursor-not-allowed bg-[#e5e7eb] text-[#9ca3af]",
                  ].join(" ")}
                >
                  Accept request
                </button>

                <button
                  type="button"
                  onClick={handleReject}
                  disabled={booking.status !== "pending"}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    booking.status === "pending"
                      ? "border border-[#dc2626] bg-white text-[#dc2626] hover:bg-[#fef2f2]"
                      : "cursor-not-allowed border border-[#e5e7eb] bg-white text-[#9ca3af]",
                  ].join(" ")}
                >
                  Reject request
                </button>

                <button
                  type="button"
                  onClick={handleComplete}
                  disabled={booking.status !== "confirmed"}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    booking.status === "confirmed"
                      ? "bg-[#16a34a] text-white hover:bg-[#15803d]"
                      : "cursor-not-allowed bg-[#e5e7eb] text-[#9ca3af]",
                  ].join(" ")}
                >
                  Mark as completed
                </button>

                <button
                  type="button"
                  onClick={handleReset}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  Reset demo bookings
                </button>

                <Link
                  to="/dev/consultant/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  Back to consultant bookings
                </Link>

                <Link
                  to={`/dev/bookings/${booking.id}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  Open student view
                </Link>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Decision help
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-[#4b5563]">
                <p>
                  Accept when the request fits your expertise and the slot is still appropriate for
                  your schedule.
                </p>
                <p>
                  Reject when the request cannot be handled or the selected slot is no longer
                  suitable.
                </p>
                <p>
                  After confirmation, mark the session as completed only when the consultation is
                  actually delivered.
                </p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
