import { Link } from "react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { ArrowLeft } from "lucide-react";
import {
  notificationsApi,
  type NotificationPreference,
} from "@/services/api/notifications";

// ── Notification-type → group mapping ────────────────────────────────────────

function groupOf(type: string): string {
  const n = parseInt(type, 10);
  if (!isNaN(n)) {
    if (n >= 100 && n < 200) return "Applications";
    if (n >= 200 && n < 300) return "Bookings";
    if (n >= 300 && n < 400) return "Payments";
    if (n >= 400 && n < 500) return "Admin";
    if (n >= 500 && n < 600) return "Community";
    if (n >= 600 && n < 700) return "Resources";
    if (n >= 700 && n < 800) return "Chat";
    if (n >= 900) return "Broadcast";
    return "Other";
  }
  // Backend sends string names when JSON serialisation is configured that way
  if (/^Application/i.test(type)) return "Applications";
  if (/^Booking|ConsultantRating/i.test(type)) return "Bookings";
  if (/^Payment|Payout/i.test(type)) return "Payments";
  if (/^Onboarding|Upgrade|Admin/i.test(type)) return "Admin";
  if (/^Reply|Post/i.test(type)) return "Community";
  if (/^Resource/i.test(type)) return "Resources";
  if (/^Chat/i.test(type)) return "Chat";
  if (/^Broadcast/i.test(type)) return "Broadcast";
  if (/^Company/i.test(type)) return "Payments";
  return "Other";
}

const GROUP_ORDER = [
  "Applications", "Bookings", "Payments", "Chat", "Community", "Resources", "Admin", "Broadcast", "Other",
];

// ── Toggle switch ─────────────────────────────────────────────────────────────

function Toggle({
  checked,
  onChange,
  disabled,
  label,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  disabled?: boolean;
  label: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 disabled:cursor-not-allowed disabled:opacity-50 ${
        checked ? "bg-brand-500" : "bg-bg-subtle border border-border-default"
      }`}
    >
      <span
        className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-sm ring-0 transition-transform ${
          checked ? "translate-x-4" : "translate-x-0"
        }`}
      />
    </button>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function NotificationPreferences() {
  const { t } = useTranslation("notifications");
  const qc = useQueryClient();

  const { data, isLoading, isError } = useQuery({
    queryKey: ["notifications", "preferences"],
    queryFn: notificationsApi.getPreferences,
    staleTime: 30_000,
  });

  const updateMut = useMutation({
    mutationFn: ({ type, channel, isEnabled }: Pick<NotificationPreference, "type" | "channel" | "isEnabled">) =>
      notificationsApi.updatePreference(type, channel, isEnabled),
    onMutate: async ({ type, channel, isEnabled }) => {
      // Optimistic update
      await qc.cancelQueries({ queryKey: ["notifications", "preferences"] });
      const prev = qc.getQueryData<typeof data>(["notifications", "preferences"]);
      qc.setQueryData<typeof data>(["notifications", "preferences"], (old) => {
        if (!old) return old;
        const updated = old.preferences.map((p) =>
          p.type === type && p.channel === channel ? { ...p, isEnabled } : p,
        );
        return { preferences: updated };
      });
      return { prev };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(["notifications", "preferences"], ctx.prev);
      toast.error(t("preferences.saveError"));
    },
    onSettled: () => {
      void qc.invalidateQueries({ queryKey: ["notifications", "preferences"] });
    },
  });

  if (isLoading) {
    return (
      <p className="py-12 text-center text-sm text-text-tertiary">
        {t("preferences.loading")}
      </p>
    );
  }

  if (isError || !data) {
    return (
      <div className="space-y-3 py-12 text-center">
        <p className="text-sm text-text-tertiary">{t("preferences.loadError")}</p>
        <Link
          to="/notifications"
          className="inline-flex items-center gap-1 text-sm text-brand-500 underline"
        >
          <ArrowLeft className="size-4" />
          {t("preferences.back")}
        </Link>
      </div>
    );
  }

  // Build a map: type → { InApp: bool, Email: bool }
  const prefMap = new Map<string, Record<string, boolean>>();
  for (const p of data.preferences) {
    if (!prefMap.has(p.type)) prefMap.set(p.type, {});
    prefMap.get(p.type)![p.channel] = p.isEnabled;
  }

  // Collect unique types, group them
  const types = [...new Set(data.preferences.map((p) => p.type))];
  const channels = [...new Set(data.preferences.map((p) => p.channel))].filter(
    (c) => c === "InApp" || c === "Email",
  );

  const grouped = new Map<string, string[]>();
  for (const type of types) {
    const g = groupOf(type);
    if (!grouped.has(g)) grouped.set(g, []);
    grouped.get(g)!.push(type);
  }

  const orderedGroups = GROUP_ORDER.filter((g) => grouped.has(g));

  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-8">
      <div>
        <Link
          to="/notifications"
          className="mb-4 inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary"
        >
          <ArrowLeft className="size-4" />
          {t("preferences.back")}
        </Link>
        <h1 className="text-2xl font-semibold text-text-primary">{t("preferences.title")}</h1>
        <p className="mt-1 text-sm text-text-secondary">{t("preferences.subtitle")}</p>
      </div>

      <div className="space-y-6">
        {orderedGroups.map((group) => {
          const groupTypes = grouped.get(group)!;
          return (
            <section key={group} className="rounded-xl border border-border-subtle bg-bg-elevated">
              <div className="border-b border-border-subtle px-5 py-3">
                <h2 className="text-sm font-semibold text-text-primary">
                  {t(`preferences.groups.${group}`, { defaultValue: group })}
                </h2>
              </div>
              <div className="divide-y divide-border-subtle">
                {/* Header row */}
                <div className="grid grid-cols-[1fr_repeat(2,5rem)] items-center px-5 py-2">
                  <span className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
                    {t("title")}
                  </span>
                  {channels.map((ch) => (
                    <span
                      key={ch}
                      className="text-center text-xs font-medium uppercase tracking-wide text-text-tertiary"
                    >
                      {t(`preferences.channel${ch}`)}
                    </span>
                  ))}
                </div>

                {groupTypes.map((type) => {
                  const prefs = prefMap.get(type) ?? {};
                  return (
                    <div
                      key={type}
                      className="grid grid-cols-[1fr_repeat(2,5rem)] items-center px-5 py-3"
                    >
                      <span className="text-sm text-text-primary">
                        {t(`preferences.types.${type}`, { defaultValue: type })}
                      </span>
                      {channels.map((ch) => {
                        const enabled = prefs[ch] ?? true;
                        return (
                          <div key={ch} className="flex justify-center">
                            <Toggle
                              checked={enabled}
                              disabled={updateMut.isPending}
                              label={`${type} ${ch}`}
                              onChange={(next) =>
                                updateMut.mutate({ type, channel: ch, isEnabled: next })
                              }
                            />
                          </div>
                        );
                      })}
                    </div>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
