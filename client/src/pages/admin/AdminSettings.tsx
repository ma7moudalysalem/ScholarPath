import { useEffect, useMemo, useState, type ReactNode } from "react";
import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { ar } from "date-fns/locale";
import { motion } from "motion/react";
import {
  AlertTriangle,
  Bot,
  CheckCircle2,
  Clock,
  DollarSign,
  History,
  KeyRound,
  Loader2,
  Mail,
  Settings as SettingsIcon,
  Shield,
  Sliders,
  Wrench,
  X,
} from "lucide-react";
import {
  settingsApi,
  type PlatformSettingDto,
} from "@/services/api/settings";
import { cn } from "@/lib/utils";

const SETTINGS_QUERY_KEY = ["admin", "platform-settings"];

// Icons per category — keys are server-provided categories, with sensible fallbacks.
const CATEGORY_ICONS: Record<string, ReactNode> = {
  General: <Sliders aria-hidden className="size-5" />,
  Access: <Shield aria-hidden className="size-5" />,
  Payments: <DollarSign aria-hidden className="size-5" />,
  AI: <Bot aria-hidden className="size-5" />,
  Email: <Mail aria-hidden className="size-5" />,
  Maintenance: <Wrench aria-hidden className="size-5" />,
  Support: <KeyRound aria-hidden className="size-5" />,
};

const CATEGORY_FALLBACK_ICON = <SettingsIcon aria-hidden className="size-5" />;

// Local draft per setting key — tracked so we can show a global save bar.
interface DraftMap {
  [settingKey: string]: string;
}

export function AdminSettings() {
  const { t, i18n } = useTranslation(["settings", "common"]);
  const qc = useQueryClient();

  const query = useQuery<PlatformSettingDto[]>({
    queryKey: SETTINGS_QUERY_KEY,
    queryFn: () => settingsApi.getSettings(),
  });

  const settings = query.data ?? [];
  const groups = useMemo(() => groupByCategory(settings), [settings]);

  // Draft state across all settings, keyed by setting key.
  const [drafts, setDrafts] = useState<DraftMap>({});

  // Compute dirty rows from drafts vs canonical values.
  const dirtyKeys = useMemo(() => {
    const keys: string[] = [];
    for (const s of settings) {
      const draft = drafts[s.key];
      if (draft !== undefined && draft !== s.value) keys.push(s.key);
    }
    return keys;
  }, [drafts, settings]);

  const dirtyCount = dirtyKeys.length;

  const updateMut = useMutation({
    mutationFn: (payload: { key: string; value: string }) =>
      settingsApi.updateSetting(payload.key, payload.value),
  });

  // Save all dirty rows in parallel; show one toast for the whole batch.
  const [saving, setSaving] = useState(false);
  const saveAll = async () => {
    if (dirtyKeys.length === 0) return;
    setSaving(true);
    try {
      const results = await Promise.allSettled(
        dirtyKeys.map((key) =>
          updateMut.mutateAsync({ key, value: drafts[key]! }),
        ),
      );
      const failed = results.filter((r) => r.status === "rejected").length;
      const ok = results.length - failed;
      void qc.invalidateQueries({ queryKey: SETTINGS_QUERY_KEY });
      if (failed === 0) toast.success(t("settings:saveAllSuccess", { count: ok }));
      else if (ok > 0)
        toast.warning(t("settings:savePartial", { ok, failed }));
      else toast.error(t("settings:saveError"));
      // Clear successfully saved drafts; failed ones stay dirty
      setDrafts((prev) => {
        const next: DraftMap = { ...prev };
        results.forEach((r, i) => {
          if (r.status === "fulfilled") delete next[dirtyKeys[i]];
        });
        return next;
      });
    } finally {
      setSaving(false);
    }
  };

  const discardAll = () => setDrafts({});

  // Sticky sidebar active section tracking
  const [activeSection, setActiveSection] = useState<string | null>(null);
  useEffect(() => {
    if (groups.length === 0) return;
    const targets = groups
      .map(([cat]) => document.getElementById(`cat-${cat}`))
      .filter((el): el is HTMLElement => el !== null);
    if (targets.length === 0) return;
    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((e) => e.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible[0]) setActiveSection(visible[0].target.id);
      },
      { rootMargin: "-30% 0px -55% 0px", threshold: 0 },
    );
    targets.forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, [groups]);

  return (
    <div className="space-y-6">
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
        className="flex flex-wrap items-end justify-between gap-3"
      >
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            {t("settings:title")}
          </h1>
          <p className="mt-1 text-sm text-text-secondary">
            {t("settings:subtitle")}
          </p>
        </div>
        <a
          href="/admin/audit-logs"
          className="btn btn-secondary btn-sm"
        >
          <History aria-hidden className="size-3.5" />
          {t("settings:viewHistory")}
        </a>
      </motion.div>

      {query.isLoading && (
        <div className="flex items-center gap-2 rounded-xl border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          <Loader2 className="size-4 animate-spin" />
          {t("settings:loading")}
        </div>
      )}

      {query.isError && !query.isLoading && (
        <div className="rounded-xl border border-danger-200 bg-danger-50 p-6 text-sm text-danger-500">
          {t("settings:loadError")}
        </div>
      )}

      {!query.isLoading && !query.isError && groups.length === 0 && (
        <div className="rounded-xl border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          {t("settings:empty")}
        </div>
      )}

      {groups.length > 0 && (
        <div className="grid gap-8 lg:grid-cols-[200px_minmax(0,1fr)]">
          {/* Sidebar */}
          <nav className="hidden lg:sticky lg:top-24 lg:block">
            <ul className="space-y-1">
              {groups.map(([category]) => {
                const id = `cat-${category}`;
                const active = activeSection === id;
                const draftsInCat = settings
                  .filter((s) => s.category === category)
                  .some((s) => dirtyKeys.includes(s.key));
                return (
                  <li key={category}>
                    <button
                      type="button"
                      onClick={() => {
                        document
                          .getElementById(id)
                          ?.scrollIntoView({ behavior: "smooth", block: "start" });
                      }}
                      className={cn(
                        "group flex w-full items-center gap-2 rounded-lg px-3 py-2 text-start text-sm transition-colors",
                        active
                          ? "bg-brand-50 font-semibold text-brand-700"
                          : "text-text-secondary hover:bg-bg-subtle hover:text-text-primary",
                      )}
                    >
                      <span
                        className={cn(
                          "flex size-7 items-center justify-center rounded-md transition-colors",
                          active
                            ? "bg-brand-100 text-brand-600"
                            : "bg-bg-subtle text-text-tertiary group-hover:text-text-secondary",
                        )}
                      >
                        {CATEGORY_ICONS[category] ?? CATEGORY_FALLBACK_ICON}
                      </span>
                      <span className="flex-1 truncate">
                        {t(`settings:categories.${category}`, {
                          defaultValue: category,
                        })}
                      </span>
                      {draftsInCat && (
                        <span
                          aria-hidden
                          className="size-1.5 rounded-full bg-warning-500"
                        />
                      )}
                    </button>
                  </li>
                );
              })}
            </ul>
          </nav>

          {/* Content */}
          <div className="space-y-6">
            {groups.map(([category, rows]) => (
              <CategoryCard
                key={category}
                id={`cat-${category}`}
                title={t(`settings:categories.${category}`, {
                  defaultValue: category,
                })}
                description={t(`settings:categoryDesc.${category}`, {
                  defaultValue: "",
                })}
                icon={CATEGORY_ICONS[category] ?? CATEGORY_FALLBACK_ICON}
              >
                {rows.map((setting, idx) => (
                  <div
                    key={setting.id}
                    className={cn(
                      "py-5",
                      idx > 0 && "border-t border-border-subtle",
                    )}
                  >
                    <SettingRow
                      setting={setting}
                      draft={drafts[setting.key]}
                      onChange={(v) =>
                        setDrafts((prev) => ({ ...prev, [setting.key]: v }))
                      }
                      onDiscard={() =>
                        setDrafts((prev) => {
                          const next = { ...prev };
                          delete next[setting.key];
                          return next;
                        })
                      }
                      locale={i18n.language}
                    />
                  </div>
                ))}
              </CategoryCard>
            ))}
          </div>
        </div>
      )}

      {/* Sticky save bar */}
      {dirtyCount > 0 && (
        <motion.div
          initial={{ y: 24, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 24, opacity: 0 }}
          transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
          className="sticky bottom-4 z-30 mx-auto flex max-w-3xl items-center justify-between gap-3 rounded-2xl border border-border-default bg-bg-elevated/95 p-3 pl-5 shadow-elevation-3 backdrop-blur-md"
        >
          <div className="flex items-center gap-2 text-sm">
            <span className="flex size-6 items-center justify-center rounded-full bg-warning-500/10 text-warning-600">
              <AlertTriangle className="size-3.5" />
            </span>
            <span className="font-medium text-text-primary">
              {t("settings:saveBar.pending", { count: dirtyCount })}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={discardAll}
              disabled={saving}
              className="btn btn-ghost btn-sm"
            >
              <X aria-hidden className="size-3.5" />
              {t("settings:saveBar.discardAll")}
            </button>
            <button
              type="button"
              onClick={saveAll}
              disabled={saving}
              className="btn btn-primary btn-sm"
            >
              {saving ? (
                <Loader2 aria-hidden className="size-3.5 animate-spin" />
              ) : (
                <CheckCircle2 aria-hidden className="size-3.5" />
              )}
              {saving
                ? t("settings:saving")
                : t("settings:saveBar.saveAll")}
            </button>
          </div>
        </motion.div>
      )}
    </div>
  );
}

function CategoryCard({
  id,
  icon,
  title,
  description,
  children,
}: {
  id: string;
  icon: ReactNode;
  title: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <motion.section
      id={id}
      initial={{ opacity: 0, y: 8 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.32, ease: [0.22, 1, 0.36, 1] }}
      className="card-premium scroll-mt-24 p-6 sm:p-8"
    >
      <div className="mb-4 flex items-start gap-3">
        <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
          {icon}
        </span>
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-text-primary">{title}</h2>
          {description && (
            <p className="mt-0.5 text-sm text-text-secondary">{description}</p>
          )}
        </div>
      </div>
      <div>{children}</div>
    </motion.section>
  );
}

function groupByCategory(
  settings: PlatformSettingDto[],
): [string, PlatformSettingDto[]][] {
  const map = new Map<string, PlatformSettingDto[]>();
  for (const setting of settings) {
    const bucket = map.get(setting.category) ?? [];
    bucket.push(setting);
    map.set(setting.category, bucket);
  }
  return [...map.entries()];
}

function SettingRow({
  setting,
  draft,
  onChange,
  onDiscard,
  locale,
}: {
  setting: PlatformSettingDto;
  draft: string | undefined;
  onChange: (next: string) => void;
  onDiscard: () => void;
  locale: string;
}) {
  const { t } = useTranslation(["settings", "common"]);

  const value = draft ?? setting.value;
  const dirty = draft !== undefined && draft !== setting.value;
  const isArabic = locale.startsWith("ar");
  const description = isArabic ? setting.descriptionAr : setting.descriptionEn;
  const dateLocale = isArabic ? ar : undefined;

  return (
    <div className="grid gap-3 md:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)] md:items-start md:gap-8">
      <div className="md:pt-2">
        <div className="flex items-center gap-2">
          <p className="font-mono text-sm font-semibold text-text-primary">
            {setting.key}
          </p>
          {dirty && (
            <span className="badge badge-warning text-[10px]">
              {t("settings:row.unsaved")}
            </span>
          )}
        </div>
        <p className="mt-1 text-sm text-text-secondary">
          {description ?? t("settings:noDescription")}
        </p>
        <p className="mt-1 inline-flex items-center gap-1 text-xs text-text-tertiary">
          <Clock aria-hidden className="size-3" />
          {setting.updatedAt
            ? t("settings:lastUpdated", {
                date: format(new Date(setting.updatedAt), "yyyy-MM-dd", {
                  locale: dateLocale,
                }),
              })
            : t("settings:neverEdited")}
        </p>
      </div>
      <div className="flex flex-col items-stretch gap-2">
        <SettingInput
          setting={setting}
          value={value}
          onChange={onChange}
        />
        {dirty && (
          <button
            type="button"
            onClick={onDiscard}
            className="self-end text-xs font-medium text-text-tertiary hover:text-text-primary"
          >
            {t("settings:row.discard")}
          </button>
        )}
      </div>
    </div>
  );
}

function SettingInput({
  setting,
  value,
  onChange,
}: {
  setting: PlatformSettingDto;
  value: string;
  onChange: (next: string) => void;
}) {
  const { t } = useTranslation(["settings"]);

  if (setting.valueType === "Boolean") {
    const enabled = value === "true";
    return (
      <div className="flex items-center justify-between gap-3 rounded-lg border border-border-subtle bg-bg-muted px-3 py-2">
        <span className="text-sm text-text-secondary">
          {enabled ? t("settings:enabled") : t("settings:disabled")}
        </span>
        <Switch
          checked={enabled}
          onChange={(next) => onChange(next ? "true" : "false")}
          label={setting.key}
        />
      </div>
    );
  }

  if (setting.valueType === "Number") {
    return (
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="input-premium font-mono text-sm"
      />
    );
  }

  if (value.length > 60) {
    return (
      <textarea
        rows={3}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="input-premium font-mono text-sm"
      />
    );
  }

  return (
    <input
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="input-premium font-mono text-sm"
    />
  );
}

// Premium toggle switch
function Switch({
  checked,
  onChange,
  label,
  disabled,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  disabled?: boolean;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full transition-colors",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-offset-2",
        "disabled:cursor-not-allowed disabled:opacity-50",
        checked
          ? "bg-gradient-to-r from-brand-500 to-brand-600 shadow-[var(--shadow-brand-sm)]"
          : "bg-border-default",
      )}
    >
      <span
        className={cn(
          "pointer-events-none inline-block size-5 rounded-full bg-white shadow-sm transition-transform",
          checked ? "translate-x-5 rtl:-translate-x-5" : "translate-x-0.5",
        )}
      />
    </button>
  );
}
