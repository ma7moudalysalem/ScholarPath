import {
  getMockBookings,
  resetMockBookings,
  setMockBookingStatus,
  subscribeMockBookings,
  type BookingWorkflowStatus,
  type MockBookingRecord,
} from "@/lib/mockBookingStore";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";

type TimelineStep = {
  titleKey: string;
  descriptionKey: string;
  isDone: boolean;
};

type ConsultantStatusMeta = {
  statusKey: BookingWorkflowStatus;
  badgeClassName: string;
  summaryKey: string;
  noteKey: string;
};

function getConsultantStatusMeta(status: BookingWorkflowStatus): ConsultantStatusMeta {
  switch (status) {
    case "pending":
      return {
        statusKey: "pending",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        summaryKey: "details.summaryCard.text.pending",
        noteKey: "details.guidance.note.pending",
      };
    case "confirmed":
      return {
        statusKey: "confirmed",
        badgeClassName: "bg-[#eff6ff] text-[#1d4ed8]",
        summaryKey: "details.summaryCard.text.confirmed",
        noteKey: "details.guidance.note.confirmed",
      };
    case "completed":
      return {
        statusKey: "completed",
        badgeClassName: "bg-[#f0fdf4] text-[#15803d]",
        summaryKey: "details.summaryCard.text.completed",
        noteKey: "details.guidance.note.completed",
      };
    case "rejected":
      return {
        statusKey: "rejected",
        badgeClassName: "bg-[#fef2f2] text-[#dc2626]",
        summaryKey: "details.summaryCard.text.rejected",
        noteKey: "details.guidance.note.rejected",
      };
    case "cancelled":
      return {
        statusKey: "cancelled",
        badgeClassName: "bg-[#f3f4f6] text-[#4b5563]",
        summaryKey: "details.summaryCard.text.cancelled",
        noteKey: "details.guidance.note.cancelled",
      };
    default:
      return {
        statusKey: "pending",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        summaryKey: "details.summaryCard.text.default",
        noteKey: "details.guidance.note.default",
      };
  }
}

function buildTimeline(status: BookingWorkflowStatus): TimelineStep[] {
  switch (status) {
    case "pending":
      return [
        {
          titleKey: "details.timeline.pending.step1Title",
          descriptionKey: "details.timeline.pending.step1Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.pending.step2Title",
          descriptionKey: "details.timeline.pending.step2Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.pending.step3Title",
          descriptionKey: "details.timeline.pending.step3Description",
          isDone: false,
        },
        {
          titleKey: "details.timeline.pending.step4Title",
          descriptionKey: "details.timeline.pending.step4Description",
          isDone: false,
        },
      ];
    case "confirmed":
      return [
        {
          titleKey: "details.timeline.confirmed.step1Title",
          descriptionKey: "details.timeline.confirmed.step1Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.confirmed.step2Title",
          descriptionKey: "details.timeline.confirmed.step2Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.confirmed.step3Title",
          descriptionKey: "details.timeline.confirmed.step3Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.confirmed.step4Title",
          descriptionKey: "details.timeline.confirmed.step4Description",
          isDone: true,
        },
      ];
    case "completed":
      return [
        {
          titleKey: "details.timeline.completed.step1Title",
          descriptionKey: "details.timeline.completed.step1Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.completed.step2Title",
          descriptionKey: "details.timeline.completed.step2Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.completed.step3Title",
          descriptionKey: "details.timeline.completed.step3Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.completed.step4Title",
          descriptionKey: "details.timeline.completed.step4Description",
          isDone: true,
        },
      ];
    case "rejected":
      return [
        {
          titleKey: "details.timeline.rejected.step1Title",
          descriptionKey: "details.timeline.rejected.step1Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.rejected.step2Title",
          descriptionKey: "details.timeline.rejected.step2Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.rejected.step3Title",
          descriptionKey: "details.timeline.rejected.step3Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.rejected.step4Title",
          descriptionKey: "details.timeline.rejected.step4Description",
          isDone: true,
        },
      ];
    case "cancelled":
      return [
        {
          titleKey: "details.timeline.cancelled.step1Title",
          descriptionKey: "details.timeline.cancelled.step1Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.cancelled.step2Title",
          descriptionKey: "details.timeline.cancelled.step2Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.cancelled.step3Title",
          descriptionKey: "details.timeline.cancelled.step3Description",
          isDone: true,
        },
        {
          titleKey: "details.timeline.cancelled.step4Title",
          descriptionKey: "details.timeline.cancelled.step4Description",
          isDone: true,
        },
      ];
    default:
      return [];
  }
}

export function ConsultantBookingDetails() {
  const { t } = useTranslation("consultantPortal");
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

  const bannerKey = bannerByBooking[bookingId] ?? "";

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
            <h1 className="text-2xl font-semibold text-[#1d1d1f]">{t("details.notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">
              {t("details.notFound.description")}
            </p>
            <div className="mt-6">
              <Link
                to="/dev/consultant/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                {t("details.notFound.back")}
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
      [bookingId]: "details.banner.accepted",
    }));
  };

  const handleReject = () => {
    if (booking.status !== "pending") return;

    setMockBookingStatus(booking.id, "rejected");
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "details.banner.rejected",
    }));
  };

  const handleComplete = () => {
    if (booking.status !== "confirmed") return;

    setMockBookingStatus(booking.id, "completed");
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "details.banner.completed",
    }));
  };

  const handleReset = () => {
    resetMockBookings();
    setBannerByBooking((current) => ({
      ...current,
      [bookingId]: "details.banner.reset",
    }));
  };

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
          <p className="text-sm font-medium text-[#2563eb]">{t("moduleTag")}</p>

          <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <h1 className="text-4xl font-bold tracking-[-0.02em] text-[#1d1d1f]">
                {t("details.title")}
              </h1>

              <p className="mt-3 max-w-3xl text-base leading-7 text-[#4b5563]">
                {t("details.subtitle")}
              </p>
            </div>

            <span
              className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${statusMeta.badgeClassName}`}
            >
              {t(`status.${statusMeta.statusKey}`)}
            </span>
          </div>
        </div>

        {bannerKey ? (
          <div className="mt-8 rounded-xl border border-[#bbf7d0] bg-[#f0fdf4] p-5">
            <p className="text-sm font-medium text-[#166534]">{t(bannerKey)}</p>
          </div>
        ) : null}

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.summaryCard.title")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-[#4b5563]">{t(statusMeta.summaryKey)}</p>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.bookingReference")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.reference}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.studentName")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentName}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.studentEmail")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentEmail}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.sessionTopic")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.topic}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.sessionType}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.studentStage")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentStage}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.date}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.time}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.duration}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.fee")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.fee}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.holdStatus")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {t(`holdStatus.${statusMeta.statusKey}`)}
                  </p>
                </div>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.guidance.title")}
              </h2>

              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                <p className="text-sm leading-7 text-[#4b5563]">{t(statusMeta.noteKey)}</p>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.timeline.title")}
              </h2>

              <div className="mt-5 space-y-4">
                {timeline.map((step, index) => (
                  <div key={step.titleKey} className="flex gap-4">
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
                      <p className="text-sm font-medium text-[#1d1d1f]">{t(step.titleKey)}</p>
                      <p className="mt-1 text-sm leading-6 text-[#4b5563]">
                        {t(step.descriptionKey)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>

          <aside className="space-y-6 lg:col-span-4">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.actions.title")}
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
                  {t("details.actions.accept")}
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
                  {t("details.actions.reject")}
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
                  {t("details.actions.complete")}
                </button>

                <button
                  type="button"
                  onClick={handleReset}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.actions.reset")}
                </button>

                <Link
                  to="/dev/consultant/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.actions.back")}
                </Link>

                <Link
                  to={`/dev/bookings/${booking.id}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.actions.openStudentView")}
                </Link>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.decisionHelp.title")}
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-[#4b5563]">
                <p>{t("details.decisionHelp.accept")}</p>
                <p>{t("details.decisionHelp.reject")}</p>
                <p>{t("details.decisionHelp.complete")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
