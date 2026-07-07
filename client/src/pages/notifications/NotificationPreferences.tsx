import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Link } from "react-router";
import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { motion } from "motion/react";
import {
  ArrowLeft,
  Bell,
  BookOpen,
  Briefcase,
  Calendar,
  CreditCard,
  Loader2,
  Mail,
  MessageSquare,
  Moon,
  Send,
  Smartphone,
  Sparkles,
  TestTube,
  Users,
  Volume2,
  VolumeX,
} from "lucide-react";
import {
  notificationsApi,
  type NotificationPreference,
  type NotificationSettings,
} from "@/services/api/notifications";
import {
  isNotificationSoundEnabled,
  playNotificationChime,
  setNotificationSoundEnabled,
} from "@/lib/notificationSound";
import { usePaymentsEnabled } from "@/hooks/usePlatformStatus";
import { cn } from "@/lib/utils";

// ── Group/category metadata ──────────────────────────────────────────────────

interface GroupMeta {
  icon: ReactNode;
  // brand|success|warning|danger|info — drives the tint of the icon background
  tone: "brand" | "success" | "warning" | "neutral";
}

const GROUP_META: Record<string, GroupMeta> = {
  Applications: {
    icon: <BookOpen aria-hidden className="size-4" />,
    tone: "brand",
  },
  Bookings: {
    icon: <Calendar aria-hidden className="size-4" />,
    tone: "brand",
  },
  Payments: {
    icon: <CreditCard aria-hidden className="size-4" />,
    tone: "success",
  },
  Chat: {
    icon: <MessageSquare aria-hidden className="size-4" />,
    tone: "brand",
  },
  Community: {
    icon: <Users aria-hidden className="size-4" />,
    tone: "brand",
  },
  Resources: {
    icon: <BookOpen aria-hidden className="size-4" />,
    tone: "neutral",
  },
  Admin: {
    icon: <Briefcase aria-hidden className="size-4" />,
    tone: "warning",
  },
  Broadcast: {
    icon: <Sparkles aria-hidden className="size-4" />,
    tone: "brand",
  },
  Other: {
    icon: <Bell aria-hidden className="size-4" />,
    tone: "neutral",
  },
};

const TONE_CLASSES: Record<GroupMeta["tone"], string> = {
  brand: "bg-brand-50 text-brand-600",
  success: "bg-success-50 text-success-600",
  warning: "bg-warning-50 text-warning-600",
  neutral: "bg-bg-subtle text-text-secondary",
};

const CHANNEL_META: Record<string, { icon: ReactNode; key: string }> = {
  InApp: { icon: <Bell aria-hidden className="size-3.5" />, key: "channelInApp" },
  Email: { icon: <Mail aria-hidden className="size-3.5" />, key: "channelEmail" },
  Sms: { icon: <Smartphone aria-hidden className="size-3.5" />, key: "channelSms" },
  Push: { icon: <Bell aria-hidden className="size-3.5" />, key: "channelPush" },
};

const GROUP_ORDER = [
  "Applications",
  "Bookings",
  "Payments",
  "Chat",
  "Community",
  "Resources",
  "Admin",
  "Broadcast",
  "Other",
] as const;

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
  if (/^Application/i.test(type)) return "Applications";
  if (/^Booking|ConsultantRating/i.test(type)) return "Bookings";
  if (/^Payment|Payout/i.test(type)) return "Payments";
  if (/^Onboarding|Upgrade|Admin/i.test(type)) return "Admin";
  if (/^Reply|Post/i.test(type)) return "Community";
  if (/^Resource/i.test(type)) return "Resources";
  if (/^Chat/i.test(type)) return "Chat";
  if (/^Broadcast/i.test(type)) return "Broadcast";
  // Only the money-bearing provider events belong under Payments; the rest
  // (rating received, review request, low-rating flag) are scholarship-domain —
  // grouping them under Payments hid their toggles entirely in free mode.
  if (/^ScholarshipProvider/i.test(type)) {
    return isMoneyNotificationType(type) ? "Payments" : "Applications";
  }
  return "Other";
}

/**
 * Whether a notification type is genuinely about money (a charge, refund, hold,
 * or payout) — as opposed to the non-money ScholarshipProvider events (rating
 * received, incoming/completed review request, low-rating flag) that groupOf()
 * also buckets under "Payments". In free mode we hide ONLY the money ones; the
 * non-money provider notifications still fire, so their toggles must remain.
 */
function isMoneyNotificationType(type: string): boolean {
  return (
    /^Payment|^Payout/i.test(type) ||
    (/^ScholarshipProvider/i.test(type) && /Payment|Refund/i.test(type))
  );
}

// ── Toggle switch (premium variant) ──────────────────────────────────────────

function Toggle({
  checked,
  onChange,
  disabled,
  label,
  size = "md",
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  disabled?: boolean;
  label: string;
  size?: "sm" | "md";
}) {
  const dims =
    size === "sm"
      ? { track: "h-5 w-9", knob: "size-4", on: "translate-x-4 rtl:-translate-x-4", off: "translate-x-0.5" }
      : { track: "h-6 w-11", knob: "size-5", on: "translate-x-5 rtl:-translate-x-5", off: "translate-x-0.5" };
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative inline-flex shrink-0 cursor-pointer items-center rounded-full transition-colors",
        dims.track,
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-2",
        "disabled:cursor-not-allowed disabled:opacity-50",
        checked
          ? "bg-gradient-to-r from-brand-500 to-brand-600 shadow-[var(--shadow-brand-sm)]"
          : "bg-border-default",
      )}
    >
      <span
        className={cn(
          "pointer-events-none inline-block rounded-full bg-white shadow-sm transition-transform",
          dims.knob,
          checked ? dims.on : dims.off,
        )}
      />
    </button>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export function NotificationPreferences() {
  const { t } = useTranslation(["notifications", "common"]);
  const qc = useQueryClient();
  const paymentsEnabled = usePaymentsEnabled();

  const { data, isLoading, isError } = useQuery({
    queryKey: ["notifications", "preferences"],
    queryFn: notificationsApi.getPreferences,
    staleTime: 30_000,
  });

  // Per-user UI prefs that aren't persisted server-side (yet): muteAll, quietHours
  const [muteAll, setMuteAll] = useState(false);
  const [quietHoursEnabled, setQuietHoursEnabled] = useState(false);
  const [quietStart, setQuietStart] = useState("22:00");
  const [quietEnd, setQuietEnd] = useState("08:00");
  // Notification sound is a per-device preference (localStorage), not server-side.
  const [soundEnabled, setSoundEnabled] = useState(isNotificationSoundEnabled);

  // Hydrate the DND controls from the server ONCE (a later refetch must not
  // clobber an edit the user just made and we optimistically kept locally).
  const settingsHydrated = useRef(false);
  useEffect(() => {
    if (settingsHydrated.current || !data?.settings) return;
    settingsHydrated.current = true;
    const s = data.settings;
    /* eslint-disable react-hooks/set-state-in-effect -- one-time hydration of local
       edit-state from the server fetch; the ref guard prevents any re-sync. */
    setMuteAll(s.muted);
    setQuietHoursEnabled(s.quietHoursEnabled);
    if (s.quietStart) setQuietStart(s.quietStart);
    if (s.quietEnd) setQuietEnd(s.quietEnd);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, [data]);

  const settingsMut = useMutation({
    mutationFn: (s: NotificationSettings) => notificationsApi.updateSettings(s),
    onError: () => toast.error(t("notifications:preferences.saveError")),
  });

  // Persist the current DND state; `overrides` carries a just-changed value that
  // React's setState hasn't flushed into the closure yet.
  const persistSettings = (overrides?: Partial<NotificationSettings>) => {
    settingsMut.mutate({
      muted: overrides?.muted ?? muteAll,
      quietHoursEnabled: overrides?.quietHoursEnabled ?? quietHoursEnabled,
      quietStart: overrides?.quietStart ?? quietStart,
      quietEnd: overrides?.quietEnd ?? quietEnd,
      quietTimezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    });
  };

  const testMut = useMutation({
    mutationFn: () => notificationsApi.sendTest(),
    onSuccess: () => {
      toast.success(t("notifications:preferences.test.sent"));
      // Surface the freshly-created test notification in the bell + list.
      void qc.invalidateQueries({ queryKey: ["notifications"] });
    },
    onError: () => toast.error(t("notifications:preferences.saveError")),
  });

  const updateMut = useMutation({
    mutationFn: ({
      type,
      channel,
      isEnabled,
    }: Pick<NotificationPreference, "type" | "channel" | "isEnabled">) =>
      notificationsApi.updatePreference(type, channel, isEnabled),
    onMutate: async ({ type, channel, isEnabled }) => {
      await qc.cancelQueries({ queryKey: ["notifications", "preferences"] });
      const prev = qc.getQueryData<typeof data>(["notifications", "preferences"]);
      qc.setQueryData<typeof data>(
        ["notifications", "preferences"],
        (old) => {
          if (!old) return old;
          const updated = old.preferences.map((p) =>
            p.type === type && p.channel === channel ? { ...p, isEnabled } : p,
          );
          return { ...old, preferences: updated };
        },
      );
      return { prev };
    },
    onError: (_err, _vars, ctx) => {
      if (ctx?.prev) qc.setQueryData(["notifications", "preferences"], ctx.prev);
      toast.error(t("notifications:preferences.saveError"));
    },
    onSettled: () => {
      void qc.invalidateQueries({ queryKey: ["notifications", "preferences"] });
    },
  });

  // Derive groups + channels + prefMap from server data
  const { groups, channels, prefMap, channelEnabledByType } = useMemo(() => {
    const prefs = data?.preferences ?? [];
    const map = new Map<string, Record<string, boolean>>();
    for (const p of prefs) {
      if (!map.has(p.type)) map.set(p.type, {});
      map.get(p.type)![p.channel] = p.isEnabled;
    }
    const allChannels = [...new Set(prefs.map((p) => p.channel))].filter(
      (c) => c === "InApp" || c === "Email",
    );
    const types = [...new Set(prefs.map((p) => p.type))];
    const grouped = new Map<string, string[]>();
    for (const type of types) {
      // Free mode: drop genuinely money notifications entirely and never show a
      // payment-framed group. Non-money provider events (ratings, incoming review
      // requests) still fire, so keep their toggles — under a neutral group
      // instead of "Payments" (which groupOf() over-maps every ScholarshipProvider* to).
      if (!paymentsEnabled && isMoneyNotificationType(type)) continue;
      let g = groupOf(type);
      if (!paymentsEnabled && g === "Payments") g = "Other";
      if (!grouped.has(g)) grouped.set(g, []);
      grouped.get(g)!.push(type);
    }
    const ordered = GROUP_ORDER.filter((g) => grouped.has(g)).map(
      (g) => [g, grouped.get(g)!] as const,
    );

    // Per-row "any channel enabled?" helper for the master toggle per type
    const byType = new Map<string, boolean>();
    for (const type of types) {
      const prefsForType = map.get(type) ?? {};
      byType.set(
        type,
        allChannels.some((c) => prefsForType[c] ?? true),
      );
    }

    return {
      groups: ordered,
      channels: allChannels,
      prefMap: map,
      channelEnabledByType: byType,
    };
  }, [data, paymentsEnabled]);

  if (isLoading) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-10">
        <div className="skeleton mb-3 h-10 w-64" />
        <div className="skeleton mb-8 h-5 w-72" />
        <div className="space-y-4">
          {[0, 1, 2].map((i) => (
            <div key={i} className="skeleton h-48 w-full rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="mx-auto max-w-3xl space-y-4 px-4 py-10">
        <div className="card-premium border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("notifications:preferences.loadError")}
        </div>
        <Link
          to="/notifications"
          className="inline-flex items-center gap-1 text-sm text-brand-500 underline rtl:[&_svg]:rotate-180"
        >
          <ArrowLeft className="size-4" />
          {t("notifications:preferences.back")}
        </Link>
      </div>
    );
  }

  const toggleAllForType = (type: string, next: boolean) => {
    for (const ch of channels) {
      const current = prefMap.get(type)?.[ch] ?? true;
      if (current !== next) {
        updateMut.mutate({ type, channel: ch, isEnabled: next });
      }
    }
  };

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      {/* Back + title */}
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="mb-6"
      >
        <Link
          to="/notifications"
          className="mb-4 inline-flex items-center gap-1 text-sm text-text-secondary hover:text-text-primary rtl:[&_svg]:rotate-180"
        >
          <ArrowLeft className="size-4" />
          {t("notifications:preferences.back")}
        </Link>
        <div className="flex items-center gap-2">
          <span className="flex size-9 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
            <Bell aria-hidden className="size-5" />
          </span>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            {t("notifications:preferences.title")}
          </h1>
        </div>
        <p className="mt-2 text-sm text-text-secondary">
          {t("notifications:preferences.subtitle")}
        </p>
      </motion.div>

      {/* Master controls card */}
      <motion.section
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="card-premium mb-6 p-6"
      >
        <div className="grid gap-5 md:grid-cols-2 md:gap-8">
          {/* Mute all */}
          {/* Notification sound — a soft ping on new notifications (per-device). */}
          <div className="flex items-start gap-3">
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-bg-subtle text-text-secondary">
              <Volume2 aria-hidden className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-text-primary">
                    {t("notifications:preferences.sound.title")}
                  </p>
                  <p className="mt-0.5 text-xs text-text-secondary">
                    {t("notifications:preferences.sound.desc")}
                  </p>
                </div>
                <Toggle
                  checked={soundEnabled}
                  onChange={(v) => {
                    setSoundEnabled(v);
                    setNotificationSoundEnabled(v);
                    if (v) playNotificationChime();
                  }}
                  label={t("notifications:preferences.sound.title")}
                />
              </div>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-bg-subtle text-text-secondary">
              <VolumeX aria-hidden className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-text-primary">
                    {t("notifications:preferences.muteAll.title")}
                  </p>
                  <p className="mt-0.5 text-xs text-text-secondary">
                    {t("notifications:preferences.muteAll.desc")}
                  </p>
                </div>
                <Toggle
                  checked={muteAll}
                  onChange={(v) => {
                    setMuteAll(v);
                    persistSettings({ muted: v });
                  }}
                  label={t("notifications:preferences.muteAll.title")}
                />
              </div>
            </div>
          </div>

          {/* Quiet hours */}
          <div className="flex items-start gap-3">
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-bg-subtle text-text-secondary">
              <Moon aria-hidden className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-text-primary">
                    {t("notifications:preferences.quietHours.title")}
                  </p>
                  <p className="mt-0.5 text-xs text-text-secondary">
                    {t("notifications:preferences.quietHours.desc")}
                  </p>
                </div>
                <Toggle
                  checked={quietHoursEnabled}
                  onChange={(v) => {
                    setQuietHoursEnabled(v);
                    persistSettings({ quietHoursEnabled: v });
                  }}
                  label={t("notifications:preferences.quietHours.title")}
                />
              </div>
              {quietHoursEnabled && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
                  className="mt-3 grid grid-cols-2 gap-2 overflow-hidden"
                >
                  <label className="block text-xs text-text-secondary">
                    {t("notifications:preferences.quietHours.from")}
                    <input
                      type="time"
                      value={quietStart}
                      onChange={(e) => setQuietStart(e.target.value)}
                      onBlur={() => persistSettings()}
                      className="input-premium mt-1 text-sm"
                    />
                  </label>
                  <label className="block text-xs text-text-secondary">
                    {t("notifications:preferences.quietHours.to")}
                    <input
                      type="time"
                      value={quietEnd}
                      onChange={(e) => setQuietEnd(e.target.value)}
                      onBlur={() => persistSettings()}
                      className="input-premium mt-1 text-sm"
                    />
                  </label>
                </motion.div>
              )}
            </div>
          </div>
        </div>

        {/* Test notification */}
        <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-border-subtle pt-5">
          <div>
            <p className="text-sm font-semibold text-text-primary">
              {t("notifications:preferences.test.title")}
            </p>
            <p className="mt-0.5 text-xs text-text-secondary">
              {t("notifications:preferences.test.desc")}
            </p>
          </div>
          <button
            type="button"
            onClick={() => testMut.mutate()}
            disabled={testMut.isPending}
            className="btn btn-secondary btn-sm disabled:opacity-50"
          >
            <TestTube aria-hidden className="size-3.5" />
            {t("notifications:preferences.test.send")}
          </button>
        </div>
      </motion.section>

      {/* Groups */}
      <div className="space-y-6">
        {groups.map(([group, groupTypes]) => {
          const meta = GROUP_META[group] ?? GROUP_META.Other;
          return (
            <motion.section
              key={group}
              initial={{ opacity: 0, y: 8 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: "-80px" }}
              transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
              className="card-premium overflow-hidden"
            >
              <div className="flex items-start gap-3 border-b border-border-subtle p-5">
                <span
                  className={cn(
                    "mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg",
                    TONE_CLASSES[meta.tone],
                  )}
                >
                  {meta.icon}
                </span>
                <div>
                  <h2 className="text-base font-semibold text-text-primary">
                    {t(`notifications:preferences.groups.${group}`, {
                      defaultValue: group,
                    })}
                  </h2>
                  <p className="mt-0.5 text-xs text-text-secondary">
                    {t(`notifications:preferences.groupDesc.${group}`, {
                      defaultValue: "",
                    })}
                  </p>
                </div>
              </div>

              {/* Header row */}
              <div
                className="hidden bg-bg-muted/60 px-5 py-2.5 sm:grid"
                style={{
                  gridTemplateColumns: `1fr repeat(${channels.length + 1}, 5.5rem)`,
                }}
              >
                <span className="text-[11px] font-medium uppercase tracking-wide text-text-tertiary">
                  {t("notifications:preferences.eventColumn")}
                </span>
                {channels.map((ch) => {
                  const m = CHANNEL_META[ch];
                  return (
                    <div
                      key={ch}
                      className="flex items-center justify-center gap-1 text-[11px] font-medium uppercase tracking-wide text-text-tertiary"
                    >
                      {m?.icon}
                      {t(`notifications:preferences.${m?.key ?? "channel" + ch}`, {
                        defaultValue: ch,
                      })}
                    </div>
                  );
                })}
                <span className="text-center text-[11px] font-medium uppercase tracking-wide text-text-tertiary">
                  {t("notifications:preferences.allColumn")}
                </span>
              </div>

              <div className="divide-y divide-border-subtle">
                {groupTypes.map((type) => {
                  const prefs = prefMap.get(type) ?? {};
                  const anyEnabled = channelEnabledByType.get(type) ?? false;
                  return (
                    <div
                      key={type}
                      className="grid grid-cols-1 gap-2 px-5 py-3.5 sm:items-center sm:gap-3"
                      style={{
                        gridTemplateColumns: undefined,
                      }}
                    >
                      <div
                        className="grid sm:items-center"
                        style={{
                          gridTemplateColumns: `1fr repeat(${channels.length + 1}, 5.5rem)`,
                        }}
                      >
                        <span className="pe-2 text-sm font-medium text-text-primary">
                          {t(`notifications:preferences.types.${type}`, {
                            defaultValue: type,
                          })}
                        </span>
                        {channels.map((ch) => {
                          const enabled = prefs[ch] ?? true;
                          return (
                            <div
                              key={ch}
                              className="flex justify-center"
                            >
                              <Toggle
                                size="sm"
                                checked={enabled && !muteAll}
                                disabled={updateMut.isPending || muteAll}
                                label={`${type} ${ch}`}
                                onChange={(next) =>
                                  updateMut.mutate({
                                    type,
                                    channel: ch,
                                    isEnabled: next,
                                  })
                                }
                              />
                            </div>
                          );
                        })}
                        <div className="flex justify-center">
                          <button
                            type="button"
                            disabled={updateMut.isPending || muteAll}
                            onClick={() => toggleAllForType(type, !anyEnabled)}
                            className={cn(
                              "rounded-full px-2 py-0.5 text-xs font-semibold transition-colors",
                              anyEnabled && !muteAll
                                ? "text-text-secondary hover:text-text-primary"
                                : "text-brand-600 hover:underline",
                              "disabled:opacity-50",
                            )}
                          >
                            {anyEnabled && !muteAll
                              ? t("notifications:preferences.muteRow")
                              : t("notifications:preferences.enableRow")}
                          </button>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </motion.section>
          );
        })}
      </div>

      {/* Footer hint */}
      <div className="mt-8 rounded-xl border border-border-subtle bg-bg-muted p-4 text-xs text-text-secondary">
        <div className="flex items-start gap-2">
          <Send aria-hidden className="mt-0.5 size-3.5 text-text-tertiary" />
          <p>{t("notifications:preferences.footerHint")}</p>
        </div>
      </div>

      {updateMut.isPending && (
        <div className="fixed bottom-6 end-6 z-30 inline-flex items-center gap-2 rounded-full border border-border-default bg-bg-elevated px-3 py-1.5 text-xs text-text-secondary shadow-elevation-2">
          <Loader2 aria-hidden className="size-3.5 animate-spin" />
          {t("notifications:preferences.saving")}
        </div>
      )}
    </div>
  );
}
