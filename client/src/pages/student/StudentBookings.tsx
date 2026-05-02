import {
  getMockBookings,
  subscribeMockBookings,
  type BookingWorkflowStatus,
  type MockBookingRecord,
} from "@/lib/mockBookingStore";
import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";

type StudentFilter = "all" | "pending" | "confirmed" | "completed" | "closed";

function getStudentStatusMeta(status: BookingWorkflowStatus) {
  switch (status) {
    case "pending":
      return {
        label: "Pending consultant response",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        note: "Your booking request is waiting for the consultant to review it.",
      };
    case "confirmed":
      return {
        label: "Accepted",
        badgeClassName: "bg-[#eff6ff] text-[#1d4ed8]",
        holdStatus: "Ready for capture",
        note: "The consultant accepted your request and the session is now confirmed.",
      };
    case "completed":
      return {
        label: "Completed",
        badgeClassName: "bg-[#f0fdf4] text-[#15803d]",
        holdStatus: "Payment captured",
        note: "The consultation was delivered successfully and the booking is closed.",
      };
    case "rejected":
      return {
        label: "Rejected",
        badgeClassName: "bg-[#fef2f2] text-[#dc2626]",
        holdStatus: "Hold released",
        note: "The consultant rejected this request and the payment hold should be released.",
      };
    case "cancelled":
      return {
        label: "Cancelled",
        badgeClassName: "bg-[#f3f4f6] text-[#4b5563]",
        holdStatus: "No active hold",
        note: "This booking was cancelled before final completion.",
      };
    default:
      return {
        label: "Pending consultant response",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatus: "Authorization hold active",
        note: "Waiting for consultant review.",
      };
  }
}

function isClosedStatus(status: BookingWorkflowStatus) {
  return status === "rejected" || status === "cancelled";
}

export function StudentBookings() {
  const [bookings, setBookings] = useState<MockBookingRecord[]>(() => getMockBookings());
  const [filter, setFilter] = useState<StudentFilter>("all");

  useEffect(() => {
    return subscribeMockBookings(() => {
      setBookings(getMockBookings());
    });
  }, []);

  const summary = useMemo(() => {
    return {
      total: bookings.length,
      pending: bookings.filter((item) => item.status === "pending").length,
      confirmed: bookings.filter((item) => item.status === "confirmed").length,
      completed: bookings.filter((item) => item.status === "completed").length,
      closed: bookings.filter((item) => isClosedStatus(item.status)).length,
    };
  }, [bookings]);

  const filteredBookings = useMemo(() => {
    switch (filter) {
      case "pending":
        return bookings.filter((item) => item.status === "pending");
      case "confirmed":
        return bookings.filter((item) => item.status === "confirmed");
      case "completed":
        return bookings.filter((item) => item.status === "completed");
      case "closed":
        return bookings.filter((item) => isClosedStatus(item.status));
      default:
        return bookings;
    }
  }, [bookings, filter]);

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">PB-006</p>

          <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">My bookings</h1>

          <p className="max-w-3xl text-base leading-7 text-[#4b5563]">
            Track your consultation requests, confirmed sessions, completed bookings, and closed
            outcomes from the student side.
          </p>
        </div>

        <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
              Total bookings
            </p>
            <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.total}</p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
              Pending
            </p>
            <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.pending}</p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
              Accepted
            </p>
            <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.confirmed}</p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
              Completed
            </p>
            <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.completed}</p>
          </div>

          <div className="rounded-xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
            <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
              Closed
            </p>
            <p className="mt-2 text-3xl font-semibold text-[#1d1d1f]">{summary.closed}</p>
          </div>
        </div>

        <div className="mt-8 rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => setFilter("all")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  filter === "all"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                All bookings
              </button>

              <button
                type="button"
                onClick={() => setFilter("pending")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  filter === "pending"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Pending
              </button>

              <button
                type="button"
                onClick={() => setFilter("confirmed")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  filter === "confirmed"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Accepted
              </button>

              <button
                type="button"
                onClick={() => setFilter("completed")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  filter === "completed"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Completed
              </button>

              <button
                type="button"
                onClick={() => setFilter("closed")}
                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                  filter === "closed"
                    ? "bg-[#2563eb] text-white"
                    : "bg-[#f3f4f6] text-[#4b5563] hover:bg-[#e5e7eb]"
                }`}
              >
                Closed
              </button>
            </div>

            <Link
              to="/dev/consultants"
              className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
            >
              Book another consultation
            </Link>
          </div>
        </div>

        <div className="mt-8 space-y-6">
          {filteredBookings.length > 0 ? (
            filteredBookings.map((booking) => {
              const statusMeta = getStudentStatusMeta(booking.status);

              return (
                <article
                  key={booking.id}
                  className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm"
                >
                  <div className="flex flex-col gap-6 xl:flex-row xl:items-start xl:justify-between">
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-3">
                        <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                          {booking.consultantName}
                        </h2>

                        <span
                          className={`rounded-full px-3 py-1 text-xs font-medium ${statusMeta.badgeClassName}`}
                        >
                          {statusMeta.label}
                        </span>
                      </div>

                      <p className="mt-2 text-sm leading-6 text-[#4b5563]">{booking.topic}</p>

                      <div className="mt-6 grid gap-4 rounded-xl bg-[#f9fafb] p-4 sm:grid-cols-2 lg:grid-cols-4">
                        <div>
                          <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                            Booking reference
                          </p>
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {booking.reference}
                          </p>
                        </div>

                        <div>
                          <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                            Session
                          </p>
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {booking.sessionType}
                          </p>
                        </div>

                        <div>
                          <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                            Date & time
                          </p>
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {booking.date} · {booking.time}
                          </p>
                        </div>

                        <div>
                          <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                            Duration
                          </p>
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {booking.duration}
                          </p>
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
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {statusMeta.holdStatus}
                          </p>
                        </div>

                        <div className="sm:col-span-2 lg:col-span-2">
                          <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                            Booking note
                          </p>
                          <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                            {statusMeta.note}
                          </p>
                        </div>
                      </div>
                    </div>

                    <aside className="w-full rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4 xl:max-w-[280px]">
                      <h3 className="text-base font-semibold text-[#1d1d1f]">Quick actions</h3>

                      <div className="mt-4 flex flex-col gap-3">
                        <Link
                          to={`/dev/bookings/${booking.id}`}
                          className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                        >
                          View booking details
                        </Link>

                        <Link
                          to={`/dev/consultants/${booking.consultantId}`}
                          className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                        >
                          View consultant
                        </Link>
                      </div>
                    </aside>
                  </div>
                </article>
              );
            })
          ) : (
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
              <h2 className="text-2xl font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                No bookings in this view
              </h2>

              <p className="mt-3 max-w-2xl text-sm leading-7 text-[#4b5563]">
                No bookings match the selected filter right now. Try another filter or create a new
                consultation booking.
              </p>

              <div className="mt-6">
                <Link
                  to="/dev/consultants"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  Browse consultants
                </Link>
              </div>
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
