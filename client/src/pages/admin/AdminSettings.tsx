import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { format } from "date-fns";
import { Loader2 } from "lucide-react";
import {
  settingsApi,
  type PlatformSettingDto,
} from "@/services/api/settings";

const SETTINGS_QUERY_KEY = ["admin", "platform-settings"];

export function AdminSettings() {
  const { t } = useTranslation(["settings", "common"]);

  const query = useQuery<PlatformSettingDto[]>({
    queryKey: SETTINGS_QUERY_KEY,
    queryFn: () => settingsApi.getSettings(),
  });

  // Group settings into category buckets, preserving the server's sort order.
  const groups = groupByCategory(query.data ?? []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-text-primary">
          {t("settings:title")}
        </h1>
        <p className="mt-1 text-sm text-text-secondary">{t("settings:subtitle")}</p>
      </div>

      {query.isLoading && (
        <div className="flex items-center gap-2 rounded-lg border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          <Loader2 className="size-4 animate-spin" />
          {t("settings:loading")}
        </div>
      )}

      {query.isError && !query.isLoading && (
        <div className="rounded-lg border border-border-subtle bg-bg-elevated p-6 text-sm text-red-500">
          {t("settings:loadError")}
        </div>
      )}

      {!query.isLoading && !query.isError && groups.length === 0 && (
        <div className="rounded-lg border border-border-subtle bg-bg-elevated p-6 text-sm text-text-tertiary">
          {t("settings:empty")}
        </div>
      )}

      <div className="space-y-5">
        {groups.map(([category, settings]) => (
          <section
            key={category}
            className="rounded-lg border border-border-subtle bg-bg-elevated p-5"
          >
            <h2 className="mb-4 text-lg font-semibold text-text-primary">
              {t(`settings:categories.${category}`, { defaultValue: category })}
            </h2>
            <div className="divide-y divide-border-subtle">
              {settings.map((setting) => (
                <SettingRow key={setting.id} setting={setting} />
              ))}
            </div>
          </section>
        ))}
      </div>
    </div>
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

function SettingRow({ setting }: { setting: PlatformSettingDto }) {
  const { t, i18n } = useTranslation(["settings", "common"]);
  const qc = useQueryClient();

  // Local draft of the value — initialised from the server, and re-synced when
  // a refetch brings a newer value (adjusting state during render, not in an
  // effect).
  const [draft, setDraft] = useState(setting.value);
  const [syncedValue, setSyncedValue] = useState(setting.value);
  if (syncedValue !== setting.value) {
    setSyncedValue(setting.value);
    setDraft(setting.value);
  }

  const mut = useMutation({
    mutationFn: (value: string) => settingsApi.updateSetting(setting.key, value),
    onSuccess: () => {
      toast.success(t("settings:saveSuccess"));
      void qc.invalidateQueries({ queryKey: SETTINGS_QUERY_KEY });
    },
    onError: () => toast.error(t("settings:saveError")),
  });

  const isArabic = i18n.language.startsWith("ar");
  const description = isArabic ? setting.descriptionAr : setting.descriptionEn;
  const dirty = draft !== setting.value;

  return (
    <div className="flex flex-col gap-3 py-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="min-w-0 sm:me-6">
        <p className="font-medium text-text-primary">{setting.key}</p>
        <p className="mt-0.5 text-sm text-text-secondary">
          {description ?? t("settings:noDescription")}
        </p>
        <p className="mt-1 text-xs text-text-tertiary">
          {setting.updatedAt
            ? t("settings:lastUpdated", {
                date: format(new Date(setting.updatedAt), "yyyy-MM-dd"),
              })
            : t("settings:neverEdited")}
        </p>
      </div>

      <div className="flex shrink-0 items-center gap-3">
        <SettingInput setting={setting} value={draft} onChange={setDraft} />
        <button
          type="button"
          onClick={() => mut.mutate(draft)}
          disabled={mut.isPending || !dirty}
          className="inline-flex items-center justify-center rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
        >
          {mut.isPending ? t("settings:saving") : t("settings:save")}
        </button>
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
      <label className="inline-flex cursor-pointer items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(e) => onChange(e.target.checked ? "true" : "false")}
          className="size-4 rounded border-border-subtle accent-brand-500"
        />
        <span className="text-text-secondary">
          {enabled ? t("settings:enabled") : t("settings:disabled")}
        </span>
      </label>
    );
  }

  if (setting.valueType === "Number") {
    return (
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="h-10 w-40 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary focus:border-brand-500 focus:outline-none"
      />
    );
  }

  return (
    <input
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="h-10 w-64 rounded-md border border-border-subtle bg-bg-elevated px-3 text-sm text-text-primary focus:border-brand-500 focus:outline-none"
    />
  );
}
