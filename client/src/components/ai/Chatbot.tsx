import { useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { Send, MessageSquare } from "lucide-react";
import { aiApi, type ChatAnswerDto } from "@/services/api/ai";
import { AiDisclaimer } from "./AiDisclaimer";

interface Turn {
  role: "user" | "assistant";
  text: string;
  cost?: number;
  prompt?: number;
  completion?: number;
}

export function Chatbot() {
  const { t } = useTranslation(["ai", "common"]);
  const [turns, setTurns] = useState<Turn[]>([]);
  const [draft, setDraft] = useState("");
  const [sessionId, setSessionId] = useState<string | undefined>(undefined);
  const scrollRef = useRef<HTMLDivElement>(null);

  const mut = useMutation({
    mutationFn: (msg: string) => aiApi.chat(msg, sessionId),
    onSuccess: (dto: ChatAnswerDto) => {
      setSessionId(dto.sessionId);
      setTurns((prev) => [
        ...prev,
        { role: "assistant", text: dto.message, cost: dto.estimatedCostUsd, prompt: dto.promptTokens, completion: dto.completionTokens },
      ]);
      // Scroll to bottom after the DOM updates
      queueMicrotask(() => scrollRef.current?.scrollTo({ top: 1e9, behavior: "smooth" }));
    },
    onError: (err: { status?: number }) => {
      if (err.status === 409) toast.warning(t("ai:errors.budgetExceeded"));
      else toast.error(t("ai:errors.generic"));
    },
  });

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const msg = draft.trim();
    if (!msg || mut.isPending) return;
    setTurns((prev) => [...prev, { role: "user", text: msg }]);
    setDraft("");
    mut.mutate(msg);
  };

  return (
    <section className="flex flex-col overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
      <header className="flex items-center gap-2 border-b border-border-subtle px-4 py-3">
        <MessageSquare aria-hidden className="size-5 text-brand-500" />
        <h2 className="font-semibold">{t("ai:chat.heading")}</h2>
      </header>

      <div
        ref={scrollRef}
        className="flex-1 space-y-3 overflow-y-auto p-4"
        style={{ minHeight: 320, maxHeight: 480 }}
      >
        {turns.length === 0 && (
          <p className="text-sm text-text-tertiary">{t("ai:chat.emptyHint")}</p>
        )}
        {turns.map((turn, i) => (
          <div key={i} className={`flex ${turn.role === "user" ? "justify-end" : "justify-start"}`}>
            <div
              className={`max-w-[80%] rounded-lg px-3 py-2 text-sm ${
                turn.role === "user"
                  ? "bg-brand-500 text-text-on-brand"
                  : "border border-border-subtle bg-bg-canvas text-text-primary"
              }`}
            >
              <div className="text-[10px] font-semibold uppercase tracking-wide opacity-75">
                {t(turn.role === "user" ? "ai:chat.you" : "ai:chat.assistant")}
              </div>
              <p className="mt-0.5 whitespace-pre-wrap">{turn.text}</p>
              {turn.role === "assistant" && turn.cost != null && (
                <div className="mt-1 text-[10px] tabular-nums opacity-60">
                  {t("ai:chat.costHint", {
                    cost: turn.cost.toFixed(4),
                    prompt: turn.prompt ?? 0,
                    completion: turn.completion ?? 0,
                  })}
                </div>
              )}
            </div>
          </div>
        ))}
        {mut.isPending && (
          <div className="flex justify-start">
            <div className="rounded-lg border border-border-subtle bg-bg-canvas px-3 py-2 text-sm text-text-tertiary">
              …
            </div>
          </div>
        )}
      </div>

      <form onSubmit={onSubmit} className="flex items-center gap-2 border-t border-border-subtle p-3">
        <input
          type="text"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder={t("ai:chat.placeholder")}
          maxLength={2000}
          className="h-10 flex-1 rounded-md border border-border-subtle bg-bg-canvas px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        />
        <button
          type="submit"
          disabled={!draft.trim() || mut.isPending}
          className="inline-flex items-center gap-1.5 rounded-md bg-brand-500 px-3 py-2 text-sm font-medium text-text-on-brand transition hover:bg-brand-600 disabled:opacity-50"
        >
          <Send aria-hidden className="size-4" />
          {t("ai:chat.send")}
        </button>
      </form>

      <div className="border-t border-border-subtle bg-bg-subtle/40 px-4 py-2">
        <AiDisclaimer />
      </div>
    </section>
  );
}
