import { useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
  Send,
  MessageSquare,
  FileText,
  Plus,
  Loader2,
} from "lucide-react";
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

export function Chatbot() {
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
  // pending turns, clear pendingTurns so we don't double-render. This IS a
  // "react to external state change" pattern — exactly the case the
  // react-hooks/set-state-in-effect rule warns against — but the alternative
  // (writing optimistic rows directly into the react-query cache) introduces
  // a new failure mode when the session id changes mid-request.
  useEffect(() => {
    if (pendingTurns.length === 0) return;
    if (turnsQuery.data && turnsQuery.data.length * 2 >= serverTurns.length + pendingTurns.length) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setPendingTurns([]);
    }
  }, [turnsQuery.data, serverTurns.length, pendingTurns.length]);

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

  // ── Sidebar render helper ─────────────────────────────────────────────────
  // Plain function (not memoised) — the JSX is cheap to recompute and the
  // handlers it closes over are stable enough that memoisation wouldn't help.

  const sidebar = (
      <aside className="w-full md:w-64 md:flex-shrink-0 border-b md:border-b-0 md:border-e border-border-subtle bg-bg-subtle/40 flex flex-col">
        <div className="p-3 border-b border-border-subtle">
          <button
            type="button"
            onClick={handleNewChat}
            className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg bg-brand-500 text-white text-sm font-medium hover:bg-brand-600 transition-colors"
          >
            <Plus size={14} />
            {t("ai:chat.sidebar.newChat")}
          </button>
        </div>
        <div className="px-3 pt-3 pb-2">
          <h3 className="text-[10px] font-bold uppercase tracking-wider text-text-tertiary">
            {t("ai:chat.sidebar.title")}
          </h3>
        </div>
        <div className="flex-1 overflow-y-auto px-2 pb-3 space-y-1 max-h-[260px] md:max-h-none">
          {sessionsQuery.isError ? (
            <p className="px-2 py-3 text-xs text-danger-500">
              {t("ai:chat.sidebar.loadError")}
            </p>
          ) : sessions.length === 0 ? (
            <p className="px-2 py-6 text-xs text-text-tertiary text-center">
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
                  className={`group relative w-full text-start px-3 py-2 rounded-lg transition-colors ${
                    isActive
                      ? "bg-brand-50/70"
                      : "hover:bg-bg-elevated"
                  }`}
                >
                  {isActive && (
                    <span
                      aria-hidden
                      className="absolute start-0 top-1.5 bottom-1.5 w-1 rounded-full bg-brand-500"
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
    <section className="flex flex-col md:flex-row overflow-hidden rounded-lg border border-border-subtle bg-bg-elevated">
      {sidebar}

      <div className="flex flex-1 flex-col min-w-0">
        <header className="flex items-center gap-2 border-b border-border-subtle px-4 py-3">
          <MessageSquare aria-hidden className="size-5 text-brand-500" />
          <h2 className="font-semibold">{t("ai:chat.heading")}</h2>
        </header>

        <div
          ref={scrollRef}
          className="flex-1 space-y-3 overflow-y-auto p-4"
          style={{ minHeight: 320, maxHeight: 480 }}
        >
          {turnsQuery.isLoading && sessionId ? (
            <div className="flex items-center justify-center h-full text-text-tertiary text-sm gap-2">
              <Loader2 size={14} className="animate-spin" />
              <span>{t("ai:chat.sidebar.loadingTurns")}</span>
            </div>
          ) : turns.length === 0 ? (
            <p className="text-sm text-text-tertiary">
              {isFreshChat
                ? t("ai:chat.emptyHint")
                : t("ai:chat.emptyHint")}
            </p>
          ) : null}
          {turns.map((turn, i) => (
            <div
              key={i}
              className={`flex ${turn.role === "user" ? "justify-end" : "justify-start"}`}
            >
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
                {turn.role === "assistant" && turn.sources && turn.sources.length > 0 && (
                  <div className="mt-2 border-t border-border-subtle pt-1.5">
                    <div className="text-[10px] font-semibold uppercase tracking-wide opacity-60">
                      {t("ai:chat.sources")}
                    </div>
                    <ul className="mt-1 space-y-0.5">
                      {turn.sources.map((src, si) => (
                        <li key={si} className="text-[11px]">
                          {src.sourceType === "Scholarship" && src.scholarshipId ? (
                            <Link
                              to={`/student/scholarships/${src.scholarshipId}`}
                              className="inline-flex items-center gap-1 text-brand-500 hover:underline"
                            >
                              <FileText aria-hidden className="size-3 shrink-0" />
                              {src.title}
                            </Link>
                          ) : (
                            <span className="inline-flex items-center gap-1 opacity-70">
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
                {t("ai:chat.thinking")}
              </div>
            </div>
          )}
        </div>

        <form
          onSubmit={onSubmit}
          className="flex items-center gap-2 border-t border-border-subtle p-3"
        >
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
      </div>
    </section>
  );
}
