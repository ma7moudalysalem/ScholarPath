import { useState, useEffect, useMemo, useRef } from "react";
import { useTranslation } from "react-i18next";
import {
  Send,
  Search,
  Circle,
  MessageCircle,
  Slash,
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
    <div className="p-4 border-b border-border-subtle flex items-center justify-between bg-bg-elevated/80 backdrop-blur-md sticky top-0 z-10">
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={() => selectConversation(null)}
          aria-label={t("chat.back_to_list", "Back to conversations")}
          className="md:hidden -ms-1 me-1 p-1.5 rounded-full text-text-secondary hover:bg-bg-subtle transition-colors"
        >
          <ArrowLeft size={18} className={isRtl ? "rotate-180" : ""} />
        </button>
        <UserAvatar
          userId={selectedConv!.otherParticipantId}
          name={selectedConv!.otherParticipantName}
          className="w-10 h-10 border border-border-subtle flex-shrink-0"
        />
        <div className="min-w-0">
          <h3 className="text-sm font-bold truncate">{selectedConv!.otherParticipantName}</h3>
          <span
            className={`text-[10px] flex items-center gap-1 font-medium ${
              selectedParticipantOnline ? "text-success-500" : "text-text-tertiary"
            }`}
          >
            <Circle size={8} fill="currentColor" />
            {selectedParticipantOnline
              ? t("chat.online", "Online")
              : t("chat.offline", "Offline")}
          </span>
        </div>
      </div>
      <div className="flex items-center gap-2">
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
          className={`flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-full transition-colors ${
            selectedConv!.isBlocked
              ? "text-brand-500 hover:bg-brand-50"
              : "text-danger-500 hover:bg-danger-50"
          }`}
        >
          <Slash size={16} />
          <span className="hidden sm:inline">
            {selectedConv!.isBlocked
              ? t("chat.unblock_user", "Unblock user")
              : t("chat.block_user", "Block user")}
          </span>
        </button>
      </div>
    </div>
  );

  return (
    <div className="flex h-[calc(100vh-120px)] max-w-6xl mx-auto bg-bg-elevated rounded-3xl border border-border-subtle shadow-lg overflow-hidden my-4">
      {/* Conversation List — full-width on mobile when no thread is open. */}
      <aside
        className={`${
          selectedConv ? "hidden md:flex" : "flex"
        } w-full md:w-80 border-e border-border-subtle flex-col bg-bg-muted/50 flex-shrink-0`}
      >
        <div className="p-4 border-b border-border-subtle">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xs font-bold text-text-tertiary uppercase tracking-wider px-1">
              {t("chat.messages", "Direct Messages")}
            </h2>
            <button
              type="button"
              onClick={() => setIsComposeOpen(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-brand-500 text-white text-xs font-bold rounded-xl shadow-sm hover:bg-brand-600 transition-all"
            >
              <PenSquare size={14} />
              {t("chat.new_message", "New Message")}
            </button>
          </div>
          <div className="relative">
            <Search
              className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary"
              size={16}
              aria-hidden
            />
            <input
              type="text"
              value={conversationSearch}
              onChange={(e) => setConversationSearch(e.target.value)}
              placeholder={t("chat.search_placeholder", "Search conversations...")}
              aria-label={t("chat.header_search_aria", "Search conversations")}
              className="w-full ps-9 pe-4 py-2 bg-bg-elevated border border-border-subtle rounded-xl text-sm outline-none focus:ring-2 focus:ring-brand-400 transition-all"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {filteredConversations.length === 0 ? (
            <div className="px-4 py-10 text-center text-sm text-text-tertiary">
              {conversationSearch.trim()
                ? t("chat.search_no_match", "No matching conversations.")
                : t("chat.no_conversations", "No conversations yet. Start a new message.")}
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
                  className={`group w-full flex items-center gap-3 p-3 rounded-2xl transition-all ${
                    isActive
                      ? "bg-brand-500 text-white shadow-md shadow-brand-500/20"
                      : "hover:bg-bg-subtle"
                  }`}
                >
                  <div className="relative flex-shrink-0">
                    <UserAvatar
                      userId={conv.otherParticipantId}
                      name={conv.otherParticipantName}
                      className="w-12 h-12 border-2 border-white"
                    />
                    {isOnlineDot && (
                      <div className="absolute bottom-0 end-0 w-3 h-3 bg-success-500 border-2 border-bg-elevated rounded-full" />
                    )}
                  </div>
                  <div className="flex-1 text-start overflow-hidden min-w-0">
                    <div className="flex justify-between items-baseline mb-1 gap-2">
                      <h4 className="text-sm font-bold truncate">
                        {conv.otherParticipantName}
                      </h4>
                      {conv.lastMessageAt && (
                        <span
                          className={`text-[10px] flex-shrink-0 ${
                            isActive ? "text-white/70" : "text-text-tertiary"
                          }`}
                        >
                          {formatDistanceToNow(new Date(conv.lastMessageAt), {
                            addSuffix: false,
                            locale: dateLocale,
                          })}
                        </span>
                      )}
                    </div>
                    <p
                      className={`text-xs truncate ${
                        isActive ? "text-white/80" : "text-text-secondary"
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
              className="flex-1 overflow-y-auto p-6 space-y-4 scroll-smooth"
            >
              {isLoadingMessages || (isConversationPersisted && isFetchingMessages && messages.length === 0) ? (
                <div className="flex items-center justify-center h-full text-text-tertiary text-sm gap-2">
                  <Loader2 size={16} className="animate-spin" />
                  <span>{t("chat.thread_loading", "Loading messages…")}</span>
                </div>
              ) : messages.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-center text-text-tertiary gap-2 px-6">
                  <div className="w-14 h-14 rounded-full bg-brand-50 text-brand-500 flex items-center justify-center border border-brand-100">
                    <MessageCircle size={24} />
                  </div>
                  <h4 className="text-sm font-bold text-text-secondary">
                    {t("chat.thread_empty_title", "No messages yet")}
                  </h4>
                  <p className="text-xs max-w-xs">
                    {t("chat.thread_empty_desc", "Be the first to say hello.")}
                  </p>
                </div>
              ) : (
                groupedMessages.map((group) => (
                  <div key={group.key} className="space-y-3">
                    <div className="flex items-center gap-3 py-1">
                      <div className="flex-1 h-px bg-border-subtle" />
                      <span className="text-[10px] uppercase tracking-wider text-text-tertiary font-bold px-2">
                        {group.label}
                      </span>
                      <div className="flex-1 h-px bg-border-subtle" />
                    </div>
                    {group.items.map((msg) => {
                      const isMe = msg.senderId === currentUser?.id;
                      return (
                        <div
                          key={msg.id}
                          className={`flex ${isMe ? "justify-end" : "justify-start"}`}
                        >
                          <div
                            className={`max-w-[75%] rounded-2xl px-4 py-2.5 shadow-sm ${
                              isMe
                                ? "bg-brand-500 text-white"
                                : "bg-bg-subtle text-text-primary border border-border-subtle"
                            }`}
                          >
                            <p className="text-sm leading-relaxed whitespace-pre-wrap break-words">
                              {msg.body}
                            </p>
                            <div
                              className={`text-[10px] mt-1 ${
                                isMe ? "text-white/70 text-end" : "text-text-tertiary"
                              }`}
                            >
                              {formatTime(msg.sentAt)}
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                ))
              )}

              {typingUser && (
                <div className="flex justify-start">
                  <div className="bg-bg-subtle border border-border-subtle rounded-2xl p-3 px-4">
                    <div className="flex gap-1">
                      <span className="w-1.5 h-1.5 bg-text-tertiary rounded-full animate-bounce" />
                      <span className="w-1.5 h-1.5 bg-text-tertiary rounded-full animate-bounce [animation-delay:0.2s]" />
                      <span className="w-1.5 h-1.5 bg-text-tertiary rounded-full animate-bounce [animation-delay:0.4s]" />
                    </div>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input */}
            <div className="p-4 bg-bg-muted/30 border-t border-border-subtle">
              <form onSubmit={handleSendMessage} className="flex gap-2 items-end">
                <textarea
                  value={messageBody}
                  onChange={handleTyping}
                  disabled={isBlocked}
                  placeholder={
                    isBlocked
                      ? t("chat.input_disabled_blocked", "Conversation is blocked.")
                      : t("chat.type_placeholder", "Type a message...")
                  }
                  className="flex-1 bg-bg-elevated text-text-primary border border-border-subtle rounded-2xl p-3 px-4 outline-none focus:ring-2 focus:ring-brand-400 transition-all text-sm resize-none disabled:opacity-60 disabled:cursor-not-allowed"
                  rows={1}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      if (!isBlocked && !isSending) handleSendMessage(e);
                    }
                  }}
                />
                <button
                  type="submit"
                  disabled={!messageBody.trim() || isBlocked || isSending}
                  aria-label={t("chat.new_message", "New Message")}
                  className="p-3 bg-brand-500 text-white rounded-2xl shadow-lg hover:bg-brand-600 disabled:opacity-50 disabled:shadow-none disabled:cursor-not-allowed transition-all flex-shrink-0"
                >
                  {isSending ? (
                    <Loader2 size={20} className="animate-spin" />
                  ) : (
                    <Send size={20} className={isRtl ? "rotate-180" : ""} />
                  )}
                </button>
              </form>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-bg-muted/10">
            <div className="w-20 h-20 bg-brand-50 rounded-3xl flex items-center justify-center mb-6 text-brand-500 shadow-sm border border-brand-100">
              <MessageCircle size={40} />
            </div>
            <h3 className="text-xl font-bold mb-2">
              {t("chat.empty_title", "Your Conversations")}
            </h3>
            <p className="text-text-secondary max-w-xs mx-auto mb-6">
              {t("chat.empty_desc", "Pick a conversation from the sidebar, or start a new message.")}
            </p>
            <button
              type="button"
              onClick={() => setIsComposeOpen(true)}
              className="flex items-center gap-2 px-4 py-2.5 bg-brand-500 text-white text-sm font-bold rounded-2xl shadow-lg hover:bg-brand-600 transition-all"
            >
              <PenSquare size={16} />
              {t("chat.new_message", "New Message")}
            </button>
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
