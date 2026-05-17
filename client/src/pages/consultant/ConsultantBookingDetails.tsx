import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router";
import { toast } from "sonner";
import {
  useAcceptBookingMutation,
  useBookingDetailQuery,
  useCancelBookingMutation,
  useMarkNoShowMutation,
  useRejectBookingMutation,
} from "@/hooks/useBookingsQuery";
import {
  durationLabel,
  formatDate,
  formatTime,
  formatUsd,
  statusBadgeClass,
  statusBucket,
  statusLabelKey,
} from "@/lib/bookingFormat";

export function ConsultantBookingDetails() {
  const { t, i18n } = useTranslation("consultantPortal");
  const lang = i18n.language;
  const { id } = useParams();

  const { data: booking, isLoading, isError } = useBookingDetailQuery(id);

  const acceptMutation = useAcceptBookingMutation();
  const rejectMutation = useRejectBookingMutation();
  const cancelMutation = useMarkNoShowMutation();
  const noShowMutation = useCancelBookingMutation();

  const [meetingUrl, setMeetingUrl] = useState("");
  const [meetingUrlError, setMeetingUrlError] = useState("");

  if (isLoading) {
    return (
      <main className="min-h-screen bg-[#f5f5f7]">
        <section className="mx-auto w-full max-w-[960px] px-4 py-10 sm:px-6 lg:px-8">
          <div className="space-y-4">
            <div className="h-10 w-72 animate-pulse rounded-lg bg-white" />
            <div className="h-80 animate-pulse rounded-2xl border border-[#e5e7eb] bg-white shadow-sm" />
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
            <h1 className="text-2xl font-semibold text-[#1d1d1f]">{t("details.notFound.title")}</h1>
            <p className="mt-3 text-sm leading-7 text-[#4b5563]">
              {t("details.notFound.description")}
            </p>
            <div className="mt-6">
              <Link
                to="/consultant/bookings"
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

  const bucket = statusBucket(booking.status);
  const isRequested = booking.status === "Requested";
  const isConfirmed = booking.status === "Confirmed";
  const isBusy =
    acceptMutation.isPending ||
    rejectMutation.isPending ||
    cancelMutation.isPending ||
    noShowMutation.isPending;

  const handleAccept = () => {
    if (!isRequested) return;

    const trimmed = meetingUrl.trim();
    if (!trimmed) {
      setMeetingUrlError(t("details.actions.meetingUrlRequired"));
      return;
    }
    setMeetingUrlError("");

    acceptMutation.mutate(
      { id: booking.id, meetingUrl: trimmed },
      {
        onSuccess: () => toast.success(t("details.banner.accepted")),
        onError: () => toast.error(t("states.error")),
      },
    );
  };

  const handleReject = () => {
    if (!isRequested) return;
    rejectMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.rejected")),
      onError: () => toast.error(t("states.error")),
    });
  };

  const handleCancel = () => {
    if (!isRequested && !isConfirmed) return;
    noShowMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.cancelled")),
      onError: () => toast.error(t("states.error")),
    });
  };

  const handleNoShow = () => {
    if (!isConfirmed) return;
    cancelMutation.mutate(booking.id, {
      onSuccess: () => toast.success(t("details.banner.noShow")),
      onError: () => toast.error(t("states.error")),
    });
  };

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
                {t("details.summaryCard.title")}
              </h2>

              <p className="mt-3 text-sm leading-7 text-[#4b5563]">
                {t(`details.summaryCard.text.${bucket}`)}
              </p>

              <div className="mt-5 grid gap-4 rounded-xl bg-[#f9fafb] p-5 sm:grid-cols-2 xl:grid-cols-3">
                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.studentName")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{booking.studentName}</p>
                </div>

                {booking.studentEmail ? (
                  <div>
                    <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                      {t("details.summaryCard.studentEmail")}
                    </p>
                    <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                      {booking.studentEmail}
                    </p>
                  </div>
                ) : null}

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.sessionType")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">{t("sessionType")}</p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.selectedDate")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatDate(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.selectedTime")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {formatTime(booking.scheduledStartAt, lang)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.duration")}
                  </p>
                  <p className="mt-1 text-sm font-medium text-[#1d1d1f]">
                    {durationLabel(booking.durationMinutes, t)}
                  </p>
                </div>

                <div>
                  <p className="text-[10px] font-medium tracking-[0.02em] text-[#9ca3af] uppercase">
                    {t("details.summaryCard.fee")}
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
                  {t("details.meeting.title")}
                </h2>

                <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                  <p className="text-sm leading-7 text-[#4b5563]">{t("details.meeting.note")}</p>
                  <a
                    href={booking.meetingUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="mt-4 inline-flex h-11 items-center justify-center rounded-lg bg-[#2563eb] px-5 text-sm font-medium text-white transition hover:bg-[#1d4ed8]"
                  >
                    {t("details.meeting.join")}
                  </a>
                </div>
              </div>
            ) : null}

            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.guidance.title")}
              </h2>

              <div className="mt-5 rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-5">
                <p className="text-sm leading-7 text-[#4b5563]">
                  {t(`details.guidance.note.${bucket}`)}
                </p>
              </div>
            </div>
          </section>

          <aside className="space-y-6 lg:col-span-4">
            <div className="rounded-2xl border border-[#e5e7eb] bg-white p-6 shadow-sm">
              <h2 className="text-lg font-semibold tracking-[-0.01em] text-[#1d1d1f]">
                {t("details.actions.title")}
              </h2>

              <div className="mt-5 flex flex-col gap-3">
                {isRequested ? (
                  <div>
                    <label className="block text-sm font-medium text-[#1d1d1f]">
                      {t("details.actions.meetingUrlLabel")}
                    </label>
                    <input
                      type="url"
                      value={meetingUrl}
                      onChange={(event) => {
                        setMeetingUrl(event.target.value);
                        setMeetingUrlError("");
                      }}
                      placeholder={t("details.actions.meetingUrlPlaceholder")}
                      className={`mt-2 h-11 w-full rounded-lg border bg-white px-3 text-sm text-[#1d1d1f] outline-none ${
                        meetingUrlError
                          ? "border-[#ef4444] focus:border-[#ef4444]"
                          : "border-[#d1d5db] focus:border-[#2563eb]"
                      }`}
                    />
                    {meetingUrlError ? (
                      <p className="mt-2 text-sm text-[#dc2626]">{meetingUrlError}</p>
                    ) : null}
                  </div>
                ) : null}

                <button
                  type="button"
                  onClick={handleAccept}
                  disabled={!isRequested || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isRequested && !isBusy
                      ? "bg-[#2563eb] text-white hover:bg-[#1d4ed8]"
                      : "cursor-not-allowed bg-[#e5e7eb] text-[#9ca3af]",
                  ].join(" ")}
                >
                  {acceptMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.accept")}
                </button>

                <button
                  type="button"
                  onClick={handleReject}
                  disabled={!isRequested || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isRequested && !isBusy
                      ? "border border-[#dc2626] bg-white text-[#dc2626] hover:bg-[#fef2f2]"
                      : "cursor-not-allowed border border-[#e5e7eb] bg-white text-[#9ca3af]",
                  ].join(" ")}
                >
                  {rejectMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.reject")}
                </button>

                <button
                  type="button"
                  onClick={handleNoShow}
                  disabled={!isConfirmed || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    isConfirmed && !isBusy
                      ? "border border-[#b45309] bg-white text-[#b45309] hover:bg-[#fffbeb]"
                      : "cursor-not-allowed border border-[#e5e7eb] bg-white text-[#9ca3af]",
                  ].join(" ")}
                >
                  {cancelMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.noShow")}
                </button>

                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={(!isRequested && !isConfirmed) || isBusy}
                  className={[
                    "inline-flex h-12 items-center justify-center rounded-lg px-5 text-sm font-medium transition",
                    (isRequested || isConfirmed) && !isBusy
                      ? "border border-[#dc2626] bg-white text-[#dc2626] hover:bg-[#fef2f2]"
                      : "cursor-not-allowed border border-[#e5e7eb] bg-white text-[#9ca3af]",
                  ].join(" ")}
                >
                  {noShowMutation.isPending
                    ? t("states.submitting")
                    : t("details.actions.cancel")}
                </button>

                <Link
                  to="/consultant/bookings"
                  className="inline-flex h-12 items-center justify-center rounded-lg border-[1.5px] border-[#2563eb] bg-transparent px-5 text-sm font-medium text-[#2563eb] transition hover:bg-[#eff6ff]"
                >
                  {t("details.actions.back")}
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
                <p>{t("details.decisionHelp.noShow")}</p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </main>
  );
}
