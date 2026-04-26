import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Send } from "lucide-react";
import { adminApi } from "@/services/api/admin";

export function BroadcastComposer() {
  const { t } = useTranslation(["admin", "common"]);
  const [titleEn, setTitleEn] = useState("");
  const [titleAr, setTitleAr] = useState("");
  const [bodyEn, setBodyEn] = useState("");
  const [bodyAr, setBodyAr] = useState("");

  const mut = useMutation({
    mutationFn: () => adminApi.sendBroadcast({ titleEn, titleAr, bodyEn, bodyAr, targetRole: null }),
    onSuccess: (res) => {
      toast.success(t("admin:broadcast.success", { count: res.recipientCount }));
      setTitleEn(""); setTitleAr(""); setBodyEn(""); setBodyAr("");
    },
    onError: () => toast.error(t("common:status.error")),
  });

  const canSend = titleEn.trim() && titleAr.trim() && bodyEn.trim() && bodyAr.trim();

  return (
    <div className="mx-auto max-w-2xl space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin:broadcast.title")}</h1>
        <p className="mt-1 text-sm text-text-secondary">{t("admin:broadcast.subtitle")}</p>
      </div>

      <form
        onSubmit={(e) => { e.preventDefault(); if (canSend) mut.mutate(); }}
        className="space-y-4 rounded-lg border border-border-subtle bg-bg-elevated p-5"
      >
        <div className="grid gap-4 md:grid-cols-2">
          <label className="space-y-1 text-sm">
            <span className="text-text-secondary">{t("admin:broadcast.titleEn")}</span>
            <input
              type="text"
              value={titleEn}
              onChange={(e) => setTitleEn(e.target.value)}
              maxLength={160}
              className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </label>
          <label className="space-y-1 text-sm">
            <span className="text-text-secondary">{t("admin:broadcast.titleAr")}</span>
            <input
              type="text"
              value={titleAr}
              onChange={(e) => setTitleAr(e.target.value)}
              maxLength={160}
              dir="rtl"
              className="h-10 w-full rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </label>
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <label className="space-y-1 text-sm">
            <span className="text-text-secondary">{t("admin:broadcast.bodyEn")}</span>
            <textarea
              value={bodyEn}
              onChange={(e) => setBodyEn(e.target.value)}
              maxLength={2000}
              rows={6}
              className="w-full rounded-md border border-border-subtle bg-bg-canvas p-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </label>
          <label className="space-y-1 text-sm">
            <span className="text-text-secondary">{t("admin:broadcast.bodyAr")}</span>
            <textarea
              value={bodyAr}
              onChange={(e) => setBodyAr(e.target.value)}
              maxLength={2000}
              rows={6}
              dir="rtl"
              className="w-full rounded-md border border-border-subtle bg-bg-canvas p-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
            />
          </label>
        </div>

        <button
          type="submit"
          disabled={!canSend || mut.isPending}
          className="inline-flex items-center gap-2 rounded-md bg-brand-500 px-4 py-2 text-sm font-medium text-text-on-brand shadow-sm transition hover:bg-brand-600 disabled:opacity-50"
        >
          <Send aria-hidden className="size-4" />
          {t("admin:broadcast.submit")}
        </button>
      </form>
    </div>
  );
}
