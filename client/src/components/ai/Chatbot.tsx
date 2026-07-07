import { useEffect, useImperativeHandle, useMemo, useRef, useState, forwardRef } from "react";
import { Link } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  Send,
  FileText,
  Plus,
  Loader2,
  Sparkles,
  GraduationCap,
  Calendar,
  ClipboardCheck,
  PenSquare,
} from "lucide-react";
import { motion, AnimatePresence } from "motion/react";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import {
  aiApi,
  type AiSessionSummary,
  type AiSessionTurn,
  type ChatAnswerDto,
  type ChatSourceDto,
} from "@/services/api/ai";
import { apiErrorMessage } from "@/services/api/client";
import { AiDisclaimer } from "./AiDisclaimer";

interface Turn {
  role: "user" | "assistant";
  text: string;
  cost?: number;
  prompt?: number;
  completion?: number;
  sources?: ChatSourceDto[];
}

/**
 * Persists the current chat session id across hard refreshes so the user
 * doesn't lose their place mid-conversation. Cleared with "New chat".
 */
const SESSION_STORAGE_KEY = "scholarpath.aiChat.sessionId";

function readPersistedSessionId(): string | undefined {
  if (typeof window === "undefined") return undefined;
  const raw = window.localStorage.getItem(SESSION_STORAGE_KEY);
  return raw && raw.trim().length > 0 ? raw : undefined;
}

function persistSessionId(id: string | undefined) {
  if (typeof window === "undefined") return;
  if (id) window.localStorage.setItem(SESSION_STORAGE_KEY, id);
  else window.localStorage.removeItem(SESSION_STORAGE_KEY);
}

/**
 * Imperative handle so the surrounding page (e.g. AiFeatures Quick Actions)
 * can prefill the chat input with a starter prompt without auto-sending.
 * The user sees the text in the textbox and can edit before hitting send.
 */
export interface ChatbotHandle {
  prefillDraft: (text: string) => void;
}

export const Chatbot = forwardRef<ChatbotHandle>(function Chatbot(_, ref) {
  const { t, i18n } = useTranslation(["ai", "common"]);
  const dateLocale = i18n.language.startsWith("ar") ? ar : undefined;
  const qc = useQueryClient();

  // Optimistic turns added during this render — extended on send, cleared on
  // session switch / new-chat. The "real" history comes from the React Query
  // cache (turnsQuery.data) so we don't need an effect+setState to hydrate.
  const [pendingTurns, setPendingTurns] = useState<Turn[]>([]);
  const [draft, setDraft] = useState("");
  const [sessionId, setSessionId] = useState<string | undefined>(() =>
    readPersistedSessionId(),
  );
  const scrollRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Expose a prefill method so external Quick Action buttons can stage a
  // starter prompt in the input without auto-sending. Focus + caret-to-end
  // makes the next keypress edit the prompt naturally.
  useImperativeHandle(ref, () => ({
    prefillDraft: (text: string) => {
      setDraft(text);
      requestAnimationFrame(() => {
        const el = inputRef.current;
        if (!el) return;
        el.focus();
        el.setSelectionRange(text.length, text.length);
      });
    },
  }), []);

  // ── Sessions sidebar ──────────────────────────────────────────────────────

  const sessionsQuery = useQuery<AiSessionSummary[]>({
    queryKey: ["ai", "chat", "sessions"],
    queryFn: () => aiApi.chatSessions(),
  });

  // Load past turns of the persisted-or-selected session on mount + on switch.
  // Disabled when there's no session (a fresh "New chat" starts blank).
  const turnsQuery = useQuery<AiSessionTurn[]>({
    queryKey: ["ai", "chat", "session", sessionId],
    queryFn: () => aiApi.chatSessionTurns(sessionId!),
    enabled: !!sessionId,
  });

  // Derive the persisted turns from the React Query cache — no state copy,
  // no hydration effect (which the react-hooks lint rule discourages). The
  // pane displays [serverTurns, ...pendingTurns] so an in-flight optimistic
  // message appears immediately and the assistant reply lands when the cache
  // refresh comes back.
  const serverTurns = useMemo<Turn[]>(() => {
    const rows = turnsQuery.data;
    if (!sessionId || !rows) return [];
    const replayed: Turn[] = [];
    for (const r of rows) {
      if (r.promptText) replayed.push({ role: "user", text: r.promptText });
      if (r.responseText) {
        replayed.push({
          role: "assistant",
          text: r.responseText,
          cost: r.costUsd ?? undefined,
          prompt: r.promptTokens ?? undefined,
          completion: r.completionTokens ?? undefined,
        });
      }
    }
    return replayed;
  }, [sessionId, turnsQuery.data]);

  const turns = useMemo(
    () => [...serverTurns, ...pendingTurns],
    [serverTurns, pendingTurns],
  );

  // Always-scroll-to-bottom on new content.
  useEffect(() => {
    queueMicrotask(() => scrollRef.current?.scrollTo({ top: 1e9, behavior: "smooth" }));
  }, [turns.length]);

  // ── Mutations ─────────────────────────────────────────────────────────────

  const mut = useMutation({
    mutationFn: (msg: string) => aiApi.chat(msg, sessionId),
    onSuccess: (dto: ChatAnswerDto) => {
      // Optimistically append the assistant reply so the user sees it
      // immediately — we then refresh the server cache for this session +
      // the sessions sidebar, and clear pendingTurns once the cache catches up
      // (handled below in the post-refetch effect).
      setPendingTurns((prev) => [
        ...prev,
        {
          role: "assistant",
          text: dto.message,
          cost: dto.estimatedCostUsd,
          prompt: dto.promptTokens,
          completion: dto.completionTokens,
          sources: dto.sources,
        },
      ]);

      // A first-turn response materialises a real session id — persist it so
      // a refresh keeps the same chat open. setSessionId also triggers the
      // turns query for the new id; the next render will show the persisted
      // turns from the server cache.
      if (sessionId !== dto.sessionId) {
        setSessionId(dto.sessionId);
        persistSessionId(dto.sessionId);
      } else {
        // Same session — force a refetch so the persisted history catches up
        // with the optimistic pending turns.
        void qc.invalidateQueries({
          queryKey: ["ai", "chat", "session", dto.sessionId],
        });
      }
      void qc.invalidateQueries({ queryKey: ["ai", "chat", "sessions"] });
    },
    onError: (err: unknown) => {
      // Roll back the optimistic user turn so they can retry — otherwise the
      // pane would show a question that was never answered.
      setPendingTurns((prev) => prev.slice(0, -1));
      const status = (err as { status?: number })?.status;
      if (status === 409) toast.warning(t("ai:errors.budgetExceeded"));
      else toast.error(apiErrorMessage(err, t("ai:errors.generic")));
    },
  });

  // When the server's persisted history catches up with the optimistic
  // pending turns, clear pendingTurns so we don't double-render. We detect
  // "caught up" by checking whether the tail of the server history already
  // matches the optimistic turns (same role + text) — robust across both the
  // same-session refetch and the first-turn session-id switch. The previous
  // count-based check could never fire (serverTurns is derived from
  // turnsQuery.data, so the two sides were always equal minus pendingTurns),
  // which left every answer rendered twice.
  useEffect(() => {
    if (pendingTurns.length === 0 || !turnsQuery.data) return;
    const tail = serverTurns.slice(-pendingTurns.length);
    // Match on ROLE for every turn, but only require TEXT equality for the
    // ASSISTANT turn — the server stores the user prompt PII-REDACTED, so a raw
    // optimistic prompt containing an email/phone/card never equalled the
    // persisted (redacted) text, `caughtUp` never fired, and the whole turn
    // rendered twice for the rest of the session. The assistant response is
    // stored verbatim, so it's the reliable anchor.
    const caughtUp =
      tail.length === pendingTurns.length &&
      tail.every(
        (turn, i) =>
          turn.role === pendingTurns[i].role &&
          (pendingTurns[i].role !== "assistant" || turn.text === pendingTurns[i].text),
      );
    if (caughtUp) setPendingTurns([]);
  }, [turnsQuery.data, serverTurns, pendingTurns]);

  // ── Handlers ──────────────────────────────────────────────────────────────

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const msg = draft.trim();
    if (!msg || mut.isPending) return;
    setPendingTurns((prev) => [...prev, { role: "user", text: msg }]);
    setDraft("");
    mut.mutate(msg);
  };

  const handleNewChat = () => {
    setSessionId(undefined);
    persistSessionId(undefined);
    setPendingTurns([]);
    setDraft("");
  };

  const handleSelectSession = (id: string) => {
    if (id === sessionId) return;
    setSessionId(id);
    persistSessionId(id);
    setPendingTurns([]);
    // turnsQuery hydrates serverTurns from the cache on the next render.
  };

  const sessions = sessionsQuery.data ?? [];
  const isFreshChat = !sessionId;

  // ── Suggested prompt chips ────────────────────────────────────────────────
  const SUGGESTIONS: { key: string; icon: typeof Sparkles }[] = [
    { key: "scholarships", icon: GraduationCap },
    { key: "deadlines",    icon: Calendar },
    { key: "eligibility",  icon: ClipboardCheck },
    { key: "essay",        icon: PenSquare },
  ];

  const handlePickSuggestion = (text: string) => {
    if (mut.isPending) return;
    setPendingTurns((prev) => [...prev, { role: "user", text }]);
    setDraft("");
    mut.mutate(text);
  };

  // ── Sidebar render helper ─────────────────────────────────────────────────
  // Plain function (not memoised) — the JSX is cheap to recompute and the
  // handlers it closes over are stable enough that memoisation wouldn't help.

  const sidebar = (
    <aside className="w-full md:w-64 md:flex-shrink-0 border-b md:border-b-0 md:border-e border-border-subtle bg-bg-subtle/40 flex flex-col">
      <div className="p-3 border-b border-border-subtle">
        <button
          type="button"
          onClick={handleNewChat}
          className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white text-sm font-semibold shadow-brand-sm hover:shadow-brand-md hover:from-brand-600 hover:to-brand-800 active:scale-[0.98] transition-all"
        >
          <Plus size={14} />
          {t("ai:chat.sidebar.newChat")}
        </button>
      </div>
      <div className="px-4 pt-3 pb-2">
        <h3 className="text-[10px] font-bold uppercase tracking-wider text-text-tertiary">
          {t("ai:chat.sidebar.title")}
        </h3>
      </div>
      <div className="flex-1 overflow-y-auto px-2 pb-3 space-y-0.5 max-h-[260px] md:max-h-none scrollbar-premium">
        {sessionsQuery.isError ? (
          <p className="px-2 py-3 text-xs text-danger-500">
            {t("ai:chat.sidebar.loadError")}
          </p>
        ) : sessions.length === 0 ? (
          <p className="px-3 py-6 text-xs text-text-tertiary text-center leading-relaxed">
            {t("ai:chat.sidebar.empty")}
          </p>
        ) : (
          sessions.map((s) => {
            const isActive = s.sessionId === sessionId;
            const lastAt = s.lastTurnAt ?? s.startedAt;
            return (
              <button
                key={s.sessionId}
                type="button"
                onClick={() => handleSelectSession(s.sessionId)}
                aria-current={isActive ? "true" : undefined}
                className={`group relative w-full text-start px-3 py-2 rounded-lg transition-all border ${
                  isActive
                    ? "bg-bg-elevated border-border-subtle shadow-elevation-1"
                    : "border-transparent hover:bg-bg-elevated/60"
                }`}
              >
                {isActive && (
                  <span
                    aria-hidden
                    className="absolute start-0 top-1.5 bottom-1.5 w-[3px] rounded-full bg-gradient-to-b from-brand-500 to-brand-700"
                  />
                )}
                <p
                  className={`text-xs leading-snug line-clamp-2 ${
                    isActive ? "font-semibold text-text-primary" : "text-text-secondary"
                  }`}
                >
                  {s.title}
                </p>
                <p className="text-[10px] text-text-tertiary mt-1 flex items-center gap-1.5">
                  <span>
                    {formatDistanceToNow(new Date(lastAt), {
                      addSuffix: true,
                      locale: dateLocale,
                    })}
                  </span>
                  <span aria-hidden>·</span>
                  <span>
                    {t("ai:chat.sidebar.turnCount", { count: s.turnCount })}
                  </span>
                </p>
              </button>
            );
          })
        )}
      </div>
    </aside>
  );

  return (
    <section className="flex flex-col md:flex-row overflow-hidden rounded-2xl border border-border-subtle bg-bg-elevated shadow-elevation-2">
      {sidebar}

      <div className="flex flex-1 flex-col min-w-0">
        <header className="flex items-center gap-2.5 border-b border-border-subtle px-5 py-3.5 bg-bg-elevated/95 backdrop-blur">
          <span className="inline-flex size-8 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm">
            <Sparkles aria-hidden className="size-4" />
          </span>
          <div>
            <h2 className="font-bold text-text-primary tracking-tight leading-none">{t("ai:chat.heading")}</h2>
            <p className="text-[10px] text-text-tertiary uppercase tracking-wider font-semibold mt-0.5">{t("ai:disclaimer")}</p>
          </div>
        </header>

        <div
          ref={scrollRef}
          className="flex-1 space-y-4 overflow-y-auto px-4 sm:px-5 py-5 bg-bg-subtle/30 scrollbar-premium"
          style={{ minHeight: 360, maxHeight: 540 }}
        >
          {turnsQuery.isLoading && sessionId ? (
            <div className="flex items-center justify-center h-full text-text-tertiary text-sm gap-2">
              <Loader2 size={14} className="animate-spin" />
              <span>{t("ai:chat.sidebar.loadingTurns")}</span>
            </div>
          ) : turns.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center px-4">
              <div className="relative mb-5">
                <div className="size-16 rounded-3xl bg-gradient-to-br from-brand-50 to-brand-100 text-brand-600 flex items-center justify-center border border-brand-200/60 shadow-elevation-1">
                  <Sparkles size={28} />
                </div>
                <div aria-hidden className="absolute inset-0 rounded-3xl bg-brand-500/15 blur-2xl -z-10" />
              </div>
              <h3 className="text-lg font-bold text-text-primary tracking-tight mb-1.5">
                {t("ai:chat.emptyTitle", "How can I help you today?")}
              </h3>
              <p className="text-sm text-text-secondary max-w-sm leading-relaxed mb-6">
                {t("ai:chat.emptyHint")}
              </p>
              {isFreshChat && (
                <div className="w-full max-w-md">
                  <p className="text-[10px] font-bold uppercase tracking-wider text-text-tertiary mb-2.5">
                    {t("ai:chat.suggestionsTitle", "Try asking")}
                  </p>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    {SUGGESTIONS.map(({ key, icon: Icon }) => {
                      const text = t(`ai:chat.suggestions.${key}`);
                      return (
                        <button
                          key={key}
                          type="button"
                          onClick={() => handlePickSuggestion(text)}
                          className="group flex items-start gap-2.5 text-start px-3.5 py-3 rounded-xl border border-border-subtle bg-bg-elevated hover:border-brand-300 hover:bg-brand-50/40 transition-all shadow-elevation-1 hover:shadow-elevation-2"
                        >
                          <span className="inline-flex size-7 items-center justify-center rounded-lg bg-brand-50 text-brand-600 shrink-0 group-hover:bg-brand-100">
                            <Icon size={14} aria-hidden />
                          </span>
                          <span className="text-xs text-text-primary font-medium leading-snug">
                            {text}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          ) : null}
          {turns.map((turn, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.2 }}
              className={`flex items-end gap-2 ${turn.role === "user" ? "justify-end" : "justify-start"}`}
            >
              {turn.role === "assistant" && (
                <span className="inline-flex size-7 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm shrink-0 ring-2 ring-bg-elevated">
                  <Sparkles size={12} aria-hidden />
                </span>
              )}
              <div
                className={`max-w-[80%] rounded-2xl px-4 py-2.5 text-sm shadow-elevation-1 ${
                  turn.role === "user"
                    ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm rounded-br-md"
                    : "bg-bg-elevated text-text-primary border border-border-subtle rounded-bl-md"
                }`}
              >
                <p className="whitespace-pre-wrap break-words leading-relaxed">{turn.text}</p>
                {turn.role === "assistant" && turn.sources && turn.sources.length > 0 && (
                  <div className="mt-3 border-t border-border-subtle pt-2">
                    <div className="text-[10px] font-bold uppercase tracking-wider text-text-tertiary mb-1.5">
                      {t("ai:chat.sources")}
                    </div>
                    <ul className="space-y-1">
                      {turn.sources.map((src, si) => (
                        <li key={si} className="text-xs">
                          {src.sourceType === "Scholarship" && src.scholarshipId ? (
                            <Link
                              to={`/student/scholarships/${src.scholarshipId}`}
                              className="inline-flex items-center gap-1.5 text-brand-600 hover:text-brand-700 hover:underline font-medium"
                            >
                              <FileText aria-hidden className="size-3 shrink-0" />
                              {src.title}
                            </Link>
                          ) : (
                            <span className="inline-flex items-center gap-1.5 text-text-tertiary">
                              <FileText aria-hidden className="size-3 shrink-0" />
                              {src.title}
                            </span>
                          )}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {turn.role === "assistant" && turn.cost != null && (
                  <div className="mt-1.5 text-[10px] tabular-nums text-text-tertiary">
                    {t("ai:chat.costHint", {
                      cost: turn.cost.toFixed(4),
                      prompt: turn.prompt ?? 0,
                      completion: turn.completion ?? 0,
                    })}
                  </div>
                )}
              </div>
            </motion.div>
          ))}
          <AnimatePresence>
            {mut.isPending && (
              <motion.div
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                className="flex items-end gap-2 justify-start"
              >
                <span className="inline-flex size-7 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm shrink-0 ring-2 ring-bg-elevated">
                  <Sparkles size={12} aria-hidden />
                </span>
                <div className="rounded-2xl rounded-bl-md border border-border-subtle bg-bg-elevated px-4 py-2.5 text-sm text-text-tertiary shadow-elevation-1">
                  <div className="flex gap-1 items-center">
                    <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce" />
                    <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce [animation-delay:0.15s]" />
                    <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce [animation-delay:0.3s]" />
                  </div>
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        <form
          onSubmit={onSubmit}
          className="border-t border-border-subtle p-3 bg-bg-elevated"
        >
          <div className="flex items-center gap-2 rounded-2xl border bg-bg-subtle/60 px-3 py-2 transition-colors focus-within:border-brand-300 focus-within:bg-bg-elevated focus-within:ring-2 focus-within:ring-brand-400/30 border-border-subtle">
            <input
              ref={inputRef}
              type="text"
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              placeholder={t("ai:chat.placeholder")}
              maxLength={2000}
              className="flex-1 bg-transparent text-text-primary outline-none text-sm placeholder:text-text-tertiary py-1.5"
            />
            <button
              type="submit"
              disabled={!draft.trim() || mut.isPending}
              aria-label={t("ai:chat.send")}
              className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm transition-all hover:shadow-brand-md hover:from-brand-600 hover:to-brand-800 active:scale-95 disabled:bg-none disabled:bg-bg-subtle disabled:text-text-tertiary disabled:shadow-none disabled:cursor-not-allowed shrink-0"
            >
              {mut.isPending ? (
                <Loader2 size={16} className="animate-spin" aria-hidden />
              ) : (
                <Send size={14} className="rtl:rotate-180" aria-hidden />
              )}
            </button>
          </div>
        </form>

        <div className="border-t border-border-subtle bg-bg-subtle/40 px-4 py-2">
          <AiDisclaimer />
        </div>
      </div>
    </section>
  );
});
