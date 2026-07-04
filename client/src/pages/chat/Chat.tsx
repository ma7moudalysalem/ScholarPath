import { useState, useEffect, useMemo, useRef } from "react";
import { useTranslation } from "react-i18next";
import {
  Send,
  Search,
  Circle,
  MessageCircle,
  PenSquare,
  ArrowLeft,
  Loader2,
  ShieldAlert,
} from "lucide-react";
import { toast } from "sonner";
import {
  chatApi,
  type ChatContact,
  type ChatConversation,
  type ChatMessage,
} from "@/services/api/chat";
import { useAuthStore } from "@/stores/authStore";
import { UserAvatar } from "@/components/common/UserAvatar";
import { createHubConnection } from "@/lib/signalr";
import { usePresenceStore } from "@/stores/presenceStore";
import type { HubConnection } from "@microsoft/signalr";
import { formatDistanceToNow, format, isToday, isYesterday, isSameDay } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { NewMessageModal } from "@/components/chat/NewMessageModal";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { apiErrorMessage } from "@/services/api/client";

export function Chat() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.dir() === "rtl";
  const dateLocale = isRtl ? ar : undefined;
  const qc = useQueryClient();

  const currentUser = useAuthStore((state) => state.user);
  const accessToken = useAuthStore((state) => state.tokens?.accessToken);

  const [selectedConv, setSelectedConv] = useState<ChatConversation | null>(null);
  const [messageBody, setMessageBody] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const [typingUser, setTypingUser] = useState<string | null>(null);
  const [conversationSearch, setConversationSearch] = useState("");
  const [isComposeOpen, setIsComposeOpen] = useState(false);
  const [isBlockConfirmOpen, setBlockConfirmOpen] = useState(false);
  const [isBlockSubmitting, setBlockSubmitting] = useState(false);
  const [isSending, setIsSending] = useState(false);

  const hubConnectionRef = useRef<HubConnection | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesScrollRef = useRef<HTMLDivElement>(null);
  // Refs let the once-registered hub handlers read the latest selection /
  // identity without the connection effect re-running (and reconnecting).
  const selectedConvRef = useRef<ChatConversation | null>(null);
  const currentUserIdRef = useRef<string | undefined>(currentUser?.id);
  // Tracks the conversation id we last joined on the hub so we can leave it
  // cleanly when the user picks a different thread — otherwise typing pings
  // and old MessageReceived broadcasts leak across conversations.
  const joinedConversationIdRef = useRef<string | null>(null);
  // Typing-stop debounce timer — when the user stops typing for 3s we send
  // TypingStop. The previous version leaked one timer per keystroke.
  const typingStopTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Live chat presence — drives the online dots in the list + thread header.
  const onlineUserIds = usePresenceStore((state) => state.onlineUserIds);
  const selectedParticipantOnline = selectedConv
    ? onlineUserIds.has(selectedConv.otherParticipantId) || selectedConv.isOnline
    : false;

  const { data: conversations = [] } = useQuery({
    queryKey: ["chat", "conversations"],
    queryFn: () => chatApi.getConversations(),
  });

  const filteredConversations = useMemo(() => {
    const term = conversationSearch.trim().toLowerCase();
    if (!term) return conversations;
    return conversations.filter((conv) =>
      conv.otherParticipantName.toLowerCase().includes(term),
    );
  }, [conversations, conversationSearch]);

  // A "pending:" id is a not-yet-created conversation from the compose flow —
  // skip the message fetch until the first send turns it into a real one.
  const isConversationPersisted = !!selectedConv && !selectedConv.id.startsWith("pending:");

  const {
    data: messages = [],
    isLoading: isLoadingMessages,
    isFetching: isFetchingMessages,
  } = useQuery({
    queryKey: ["chat", "messages", selectedConv?.id],
    queryFn: () => chatApi.getMessages(selectedConv!.id),
    enabled: isConversationPersisted,
    // No keepPreviousData on purpose — we want a clean break between threads
    // so messages from the previous conversation never bleed into the new one.
  });

  const scrollToBottom = (smooth = true) => {
    messagesEndRef.current?.scrollIntoView({ behavior: smooth ? "smooth" : "auto" });
  };

  useEffect(() => {
    // After a thread loads, jump to the latest message without animation —
    // animating a long scroll on the very first paint feels janky.
    scrollToBottom(false);
  }, [selectedConv?.id]);

  useEffect(() => {
    scrollToBottom(true);
  }, [messages.length]);

  // Keep the handler-facing refs in sync with the latest render values.
  useEffect(() => {
    selectedConvRef.current = selectedConv;
  }, [selectedConv]);

  useEffect(() => {
    currentUserIdRef.current = currentUser?.id;
  }, [currentUser?.id]);

  // selectConversation centralises every place that changes the open thread —
  // it doubles as the "reset side-effects" hook so we don't depend on an
  // effect-on-prop-change pattern (which is what react-hooks/set-state-in-effect
  // discourages). Every caller goes through this so the cleanup is guaranteed.
  const selectConversation = (next: ChatConversation | null) => {
    setTypingUser(null);
    setMessageBody("");
    setIsTyping(false);
    if (typingStopTimerRef.current) {
      clearTimeout(typingStopTimerRef.current);
      typingStopTimerRef.current = null;
    }
    setSelectedConv(next);
  };

  useEffect(() => {
    if (!accessToken) return;

    const connection = createHubConnection("/hubs/chat", accessToken);
    const presence = usePresenceStore.getState();

    connection.on("MessageReceived", (message: ChatMessage) => {
      const conv = selectedConvRef.current;
      // If the message belongs to the open conversation, refresh its messages.
      if (
        conv &&
        (message.senderId === conv.otherParticipantId ||
          message.senderId === currentUserIdRef.current)
      ) {
        void qc.invalidateQueries({ queryKey: ["chat", "messages", conv.id] });
      }
      // Always refresh conversations to update last message / timestamp.
      void qc.invalidateQueries({ queryKey: ["chat", "conversations"] });
    });

    connection.on("TypingStart", (conversationId: string, userId: string) => {
      if (selectedConvRef.current?.id === conversationId && userId !== currentUserIdRef.current) {
        setTypingUser(userId);
      }
    });

    connection.on("TypingStop", (conversationId: string) => {
      if (selectedConvRef.current?.id === conversationId) {
        setTypingUser(null);
      }
    });

    // Live presence — UserOnline / UserOffline keep the presence store fresh.
    connection.on("UserOnline", (userId: string) => presence.markOnline(userId));
    connection.on("UserOffline", (userId: string) => presence.markOffline(userId));

    let cancelled = false;
    connection
      .start()
      .then(async () => {
        if (cancelled) return;
        hubConnectionRef.current = connection;

        // Re-join the open conversation: a connection that finishes starting
        // after a conversation was already picked would otherwise miss its group.
        const conv = selectedConvRef.current;
        if (conv && !conv.id.startsWith("pending:")) {
          try {
            await connection.invoke("JoinConversation", conv.id);
            joinedConversationIdRef.current = conv.id;
          } catch {
            /* best-effort group re-join */
          }
        }

        // Seed presence with everyone already online.
        try {
          const online = await connection.invoke<string[]>("GetOnlineUsers");
          if (!cancelled) usePresenceStore.getState().setOnlineUsers(online);
        } catch {
          /* best-effort seed — UserOnline/UserOffline events still arrive */
        }
      })
      .catch((err: unknown) => {
        // The cleanup below aborts an in-flight start() (notably under React
        // StrictMode's double-invoke) — that rejection is expected. Surface
        // only failures from a still-live effect.
        if (!cancelled) console.warn("Chat hub failed to start", err);
      });

    return () => {
      cancelled = true;
      hubConnectionRef.current = null;
      joinedConversationIdRef.current = null;
      usePresenceStore.getState().reset();
      void connection.stop();
    };
  }, [accessToken, qc]);

  // Swap hub group membership when the open conversation changes — leave the
  // old group first so typing pings and broadcasts don't cross between threads.
  useEffect(() => {
    const hub = hubConnectionRef.current;
    if (!hub) return;

    const prevId = joinedConversationIdRef.current;
    const nextId =
      selectedConv && isConversationPersisted ? selectedConv.id : null;

    if (prevId === nextId) return;

    const swap = async () => {
      if (prevId) {
        try {
          await hub.invoke("LeaveConversation", prevId);
        } catch {
          /* best-effort leave */
        }
      }
      if (nextId) {
        try {
          await hub.invoke("JoinConversation", nextId);
        } catch {
          /* best-effort join */
        }
      }
      joinedConversationIdRef.current = nextId;
    };

    void swap();
  }, [selectedConv, isConversationPersisted]);

  const isBlocked = selectedConv?.isBlocked === true;

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedConv || !messageBody.trim() || isSending) return;

    if (isBlocked) {
      toast.error(t("chat.send_blocked_error", "You can't send a message — this conversation is blocked."));
      return;
    }

    // A "pending:" id means this is a freshly composed conversation that does
    // not exist server-side yet — the send below auto-creates it.
    const isPending = selectedConv.id.startsWith("pending:");
    const body = messageBody.trim();

    setIsSending(true);
    try {
      await chatApi.sendMessage({
        recipientId: selectedConv.otherParticipantId,
        body,
      });

      setMessageBody("");
      // Stop any pending typing-stop timer; we're done typing now.
      if (typingStopTimerRef.current) {
        clearTimeout(typingStopTimerRef.current);
        typingStopTimerRef.current = null;
      }
      setIsTyping(false);

      if (!isPending) {
        void qc.invalidateQueries({ queryKey: ["chat", "messages", selectedConv.id] });
      }

      // Refresh the list; for a pending conversation, swap the placeholder for
      // the real row the backend just created so the thread loads its messages.
      const fresh = await qc.fetchQuery({
        queryKey: ["chat", "conversations"],
        queryFn: () => chatApi.getConversations(),
      });
      if (isPending) {
        const real = fresh.find((c) => c.otherParticipantId === selectedConv.otherParticipantId);
        if (real) setSelectedConv(real);
      }
    } catch (error) {
      console.error("Failed to send message", error);
      toast.error(
        apiErrorMessage(error, t("chat.send_error", "Could not send your message.")),
      );
    } finally {
      setIsSending(false);
    }
  };

  const handleTyping = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMessageBody(e.target.value);
    if (!selectedConv || !hubConnectionRef.current) return;
    // Don't broadcast typing into a blocked or unsaved-yet conversation.
    if (isBlocked || selectedConv.id.startsWith("pending:")) return;

    if (!isTyping) {
      setIsTyping(true);
      void hubConnectionRef.current.invoke("TypingStart", selectedConv.id);
    }

    // Reset the 3s idle timer on every keystroke so the indicator stays alive
    // while the user is actively typing instead of flickering off mid-thought.
    if (typingStopTimerRef.current) clearTimeout(typingStopTimerRef.current);
    typingStopTimerRef.current = setTimeout(() => {
      setIsTyping(false);
      typingStopTimerRef.current = null;
      const hub = hubConnectionRef.current;
      const conv = selectedConvRef.current;
      if (hub && conv && !conv.id.startsWith("pending:")) {
        void hub.invoke("TypingStop", conv.id);
      }
    }, 3000);
  };

  // Compose: pick a contact → open the existing conversation if there is one,
  // otherwise stage a placeholder. POST /api/chat/messages auto-creates the
  // real conversation on the first message, so the next refresh fills it in.
  const handleSelectContact = (contact: ChatContact) => {
    const existing = conversations.find((c) => c.otherParticipantId === contact.id);
    if (existing) {
      selectConversation(existing);
      return;
    }
    selectConversation({
      id: `pending:${contact.id}`,
      otherParticipantId: contact.id,
      otherParticipantName: contact.name,
      otherParticipantAvatarUrl: contact.photoUrl,
      isOnline: false,
      isBlocked: false,
    });
  };

  const confirmToggleBlock = async () => {
    if (!selectedConv) return;
    const wasBlocked = selectedConv.isBlocked;
    setBlockSubmitting(true);

    try {
      if (wasBlocked) {
        await chatApi.unblockUser(selectedConv.otherParticipantId);
        toast.success(t("chat.unblock_success", "User unblocked."));
      } else {
        await chatApi.blockUser({ userId: selectedConv.otherParticipantId });
        toast.success(t("chat.block_success", "User blocked."));
      }

      // Keep the thread open and refresh so the button reflects the new block
      // state and the toggle stays reachable (UAT TC-006).
      const fresh = await qc.fetchQuery({
        queryKey: ["chat", "conversations"],
        queryFn: () => chatApi.getConversations(),
      });
      const updated = fresh.find(
        (c) => c.otherParticipantId === selectedConv.otherParticipantId,
      );
      setSelectedConv(updated ?? { ...selectedConv, isBlocked: !wasBlocked });
    } catch (error) {
      console.error("Failed to toggle block", error);
      toast.error(
        apiErrorMessage(
          error,
          wasBlocked
            ? t("chat.unblock_error", "Could not unblock this user.")
            : t("chat.block_error", "Could not block this user."),
        ),
      );
    } finally {
      setBlockSubmitting(false);
      setBlockConfirmOpen(false);
    }
  };

  // Build a flat list of [separator, ...messages] groupings so the renderer
  // can drop a Today / Yesterday / formatted-date divider before each calendar day.
  const groupedMessages = useMemo(() => {
    const groups: Array<{ key: string; label: string; items: ChatMessage[] }> = [];
    for (const msg of messages) {
      const sentAt = new Date(msg.sentAt);
      const last = groups[groups.length - 1];
      if (last && isSameDay(new Date(last.items[0].sentAt), sentAt)) {
        last.items.push(msg);
        continue;
      }
      let label: string;
      if (isToday(sentAt)) label = t("chat.date_today", "Today");
      else if (isYesterday(sentAt)) label = t("chat.date_yesterday", "Yesterday");
      else label = format(sentAt, "PP", { locale: dateLocale });
      groups.push({ key: msg.id, label, items: [msg] });
    }
    return groups;
  }, [messages, t, dateLocale]);

  // Locale-aware HH:MM — the previous toLocaleTimeString([], ...) call ignored
  // the active i18n locale, so Arabic users saw English digits.
  const formatTime = (iso: string) => {
    const date = new Date(iso);
    return format(date, "p", { locale: dateLocale });
  };

  // The right-pane header — extracted because the same block renders on mobile
  // when a conversation is open and on desktop when one is selected.
  const renderHeader = () => (
    <div className="px-5 py-3.5 border-b border-border-subtle flex items-center justify-between bg-bg-elevated/95 backdrop-blur-md sticky top-0 z-10">
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={() => selectConversation(null)}
          aria-label={t("chat.back_to_list", "Back to conversations")}
          className="md:hidden -ms-1 me-0.5 p-1.5 rounded-full text-text-secondary hover:bg-bg-subtle transition-colors"
        >
          <ArrowLeft size={18} className={isRtl ? "rotate-180" : ""} />
        </button>
        <div className="relative flex-shrink-0">
          <UserAvatar
            userId={selectedConv!.otherParticipantId}
            name={selectedConv!.otherParticipantName}
            className="w-10 h-10 ring-2 ring-bg-elevated"
          />
          {selectedParticipantOnline && (
            <div className="absolute -bottom-0.5 -end-0.5 w-3 h-3 bg-success-500 border-2 border-bg-elevated rounded-full" />
          )}
        </div>
        <div className="min-w-0">
          <h3 className="text-sm font-bold truncate leading-tight tracking-tight">
            {selectedConv!.otherParticipantName}
          </h3>
          {typingUser ? (
            <span
              className="text-[11px] flex items-center gap-1.5 font-medium leading-tight text-brand-600 mt-0.5"
              aria-live="polite"
            >
              <span className="flex gap-0.5">
                <span className="w-1 h-1 bg-brand-500 rounded-full animate-bounce" />
                <span className="w-1 h-1 bg-brand-500 rounded-full animate-bounce [animation-delay:0.15s]" />
                <span className="w-1 h-1 bg-brand-500 rounded-full animate-bounce [animation-delay:0.3s]" />
              </span>
              {t("chat.typing_indicator", "{{name}} is typing…", { name: selectedConv!.otherParticipantName.split(" ")[0] })}
            </span>
          ) : (
            <span
              className={`text-[11px] flex items-center gap-1 font-medium leading-tight mt-0.5 ${
                selectedParticipantOnline ? "text-success-600" : "text-text-tertiary"
              }`}
            >
              <Circle size={6} fill="currentColor" />
              {selectedParticipantOnline
                ? t("chat.online", "Online")
                : t("chat.offline", "Offline")}
            </span>
          )}
        </div>
      </div>
      <button
        type="button"
        onClick={() => setBlockConfirmOpen(true)}
        title={
          selectedConv!.isBlocked
            ? t("chat.unblock_user", "Unblock user")
            : t("chat.block_user", "Block user")
        }
        aria-label={
          selectedConv!.isBlocked
            ? t("chat.unblock_user", "Unblock user")
            : t("chat.block_user", "Block user")
        }
        className={`inline-flex items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-xs font-medium transition-colors ${
          selectedConv!.isBlocked
            ? "text-brand-600 hover:bg-brand-50"
            : "text-text-tertiary hover:bg-danger-50 hover:text-danger-500"
        }`}
      >
        <ShieldAlert size={14} aria-hidden />
        <span className="hidden sm:inline">
          {selectedConv!.isBlocked
            ? t("chat.unblock_user", "Unblock user")
            : t("chat.block_user", "Block user")}
        </span>
      </button>
    </div>
  );

  return (
    <div className="flex h-[calc(100vh-120px)] max-w-6xl mx-auto bg-bg-elevated rounded-3xl border border-border-subtle shadow-elevation-2 overflow-hidden my-4">
      {/* Conversation List — full-width on mobile when no thread is open. */}
      <aside
        className={`${
          selectedConv ? "hidden md:flex" : "flex"
        } w-full md:w-80 border-e border-border-subtle flex-col bg-bg-subtle/40 flex-shrink-0`}
      >
        <div className="px-4 pt-5 pb-3 border-b border-border-subtle bg-bg-elevated">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-baseline gap-2 px-1">
              <h2 className="text-base font-bold text-text-primary tracking-tight">
                {t("chat.messages", "Direct Messages")}
              </h2>
              {conversations.length > 0 && (
                <span className="text-xs font-semibold text-text-tertiary tabular-nums">
                  {conversations.length}
                </span>
              )}
            </div>
            <button
              type="button"
              onClick={() => setIsComposeOpen(true)}
              aria-label={t("chat.new_message", "New Message")}
              title={t("chat.new_message", "New Message")}
              className="flex items-center justify-center w-9 h-9 bg-gradient-to-br from-brand-500 to-brand-700 text-white rounded-xl shadow-brand-sm hover:shadow-brand-md hover:from-brand-600 hover:to-brand-800 active:scale-95 transition-all"
            >
              <PenSquare size={15} />
            </button>
          </div>
          <div className="relative">
            <Search
              className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary"
              size={14}
              aria-hidden
            />
            <input
              type="text"
              value={conversationSearch}
              onChange={(e) => setConversationSearch(e.target.value)}
              placeholder={t("chat.search_placeholder", "Search conversations...")}
              aria-label={t("chat.header_search_aria", "Search conversations")}
              className="w-full ps-9 pe-3 py-2 bg-bg-subtle border border-transparent rounded-xl text-sm outline-none focus:bg-bg-elevated focus:border-brand-300 focus:ring-2 focus:ring-brand-400/30 transition-all placeholder:text-text-tertiary"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-2 space-y-0.5 scrollbar-premium">
          {filteredConversations.length === 0 ? (
            <div className="flex flex-col items-center justify-center px-6 py-12 text-center gap-2">
              <div className="w-12 h-12 rounded-full bg-bg-elevated border border-border-subtle flex items-center justify-center text-text-tertiary">
                <MessageCircle size={20} aria-hidden />
              </div>
              <p className="text-sm font-semibold text-text-secondary">
                {conversationSearch.trim()
                  ? t("chat.search_no_match", "No matching conversations.")
                  : t("chat.no_conversations", "No conversations yet. Start a new message.")}
              </p>
              <p className="text-xs text-text-tertiary max-w-[14rem] leading-relaxed">
                {conversationSearch.trim()
                  ? t("chat.empty_search_hint", "Try a different name or start a new conversation.")
                  : t("chat.empty_list_subtitle", "Reach out to your consultants, classmates, or anyone in the ScholarPath network.")}
              </p>
            </div>
          ) : (
            filteredConversations.map((conv) => {
              const isActive = selectedConv?.id === conv.id;
              const isOnlineDot =
                onlineUserIds.has(conv.otherParticipantId) || conv.isOnline;
              return (
                <button
                  key={conv.id}
                  onClick={() => selectConversation(conv)}
                  aria-current={isActive ? "true" : undefined}
                  // Softer selected state — a left/right accent bar + light tint
                  // (instead of a saturated full-fill brand pill) reads as
                  // "highlighted" without overpowering the sidebar. The accent
                  // is anchored to the start edge so it flips with RTL.
                  className={`group relative w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-start transition-all ${
                    isActive
                      ? "bg-bg-elevated text-text-primary shadow-elevation-1 border border-border-subtle"
                      : "hover:bg-bg-elevated/60 text-text-primary border border-transparent"
                  }`}
                >
                  {isActive && (
                    <span
                      aria-hidden
                      className="absolute start-0 top-2.5 bottom-2.5 w-[3px] rounded-full bg-gradient-to-b from-brand-500 to-brand-700"
                    />
                  )}
                  <div className="relative flex-shrink-0">
                    <UserAvatar
                      userId={conv.otherParticipantId}
                      name={conv.otherParticipantName}
                      className="w-11 h-11 ring-2 ring-bg-elevated"
                    />
                    {isOnlineDot && (
                      <div className="absolute -bottom-0.5 -end-0.5 w-3 h-3 bg-success-500 border-2 border-bg-elevated rounded-full">
                        <span aria-hidden className="pulse-dot absolute inset-0 rounded-full bg-success-500/50" />
                      </div>
                    )}
                    {conv.isBlocked && (
                      <div
                        aria-hidden
                        className="absolute -bottom-0.5 -end-0.5 w-3 h-3 bg-danger-500 border-2 border-bg-elevated rounded-full flex items-center justify-center"
                      >
                        <ShieldAlert size={7} className="text-white" />
                      </div>
                    )}
                  </div>
                  <div className="flex-1 overflow-hidden min-w-0">
                    <div className="flex justify-between items-baseline mb-0.5 gap-2">
                      <h4
                        className={`text-sm truncate ${
                          isActive ? "font-bold text-text-primary" : "font-semibold text-text-primary"
                        }`}
                      >
                        {conv.otherParticipantName}
                      </h4>
                      {conv.lastMessageAt && (
                        <span className={`text-[10px] flex-shrink-0 tabular-nums ${
                          isActive ? "text-brand-600 font-semibold" : "text-text-tertiary"
                        }`}>
                          {formatDistanceToNow(new Date(conv.lastMessageAt), {
                            addSuffix: false,
                            locale: dateLocale,
                          })}
                        </span>
                      )}
                    </div>
                    <p
                      className={`text-xs truncate ${
                        isActive ? "text-text-secondary" : "text-text-tertiary"
                      }`}
                    >
                      {conv.lastMessageBody ||
                        t("chat.start_conversation", "No messages yet")}
                    </p>
                  </div>
                </button>
              );
            })
          )}
        </div>
      </aside>

      {/* Chat Window */}
      <main
        className={`${
          selectedConv ? "flex" : "hidden md:flex"
        } flex-1 flex-col bg-bg-elevated relative min-w-0`}
      >
        {selectedConv ? (
          <>
            {renderHeader()}

            {/* Blocked banner — sits ABOVE the messages so the state is obvious. */}
            {isBlocked && (
              <div className="px-4 py-2.5 bg-danger-50 border-b border-danger-100 text-danger-700 text-xs flex items-center gap-2">
                <ShieldAlert size={14} className="flex-shrink-0" />
                <span>{t(
                  "chat.blocked_banner_you_blocked",
                  "You blocked this person. Unblock to message each other again.",
                )}</span>
              </div>
            )}

            {/* Messages */}
            <div
              ref={messagesScrollRef}
              className="flex-1 overflow-y-auto px-4 sm:px-6 py-6 space-y-4 scroll-smooth scrollbar-premium bg-bg-subtle/30"
            >
              {isLoadingMessages || (isConversationPersisted && isFetchingMessages && messages.length === 0) ? (
                <div className="flex items-center justify-center h-full text-text-tertiary text-sm gap-2">
                  <Loader2 size={16} className="animate-spin" />
                  <span>{t("chat.thread_loading", "Loading messages…")}</span>
                </div>
              ) : messages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-center gap-3 px-6">
                  <div className="relative">
                    <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-brand-50 to-brand-100 text-brand-600 flex items-center justify-center border border-brand-200/60 shadow-elevation-1">
                      <MessageCircle size={26} />
                    </div>
                    <div aria-hidden className="absolute inset-0 rounded-2xl bg-brand-500/15 blur-2xl -z-10" />
                  </div>
                  <h4 className="text-base font-bold text-text-primary tracking-tight">
                    {t("chat.thread_empty_title", "No messages yet")}
                  </h4>
                  <p className="text-sm text-text-secondary max-w-xs leading-relaxed">
                    {t("chat.thread_empty_desc", "Be the first to say hello.")}
                  </p>
                </div>
              ) : (
                groupedMessages.map((group) => (
                  <div key={group.key} className="space-y-1.5">
                    {/* Day separator — pill with a soft gradient outline so it
                        stands apart from regular bubbles without competing. */}
                    <div className="flex items-center gap-3 py-2">
                      <div aria-hidden className="h-px flex-1 bg-gradient-to-r from-transparent to-border-default" />
                      <span className="text-[10px] uppercase tracking-wider text-text-secondary font-bold px-3 py-1 bg-bg-elevated border border-border-subtle rounded-full shadow-elevation-1">
                        {group.label}
                      </span>
                      <div aria-hidden className="h-px flex-1 bg-gradient-to-l from-transparent to-border-default" />
                    </div>
                    {group.items.map((msg, idx) => {
                      const isMe = msg.senderId === currentUser?.id;
                      // Group consecutive messages from the same sender into a
                      // tight stack — only show the avatar on the *last* message
                      // of an incoming run, so the thread feels less cluttered.
                      const next = group.items[idx + 1];
                      const isLastFromSender = !next || next.senderId !== msg.senderId;
                      const prev = group.items[idx - 1];
                      const isFirstFromSender = !prev || prev.senderId !== msg.senderId;
                      return (
                        <div
                          key={msg.id}
                          className={`flex items-end gap-2 ${
                            isMe ? "justify-end" : "justify-start"
                          } ${isLastFromSender ? "pb-1.5" : ""}`}
                        >
                          {/* Incoming-only avatar gutter — keeps the bubble
                              aligned even when the avatar is hidden (mid-run). */}
                          {!isMe && (
                            <div className="w-7 flex-shrink-0">
                              {isLastFromSender && (
                                <UserAvatar
                                  userId={msg.senderId}
                                  name={selectedConv!.otherParticipantName}
                                  className="w-7 h-7 ring-2 ring-bg-elevated"
                                  initialsClassName="text-[10px]"
                                />
                              )}
                            </div>
                          )}
                          <div
                            className={`max-w-[75%] px-3.5 py-2 shadow-elevation-1 ${
                              isMe
                                ? "bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm"
                                : "bg-bg-elevated text-text-primary border border-border-subtle"
                            } ${
                              // Bubble-tail corners: rounded on the OUTside,
                              // squared on the side that meets the previous /
                              // next message in the same sender's run.
                              isMe
                                ? `rounded-2xl ${isFirstFromSender ? "" : "rounded-tr-md"} ${isLastFromSender ? "" : "rounded-br-md"}`
                                : `rounded-2xl ${isFirstFromSender ? "" : "rounded-tl-md"} ${isLastFromSender ? "" : "rounded-bl-md"}`
                            }`}
                          >
                            <p className="text-sm leading-relaxed whitespace-pre-wrap break-words">
                              {msg.body}
                            </p>
                            {/* Only show timestamp on the LAST message of a run
                                so a back-and-forth doesn't get noisy. */}
                            {isLastFromSender && (
                              <div
                                className={`text-[10px] mt-0.5 tabular-nums ${
                                  isMe ? "text-white/75 text-end" : "text-text-tertiary"
                                }`}
                              >
                                {formatTime(msg.sentAt)}
                              </div>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                ))
              )}

              {typingUser && (
                <div className="flex items-end gap-2 justify-start">
                  <div className="w-7 flex-shrink-0">
                    <UserAvatar
                      userId={selectedConv!.otherParticipantId}
                      name={selectedConv!.otherParticipantName}
                      className="w-7 h-7 ring-2 ring-bg-elevated"
                      initialsClassName="text-[10px]"
                    />
                  </div>
                  <div className="bg-bg-elevated border border-border-subtle rounded-2xl rounded-bl-md px-4 py-2.5 shadow-elevation-1">
                    <div className="flex gap-1 items-center" aria-label={t("chat.typing_indicator", "{{name}} is typing…", { name: selectedConv!.otherParticipantName })}>
                      <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce" />
                      <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce [animation-delay:0.15s]" />
                      <span className="w-1.5 h-1.5 bg-brand-400 rounded-full animate-bounce [animation-delay:0.3s]" />
                    </div>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input */}
            <div className="px-4 py-3 bg-bg-elevated border-t border-border-subtle">
              <form
                onSubmit={handleSendMessage}
                className={`flex gap-2 items-end rounded-2xl border bg-bg-subtle/60 px-3 py-2 transition-colors focus-within:border-brand-300 focus-within:bg-bg-elevated focus-within:ring-2 focus-within:ring-brand-400/30 ${
                  isBlocked ? "border-danger-200" : "border-border-subtle"
                }`}
              >
                <textarea
                  value={messageBody}
                  onChange={handleTyping}
                  disabled={isBlocked}
                  // FR-MSG-12: cap the body at 2000 chars (matches the server validator).
                  maxLength={2000}
                  placeholder={
                    isBlocked
                      ? t("chat.input_disabled_blocked", "Conversation is blocked.")
                      : t("chat.type_placeholder", "Type a message...")
                  }
                  className="flex-1 bg-transparent text-text-primary outline-none text-sm resize-none disabled:opacity-60 disabled:cursor-not-allowed py-1.5 max-h-32"
                  rows={1}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      if (!isBlocked && !isSending && messageBody.length <= 2000) handleSendMessage(e);
                    }
                  }}
                />
                <button
                  type="submit"
                  disabled={!messageBody.trim() || isBlocked || isSending || messageBody.length > 2000}
                  aria-label={t("chat.send", "Send")}
                  className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand-sm transition-all hover:shadow-brand-md hover:from-brand-600 hover:to-brand-800 active:scale-95 disabled:bg-none disabled:bg-bg-subtle disabled:text-text-tertiary disabled:shadow-none disabled:cursor-not-allowed flex-shrink-0"
                >
                  {isSending ? (
                    <Loader2 size={18} className="animate-spin" />
                  ) : (
                    <Send size={18} className={isRtl ? "rotate-180" : ""} />
                  )}
                </button>
              </form>
              {messageBody.length > 1800 && (
                <p
                  className={`mt-1 text-end text-xs ${
                    messageBody.length >= 2000 ? "text-danger-500" : "text-text-tertiary"
                  }`}
                >
                  {messageBody.length}/2000
                </p>
              )}
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-bg-subtle/30 relative overflow-hidden">
            <div aria-hidden className="bg-mesh-hero pointer-events-none absolute inset-0 opacity-50" />
            <div className="relative z-[1] flex flex-col items-center">
              <div className="relative">
                <div className="w-20 h-20 bg-gradient-to-br from-brand-50 to-brand-100 rounded-3xl flex items-center justify-center mb-6 text-brand-600 border border-brand-200/60 shadow-elevation-2">
                  <MessageCircle size={34} />
                </div>
                <div aria-hidden className="absolute inset-0 rounded-3xl bg-brand-500/15 blur-3xl -z-10" />
              </div>
              <h3 className="text-xl font-bold mb-2 text-text-primary tracking-tight">
                {t("chat.empty_title", "Your Conversations")}
              </h3>
              <p className="text-sm text-text-secondary max-w-xs mx-auto mb-6 leading-relaxed">
                {t("chat.empty_desc", "Pick a conversation from the sidebar, or start a new message.")}
              </p>
              <button
                type="button"
                onClick={() => setIsComposeOpen(true)}
                className="btn btn-primary"
              >
                <PenSquare size={14} />
                {t("chat.new_message", "New Message")}
              </button>
            </div>
          </div>
        )}
      </main>

      <NewMessageModal
        isOpen={isComposeOpen}
        onOpenChange={setIsComposeOpen}
        onSelectContact={handleSelectContact}
      />

      <ConfirmDialog
        open={isBlockConfirmOpen}
        onOpenChange={setBlockConfirmOpen}
        title={
          selectedConv?.isBlocked
            ? t("chat.unblock_user", "Unblock user")
            : t("chat.block_user", "Block user")
        }
        description={
          selectedConv?.isBlocked
            ? t(
                "chat.unblock_confirm",
                "Unblock this person? You will be able to message each other again.",
              )
            : t(
                "chat.block_confirm",
                "Block this person? You will no longer be able to message each other.",
              )
        }
        confirmLabel={
          selectedConv?.isBlocked
            ? t("chat.unblock_user", "Unblock user")
            : t("chat.block_user", "Block user")
        }
        variant={selectedConv?.isBlocked ? "default" : "destructive"}
        loading={isBlockSubmitting}
        onConfirm={confirmToggleBlock}
      />
    </div>
  );
}
