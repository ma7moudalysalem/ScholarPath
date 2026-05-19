import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Download, Video } from "lucide-react";
import { meetingsApi } from "@/services/api/meetings";

function formatBytes(bytes: number): string {
  if (bytes <= 0) return "—";
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Lists a booking's session recordings with download links (PB-006). Renders
 * nothing until at least one recording exists. The endpoint is authorized to
 * the booking's student, its consultant, and admins.
 */
export function BookingRecordings({ bookingId }: { bookingId: string }) {
  const { t, i18n } = useTranslation("bookings");

  const { data: recordings = [] } = useQuery({
    queryKey: ["booking-recordings", bookingId],
    queryFn: () => meetingsApi.listRecordings(bookingId),
  });

  const download = async (id: string) => {
    try {
      const blob = await meetingsApi.downloadRecording(id);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `session-recording-${id}.mp4`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      toast.error(t("meeting.downloadError"));
    }
  };

  if (recordings.length === 0) return null;

  return (
    <div className="rounded-2xl border border-border-subtle bg-bg-elevated p-6 shadow-sm">
      <h2 className="text-lg font-semibold tracking-[-0.01em] text-text-primary">
        {t("meeting.recordingsTitle")}
      </h2>
      <ul className="mt-5 space-y-2">
        {recordings.map((r) => (
          <li
            key={r.id}
            className="flex items-center justify-between gap-3 rounded-xl border border-border-subtle bg-bg-muted px-4 py-3"
          >
            <span className="flex min-w-0 items-center gap-2 text-sm text-text-primary">
              <Video aria-hidden className="size-4 shrink-0 text-text-tertiary" />
              <span className="truncate">
                {new Date(r.recordedAt).toLocaleString(i18n.language)}
              </span>
              <span className="text-xs text-text-tertiary">{formatBytes(r.sizeBytes)}</span>
            </span>
            <button
              type="button"
              onClick={() => void download(r.id)}
              className="inline-flex shrink-0 items-center gap-1 rounded-md border border-border-subtle px-2.5 py-1 text-xs text-text-secondary transition hover:border-brand-500 hover:text-brand-500"
            >
              <Download aria-hidden className="size-3.5" />
              {t("meeting.download")}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
