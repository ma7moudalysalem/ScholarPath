import { useState, useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import {
  Send,
  Search,
  Circle,
  MessageCircle,
  Clock,
  Slash,
  PenSquare,
} from "lucide-react";
import { toast } from "sonner";
import { chatApi, type ChatContact, type ChatConversation, type ChatMessage } from "@/services/api/chat";
import { useAuthStore } from "@/stores/authStore";
import { UserAvatar } from "@/components/common/UserAvatar";
import { createHubConnection } from "@/lib/signalr";
import { usePresenceStore } from "@/stores/presenceStore";
import type { HubConnection } from "@microsoft/signalr";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { NewMessageModal } from "@/components/chat/NewMessageModal";

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

  const hubConnectionRef = useRef<HubConnection | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  // Refs let the once-registered hub handlers read the latest selection /
  // identity without the connection effect re-running (and reconnecting).
  const selectedConvRef = useRef<ChatConversation | null>(null);
  const currentUserIdRef = useRef<string | undefined>(currentUser?.id);

  // Live chat presence — drives the online dots in the list + thread header.
  const onlineUserIds = usePresenceStore((state) => state.onlineUserIds);
  const selectedParticipantOnline = selectedConv
    ? onlineUserIds.has(selectedConv.otherParticipantId) || selectedConv.isOnline
    : false;

  const { data: conversations = [] } = useQuery({
    queryKey: ["chat", "conversations"],
    queryFn: () => chatApi.getConversations(),
  });

  const filteredConversations = conversations.filter((conv) =>
    conv.otherParticipantName.toLowerCase().includes(conversationSearch.trim().toLowerCase()),
  );

  // A "pending:" id is a not-yet-created conversation from the compose flow —
  // skip the message fetch until the first send turns it into a real one.
  const isConversationPersisted = !!selectedConv && !selectedConv.id.startsWith("pending:");

  const { data: messages = [] } = useQuery({
    queryKey: ["chat", "messages", selectedConv?.id],
    queryFn: () => chatApi.getMessages(selectedConv!.id),
    enabled: isConversationPersisted,
    placeholderData: keepPreviousData,
  });

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Keep the handler-facing refs in sync with the latest render values.
  useEffect(() => {
    selectedConvRef.current = selectedConv;
  }, [selectedConv]);

  useEffect(() => {
    currentUserIdRef.current = currentUser?.id;
  }, [currentUser?.id]);

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
      usePresenceStore.getState().reset();
      void connection.stop();
    };
  }, [accessToken, qc]);

  useEffect(() => {
    // Skip pending (not-yet-created) conversations — their id is not a real
    // server-side conversation the hub can join.
    if (selectedConv && isConversationPersisted && hubConnectionRef.current) {
      void hubConnectionRef.current.invoke("JoinConversation", selectedConv.id);
    }
  }, [selectedConv, isConversationPersisted]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedConv || !messageBody.trim()) return;

    // A "pending:" id means this is a freshly composed conversation that does
    // not exist server-side yet — the send below auto-creates it.
    const isPending = selectedConv.id.startsWith("pending:");

    try {
      await chatApi.sendMessage({
        recipientId: selectedConv.otherParticipantId,
        body: messageBody,
      });

      setMessageBody("");
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
      toast.error(t("chat.send_error", "Could not send your message."));
    }
  };

  const handleTyping = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setMessageBody(e.target.value);
    if (!isTyping && selectedConv && hubConnectionRef.current) {
      setIsTyping(true);
      void hubConnectionRef.current.invoke("TypingStart", selectedConv.id);
      setTimeout(() => {
        setIsTyping(false);
        if (hubConnectionRef.current) {
          void hubConnectionRef.current.invoke("TypingStop", selectedConv!.id);
        }
      }, 3000);
    }
  };

  // Compose: pick a contact → open the existing conversation if there is one,
  // otherwise stage a placeholder. POST /api/chat/messages auto-creates the
  // real conversation on the first message, so the next refresh fills it in.
  const handleSelectContact = (contact: ChatContact) => {
    const existing = conversations.find((c) => c.otherParticipantId === contact.id);
    if (existing) {
      setSelectedConv(existing);
      return;
    }
    setSelectedConv({
      id: `pending:${contact.id}`,
      otherParticipantId: contact.id,
      otherParticipantName: contact.name,
      otherParticipantAvatarUrl: contact.photoUrl,
      isOnline: false,
      isBlocked: false,
    });
  };

  const handleToggleBlock = async () => {
    if (!selectedConv) return;
    const isBlocked = selectedConv.isBlocked;
    const confirmed = window.confirm(
      isBlocked
        ? t("chat.unblock_confirm", "Unblock this person? You will be able to message each other again.")
        : t("chat.block_confirm", "Block this person? You will no longer be able to message each other."),
    );
    if (!confirmed) return;

    try {
      if (isBlocked) {
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
      setSelectedConv(updated ?? { ...selectedConv, isBlocked: !isBlocked });
    } catch (error) {
      console.error("Failed to toggle block", error);
      toast.error(
        isBlocked
          ? t("chat.unblock_error", "Could not unblock this user.")
          : t("chat.block_error", "Could not block this user."),
      );
    }
  };

  return (
    <div className="flex h-[calc(100vh-120px)] max-w-6xl mx-auto bg-bg-elevated rounded-3xl border border-border-subtle shadow-lg overflow-hidden my-4">
      {/* Conversation List */}
      <aside className="w-full md:w-80 border-r border-border-subtle flex flex-col bg-bg-muted/50">
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
            <Search className="absolute start-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={16} />
            <input
              type="text"
              value={conversationSearch}
              onChange={(e) => setConversationSearch(e.target.value)}
              placeholder={t("chat.search_placeholder", "Search conversations...")}
              className="w-full ps-9 pe-4 py-2 bg-bg-elevated border border-border-subtle rounded-xl text-sm outline-none focus:ring-2 focus:ring-brand-400 transition-all"
            />
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {filteredConversations.length === 0 ? (
            <div className="px-4 py-10 text-center text-sm text-text-tertiary">
              {conversationSearch.trim()
                ? t("chat.no_search_results", "No conversations match your search.")
                : t("chat.no_conversations", "No conversations yet. Start a new message.")}
            </div>
          ) : (
            filteredConversations.map((conv) => (
              <button
                key={conv.id}
                onClick={() => setSelectedConv(conv)}
                className={`w-full flex items-center gap-3 p-3 rounded-2xl transition-all ${
                  selectedConv?.id === conv.id
                    ? "bg-brand-500 text-white shadow-md shadow-brand-500/20"
                    : "hover:bg-bg-subtle"
                }`}
              >
                <div className="relative">
                  <UserAvatar
                    userId={conv.otherParticipantId}
                    name={conv.otherParticipantName}
                    className="w-12 h-12 border-2 border-white"
                  />
                  {(onlineUserIds.has(conv.otherParticipantId) || conv.isOnline) && (
                    <div className="absolute bottom-0 end-0 w-3 h-3 bg-success-500 border-2 border-bg-elevated rounded-full" />
                  )}
                </div>
                <div className="flex-1 text-start overflow-hidden">
                  <div className="flex justify-between items-baseline mb-1">
                    <h4 className="text-sm font-bold truncate">{conv.otherParticipantName}</h4>
                    {conv.lastMessageAt && (
                      <span className={`text-[10px] ${selectedConv?.id === conv.id ? "text-white/70" : "text-text-tertiary"}`}>
                        {formatDistanceToNow(new Date(conv.lastMessageAt), { addSuffix: false, locale: dateLocale })}
                      </span>
                    )}
                  </div>
                  <p className={`text-xs truncate ${selectedConv?.id === conv.id ? "text-white/80" : "text-text-secondary"}`}>
                    {conv.lastMessageBody || t("chat.start_conversation", "No messages yet")}
                  </p>
                </div>
              </button>
            ))
          )}
        </div>
      </aside>

      {/* Chat Window */}
      <main className="flex-1 flex flex-col bg-bg-elevated relative">
        {selectedConv ? (
          <>
            {/* Header */}
            <div className="p-4 border-b border-border-subtle flex items-center justify-between bg-bg-elevated/80 backdrop-blur-md sticky top-0 z-10">
              <div className="flex items-center gap-3">
                <UserAvatar
                  userId={selectedConv.otherParticipantId}
                  name={selectedConv.otherParticipantName}
                  className="w-10 h-10 border border-border-subtle"
                />
                <div>
                  <h3 className="text-sm font-bold">{selectedConv.otherParticipantName}</h3>
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
                  onClick={handleToggleBlock}
                  title={
                    selectedConv.isBlocked
                      ? t("chat.unblock_user", "Unblock user")
                      : t("chat.block_user", "Block user")
                  }
                  aria-label={
                    selectedConv.isBlocked
                      ? t("chat.unblock_user", "Unblock user")
                      : t("chat.block_user", "Block user")
                  }
                  className={`flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-full transition-colors ${
                    selectedConv.isBlocked
                      ? "text-brand-500 hover:bg-brand-50"
                      : "text-danger-500 hover:bg-danger-50"
                  }`}
                >
                  <Slash size={16} />
                  <span className="hidden sm:inline">
                    {selectedConv.isBlocked
                      ? t("chat.unblock_user", "Unblock user")
                      : t("chat.block_user", "Block user")}
                  </span>
                </button>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-6 space-y-4">
              {messages.map((msg) => {
                const isMe = msg.senderId === currentUser?.id;
                return (
                  <div key={msg.id} className={`flex ${isMe ? "justify-end" : "justify-start"}`}>
                    <div className={`max-w-[75%] rounded-2xl p-4 shadow-sm ${
                      isMe 
                        ? "bg-brand-500 text-white rounded-tr-none" 
                        : "bg-bg-subtle text-text-primary rounded-tl-none"
                    }`}>
                      <p className="text-sm leading-relaxed">{msg.body}</p>
                      <div className={`text-[10px] mt-1 flex items-center gap-1 ${isMe ? "text-white/60 justify-end" : "text-text-tertiary"}`}>
                        <Clock size={10} />
                        <span>{new Date(msg.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                      </div>
                    </div>
                  </div>
                );
              })}
              {typingUser && (
                <div className="flex justify-start">
                  <div className="bg-bg-subtle rounded-2xl rounded-tl-none p-3 px-4">
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
              <form onSubmit={handleSendMessage} className="flex gap-2 items-center">
                <textarea
                  value={messageBody}
                  onChange={handleTyping}
                  placeholder={t("chat.type_placeholder", "Type a message...")}
                  className="flex-1 bg-bg-elevated text-text-primary border border-border-subtle rounded-2xl p-3 px-4 outline-none focus:ring-2 focus:ring-brand-400 transition-all text-sm resize-none h-12"
                  rows={1}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      handleSendMessage(e);
                    }
                  }}
                />
                <button
                  type="submit"
                  disabled={!messageBody.trim()}
                  className="p-3 bg-brand-500 text-white rounded-2xl shadow-lg hover:bg-brand-600 disabled:opacity-50 disabled:shadow-none transition-all flex-shrink-0"
                >
                  <Send size={20} />
                </button>
              </form>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center p-8 text-center bg-bg-muted/10">
            <div className="w-20 h-20 bg-brand-50 rounded-3xl flex items-center justify-center mb-6 text-brand-500 shadow-sm border border-brand-100">
              <MessageCircle size={40} />
            </div>
            <h3 className="text-xl font-bold mb-2">{t("chat.empty_title", "Your Conversations")}</h3>
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
    </div>
  );
}
