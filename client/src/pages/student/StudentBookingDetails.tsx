import {
  getMockBookings,
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

function getStudentStatusMeta(status: BookingWorkflowStatus) {
  switch (status) {
    case "pending":
      return {
        label: "Pending consultant response",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        summary:
          "Your booking request was created successfully and is now waiting for the consultant to review it.",
        note: "The payment authorization hold is active, but the amount is not captured until the consultant accepts the session.",
      };
    case "confirmed":
      return {
        label: "Accepted",
        badgeClassName: "bg-[#eff6ff] text-[#1d4ed8]",
        holdStatus: "Ready for capture",
        summary:
          "The consultant accepted your request. Your consultation is now confirmed at the selected date and time.",
        note: "Your payment hold remains valid and will move to capture after the consultation is delivered.",
      };
    case "completed":
      return {
        label: "Completed",
        badgeClassName: "bg-[#f0fdf4] text-[#15803d]",
        holdStatus: "Payment captured",
        summary:
          "The consultation was completed successfully and the booking is now closed from the student side.",
        note: "The payment was captured after successful delivery of the consultation session.",
      };
    case "rejected":
      return {
        label: "Rejected",
        badgeClassName: "bg-[#fef2f2] text-[#dc2626]",
        holdStatus: "Hold released",
        summary: "The consultant rejected this request, so the booking will not proceed.",
        note: "The authorization hold should be reversed and no consultation session is scheduled for this request.",
      };
    case "cancelled":
      return {
        label: "Cancelled",
        badgeClassName: "bg-[#f3f4f6] text-[#4b5563]",
        holdStatus: "No active hold",
        summary:
          "This booking was cancelled before final completion and remains visible in your booking history.",
        note: "No consultation session is active for this request and no payment capture should remain.",
      };
    default:
      return {
        label: "Pending consultant response",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        summary: "Your booking is waiting for consultant review.",
        note: "The system is waiting for the next booking workflow step.",
      };
  }
}

function buildTimeline(status: BookingWorkflowStatus): TimelineStep[] {
  switch (status) {
    case "pending":
      return [
        {
          title: "Booking request created",
          description: "You selected a consultation slot and completed the checkout flow.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The system placed a temporary payment hold for the consultation fee.",
          isDone: true,
        },
        {
          title: "Consultant review pending",
          description: "The consultant still needs to accept or reject your request.",
          isDone: false,
        },
        {
          title: "Session confirmation",
          description: "This step will complete after the consultant accepts your request.",
          isDone: false,
        },
      ];
    case "confirmed":
      return [
        {
          title: "Booking request created",
          description: "Your consultation request was submitted successfully.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The payment hold was authorized for this booking request.",
          isDone: true,
        },
        {
          title: "Consultant accepted request",
          description: "The consultant reviewed your request and confirmed the session.",
          isDone: true,
        },
        {
          title: "Session confirmed",
          description: "Your consultation is scheduled and ready for delivery.",
          isDone: true,
        },
      ];
    case "completed":
      return [
        {
          title: "Booking request created",
          description: "Your consultation request was submitted successfully.",
          isDone: true,
        },
        {
          title: "Consultant accepted request",
          description: "The consultation was approved and scheduled.",
          isDone: true,
        },
        {
          title: "Consultation delivered",
          description: "The session was completed successfully.",
          isDone: true,
        },
        {
          title: "Payment captured",
          description: "The authorized amount was captured after delivery.",
          isDone: true,
        },
      ];
    case "rejected":
      return [
        {
          title: "Booking request created",
          description: "Your consultation request entered the booking workflow.",
          isDone: true,
        },
        {
          title: "Authorization hold placed",
          description: "The temporary hold was created before consultant review.",
          isDone: true,
        },
        {
          title: "Consultant rejected request",
          description: "The request was not approved, so the booking will not proceed.",
          isDone: true,
        },
        {
          title: "Hold release flow",
          description: "The payment authorization should be released automatically.",
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
          description: "No active hold remains for this booking.",
          isDone: true,
        },
        {
          title: "Booking archived",
          description: "The cancelled request remains visible in booking history.",
          isDone: true,
        },
      ];
    default:
      return [];
  }
}

export function StudentBookingDetails() {
  const { id } = useParams();
  const bookingId = id ?? "1";

  const [bookingsSnapshot, setBookingsSnapshot] = useState<MockBookingRecord[]>(() =>
    getMockBookings(),
  );

  useEffect(() => {
    return subscribeMockBookings(() => {
      setBookingsSnapshot(getMockBookings());
    });
  }, []);

  const booking = useMemo(() => {
    return bookingsSnapshot.find((item) => item.id === bookingId) ?? bookingsSnapshot[0] ?? null;
  }, [bookingsSnapshot, bookingId]);

  const statusMeta = useMemo(
    () => getStudentStatusMeta(booking?.status ?? "pending"),
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
                to="/dev/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                Back to my bookings
              </Link>
            </div>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
                Booking details
              </h1>

              <p className="mt-3 max-w-3xl text-base leading-7 text-[#4b5563]">
                Review your consultation request, booking status, payment-hold state, and workflow
                progress from the student side.
              </p>
            </div>

            <span
              className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${statusMeta.badgeClassName}`}
            >
              {statusMeta.label}
            </span>
          </div>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Booking summary
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
                    Consultant
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {booking.consultantName}
                  </p>
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
                    Current stage
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
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Payment hold status
              </h2>

              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                <p className="text-sm font-medium text-[#1d1d1f]">{statusMeta.holdStatus}</p>
                <p className="mt-2 text-sm leading-7 text-[#4b5563]">{statusMeta.note}</p>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Booking timeline
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
                Quick actions
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                <Link
                  to="/dev/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  Back to my bookings
                </Link>

                <Link
                  to={`/dev/consultants/${booking.consultantId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  View consultant
                </Link>

                <Link
                  to="/dev/consultants"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  Book another consultation
                </Link>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                Student guidance
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-[#4b5563]">
                <p>
                  Pending requests are still under consultant review and may move to confirmed or
                  rejected depending on consultant action.
                </p>
                <p>
                  Confirmed requests mean your slot is reserved and the consultant accepted the
                  session.
                </p>
                <p>
                  Completed requests are closed successfully. Rejected and cancelled requests remain
                  visible in history for reference.
                </p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
