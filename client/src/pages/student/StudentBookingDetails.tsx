import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import { useBookingDetailQuery } from "@/hooks/useBookingsQuery";
import type { BookingDetail } from "@/services/api/bookings";
import {
  durationLabel,
  formatDate,
  formatTime,
  formatUsd,
  statusBadgeClass,
  statusBucket,
  statusLabelKey,
} from "@/lib/bookingFormat";

type TimelineStep = {
  key: string;
  titleKey: string;
  descriptionKey: string;
  isDone: boolean;
};

/** Builds the four-step student timeline from the booking's real status. */
function buildTimeline(booking: BookingDetail): TimelineStep[] {
  const bucket = statusBucket(booking.status);
  const requested = true;
  const reviewed =
    booking.status !== "Requested" && booking.status !== "Expired";
  const confirmed = Boolean(booking.confirmedAt) || bucket === "completed";
  const finalised =
    bucket === "completed" || bucket === "closed";

  return [
    {
      key: "requested",
      titleKey: "timeline.requestedTitle",
      descriptionKey: "timeline.requestedDescription",
      isDone: requested,
    },
    {
      key: "review",
      titleKey: "timeline.reviewTitle",
      descriptionKey: "timeline.reviewDescription",
      isDone: reviewed,
    },
    {
      key: "confirmed",
      titleKey: "timeline.confirmedTitle",
      descriptionKey: "timeline.confirmedDescription",
      isDone: confirmed,
    },
    {
      key: "outcome",
      titleKey: "timeline.outcomeTitle",
      descriptionKey: "timeline.outcomeDescription",
      isDone: finalised || confirmed,
    },
  ];
}

export function StudentBookingDetails() {
  const { t, i18n } = useTranslation("bookings");
  const lang = i18n.language;
  const { id } = useParams();

  const { data: booking, isLoading, isError } = useBookingDetailQuery(id);

  const timeline = useMemo(
    () => (booking ? buildTimeline(booking) : []),
    [booking],
  );

  if (isLoading) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-white" />
            <div className="h-72 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm" />
            <div className="h-40 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm" />
          </div>
        </section>
      </main>
    );
  }

  if (isError || !booking) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-sm">
            <h1 className="text-2xl font-semibold text-[#1d1d1f]">{t("notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">{t("notFound.description")}</p>
            <div className="mt-6">
              <Link
                to="/student/bookings"
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

  const bucket = statusBucket(booking.status);

  return (
    <main className="min-h-screen bg-[#f5f5f7]">
      <section className="mx-auto w-full max-w-[1280px] px-4 py-10 sm:px-6 lg:px-8">
        <div className="space-y-3">
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
              className={`inline-flex rounded-full px-3 py-1 text-xs font-medium ${statusBadgeClass(
                booking.status,
              )}`}
            >
              {t(statusLabelKey(booking.status))}
            </span>
          </div>
        </div>

        <div className="mt-8 grid gap-6 lg:grid-cols-12">
          <section className="space-y-6 lg:col-span-8">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.summaryTitle")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-[#4b5563]">{t(`summaries.${bucket}`)}</p>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 xl:grid-cols-3">
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
                    {t("fields.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{t("sessionType")}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatDate(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatTime(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {durationLabel(booking.durationMinutes, t)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("fields.fee")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatUsd(booking.priceUsd)}
                  </p>
                </div>
              </div>
            </div>

            {booking.meetingUrl ? (
              <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
                <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                  {t("details.meetingTitle")}
                </h2>

                <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                  <p className="text-sm leading-7 text-[#4b5563]">{t("details.meetingNote")}</p>
                  <a
                    href={booking.meetingUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-4 inline-flex h-11 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    {t("details.joinMeeting")}
                  </a>
                </div>
              </div>
            ) : null}

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.timelineTitle")}
              </h2>

              <div className="mt-5 space-y-4">
                {timeline.map((step, index) => (
                  <div key={step.key} className="flex gap-4">
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
                  to="/student/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                >
                  {t("details.backToBookings")}
                </Link>

                <Link
                  to={`/student/consultants/${booking.consultantId}`}
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.viewConsultant")}
                </Link>

                <Link
                  to="/student/consultants"
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
