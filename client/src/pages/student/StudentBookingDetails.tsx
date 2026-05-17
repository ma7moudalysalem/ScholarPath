import {
  getMockBookings,
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

type StudentStatusMeta = {
  labelKey: string;
  badgeClassName: string;
  holdStatusKey: string;
  summaryKey: string;
  noteKey: string;
};

function getStudentStatusMeta(status: BookingWorkflowStatus): StudentStatusMeta {
  switch (status) {
    case "pending":
      return {
        labelKey: "status.pending",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatusKey: "holdStatus.active",
        summaryKey: "summaries.pending",
        noteKey: "holdNotes.pending",
      };
    case "confirmed":
      return {
        labelKey: "status.accepted",
        badgeClassName: "bg-[#eff6ff] text-[#1d4ed8]",
        holdStatusKey: "holdStatus.readyForCapture",
        summaryKey: "summaries.accepted",
        noteKey: "holdNotes.accepted",
      };
    case "completed":
      return {
        labelKey: "status.completed",
        badgeClassName: "bg-[#f0fdf4] text-[#15803d]",
        holdStatusKey: "holdStatus.captured",
        summaryKey: "summaries.completed",
        noteKey: "holdNotes.completed",
      };
    case "rejected":
      return {
        labelKey: "status.rejected",
        badgeClassName: "bg-[#fef2f2] text-[#dc2626]",
        holdStatusKey: "holdStatus.released",
        summaryKey: "summaries.rejected",
        noteKey: "holdNotes.rejected",
      };
    case "cancelled":
      return {
        labelKey: "status.cancelled",
        badgeClassName: "bg-[#f3f4f6] text-[#4b5563]",
        holdStatusKey: "holdStatus.none",
        summaryKey: "summaries.cancelled",
        noteKey: "holdNotes.cancelled",
      };
    default:
      return {
        labelKey: "status.pending",
        badgeClassName: "bg-[#fffbeb] text-[#b45309]",
        holdStatusKey: "holdStatus.active",
        summaryKey: "summaries.default",
        noteKey: "holdNotes.default",
      };
  }
}

function buildTimeline(status: BookingWorkflowStatus): TimelineStep[] {
  switch (status) {
    case "pending":
      return [
        {
          titleKey: "timeline.pending.step1Title",
          descriptionKey: "timeline.pending.step1Description",
          isDone: true,
        },
        {
          titleKey: "timeline.pending.step2Title",
          descriptionKey: "timeline.pending.step2Description",
          isDone: true,
        },
        {
          titleKey: "timeline.pending.step3Title",
          descriptionKey: "timeline.pending.step3Description",
          isDone: false,
        },
        {
          titleKey: "timeline.pending.step4Title",
          descriptionKey: "timeline.pending.step4Description",
          isDone: false,
        },
      ];
    case "confirmed":
      return [
        {
          titleKey: "timeline.confirmed.step1Title",
          descriptionKey: "timeline.confirmed.step1Description",
          isDone: true,
        },
        {
          titleKey: "timeline.confirmed.step2Title",
          descriptionKey: "timeline.confirmed.step2Description",
          isDone: true,
        },
        {
          titleKey: "timeline.confirmed.step3Title",
          descriptionKey: "timeline.confirmed.step3Description",
          isDone: true,
        },
        {
          titleKey: "timeline.confirmed.step4Title",
          descriptionKey: "timeline.confirmed.step4Description",
          isDone: true,
        },
      ];
    case "completed":
      return [
        {
          titleKey: "timeline.completed.step1Title",
          descriptionKey: "timeline.completed.step1Description",
          isDone: true,
        },
        {
          titleKey: "timeline.completed.step2Title",
          descriptionKey: "timeline.completed.step2Description",
          isDone: true,
        },
        {
          titleKey: "timeline.completed.step3Title",
          descriptionKey: "timeline.completed.step3Description",
          isDone: true,
        },
        {
          titleKey: "timeline.completed.step4Title",
          descriptionKey: "timeline.completed.step4Description",
          isDone: true,
        },
      ];
    case "rejected":
      return [
        {
          titleKey: "timeline.rejected.step1Title",
          descriptionKey: "timeline.rejected.step1Description",
          isDone: true,
        },
        {
          titleKey: "timeline.rejected.step2Title",
          descriptionKey: "timeline.rejected.step2Description",
          isDone: true,
        },
        {
          titleKey: "timeline.rejected.step3Title",
          descriptionKey: "timeline.rejected.step3Description",
          isDone: true,
        },
        {
          titleKey: "timeline.rejected.step4Title",
          descriptionKey: "timeline.rejected.step4Description",
          isDone: true,
        },
      ];
    case "cancelled":
      return [
        {
          titleKey: "timeline.cancelled.step1Title",
          descriptionKey: "timeline.cancelled.step1Description",
          isDone: true,
        },
        {
          titleKey: "timeline.cancelled.step2Title",
          descriptionKey: "timeline.cancelled.step2Description",
          isDone: true,
        },
        {
          titleKey: "timeline.cancelled.step3Title",
          descriptionKey: "timeline.cancelled.step3Description",
          isDone: true,
        },
        {
          titleKey: "timeline.cancelled.step4Title",
          descriptionKey: "timeline.cancelled.step4Description",
          isDone: true,
        },
      ];
    default:
      return [];
  }
}

export function StudentBookingDetails() {
  const { t } = useTranslation("bookings");
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
            <h1 className="text-2xl font-semibold text-[#1d1d1f]">{t("notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">{t("notFound.description")}</p>
            <div className="mt-6">
              <Link
                to="/dev/bookings"
                className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
              >
                {t("notFound.backToBookings")}
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
          <p className="text-sm font-medium text-[#2563eb]">{t("tag")}</p>

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
              {t(statusMeta.labelKey)}
            </span>
          </div>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.summaryTitle")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-[#4b5563]">{t(statusMeta.summaryKey)}</p>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.bookingReference")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.reference}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.consultant")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {booking.consultantName}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.studentEmail")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentEmail}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.sessionTopic")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.topic}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.sessionType}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.currentStage")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentStage}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.date}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.time}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.duration}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.fee")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.fee}</p>
                </div>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.holdTitle")}
              </h2>

              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                <p className="text-sm font-medium text-[#1d1d1f]">{t(statusMeta.holdStatusKey)}</p>
                <p className="mt-2 text-sm leading-7 text-[#4b5563]">{t(statusMeta.noteKey)}</p>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.timelineTitle")}
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
                {t("details.quickActionsTitle")}
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                <Link
                  to="/dev/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("details.backToBookings")}
                </Link>

                <Link
                  to={`/dev/consultants/${booking.consultantId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.viewConsultant")}
                </Link>

                <Link
                  to="/dev/consultants"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.bookAnother")}
                </Link>
              </div>
            </div>

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.guidanceTitle")}
              </h2>

              <div className="mt-5 space-y-3 text-sm leading-7 text-[#4b5563]">
                <p>{t("guidance.pending")}</p>
                <p>{t("guidance.confirmed")}</p>
                <p>{t("guidance.closed")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
