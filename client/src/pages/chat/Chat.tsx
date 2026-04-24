import { useState, useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import { 
  Send, 
  Search, 
  MoreVertical, 
  Circle,
  MessageCircle,
  Clock,
  Slash
} from "lucide-react";
import { chatApi, type ChatConversation, type ChatMessage } from "@/services/api/chat";
import { useAuthStore } from "@/stores/authStore";
import { createHubConnection } from "@/lib/signalr";
import type { HubConnection } from "@microsoft/signalr";
import { formatDistanceToNow } from "date-fns";
import { ar } from "date-fns/locale";
import { useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";

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

  const hubConnectionRef = useRef<HubConnection | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { data: conversations = [] } = useQuery({
    queryKey: ["chat", "conversations"],
    queryFn: () => chatApi.getConversations(),
  });

  const { data: messages = [] } = useQuery({
    queryKey: ["chat", "messages", selectedConv?.id],
    queryFn: () => chatApi.getMessages(selectedConv!.id),
    enabled: !!selectedConv?.id,
    placeholderData: keepPreviousData,
  });

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    if (!accessToken) return;
    
    const connection = createHubConnection("/hubs/chat", accessToken);
    
    connection.on("MessageReceived", (message: ChatMessage) => {
      // If the message belongs to current conversation, refresh messages
      if (selectedConv && (message.senderId === selectedConv.otherParticipantId || message.senderId === currentUser?.id)) {
        void qc.invalidateQueries({ queryKey: ["chat", "messages", selectedConv.id] });
      }
      // Always refresh conversations to update last message/timestamp
      void qc.invalidateQueries({ queryKey: ["chat", "conversations"] });
    });

    connection.on("TypingStart", (conversationId: string, userId: string) => {
      if (selectedConv?.id === conversationId && userId !== currentUser?.id) {
        setTypingUser(userId);
      }
    });

    connection.on("TypingStop", (conversationId: string) => {
      if (selectedConv?.id === conversationId) {
        setTypingUser(null);
      }
    });

    void connection.start().then(() => {
      hubConnectionRef.current = connection;
    });

    return () => {
      void connection.stop();
    };
  }, [accessToken, selectedConv, currentUser?.id, qc]);

  useEffect(() => {
    if (selectedConv && hubConnectionRef.current) {
      void hubConnectionRef.current.invoke("JoinConversation", selectedConv.id);
    }
  }, [selectedConv]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedConv || !messageBody.trim()) return;

    try {
      await chatApi.sendMessage({
        recipientId: selectedConv.otherParticipantId,
        body: messageBody
      });
      
      setMessageBody("");
      void qc.invalidateQueries({ queryKey: ["chat", "messages", selectedConv.id] });
      void qc.invalidateQueries({ queryKey: ["chat", "conversations"] });
    } catch (error) {
      console.error("Failed to send message", error);
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

  return (
    <div className="flex h-[calc(100vh-120px)] max-w-6xl mx-auto bg-bg-elevated rounded-3xl border border-border-subtle shadow-lg overflow-hidden my-4">
      {/* Conversation List */}
      <aside className="w-full md:w-80 border-r border-border-subtle flex flex-col bg-bg-muted/50">
        <div className="p-4 border-b border-border-subtle">
          <div className="relative mb-4">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary" size={16} />
            <input
              type="text"
              placeholder={t("chat.search_placeholder", "Search messages...")}
              className="w-full pl-9 pr-4 py-2 bg-bg-elevated border border-border-subtle rounded-xl text-sm outline-none focus:ring-2 focus:ring-brand-400 transition-all"
            />
          </div>
          <h2 className="text-xs font-bold text-text-tertiary uppercase tracking-wider px-1">
            {t("chat.messages", "Direct Messages")}
          </h2>
        </div>

        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {conversations.map((conv) => (
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
                <div className="w-12 h-12 rounded-full bg-brand-100 flex items-center justify-center text-brand-600 font-bold border-2 border-white">
                  {conv.otherParticipantName[0]}
                </div>
                {conv.isOnline && (
                  <div className="absolute bottom-0 right-0 w-3 h-3 bg-success-500 border-2 border-white rounded-full" />
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
          ))}
        </div>
      </aside>

      {/* Chat Window */}
      <main className="flex-1 flex flex-col bg-bg-elevated relative">
        {selectedConv ? (
          <>
            {/* Header */}
            <div className="p-4 border-b border-border-subtle flex items-center justify-between bg-white/50 backdrop-blur-md sticky top-0 z-10">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-brand-100 flex items-center justify-center text-sm text-brand-600 font-bold border border-border-subtle">
                  {selectedConv.otherParticipantName[0]}
                </div>
                <div>
                  <h3 className="text-sm font-bold">{selectedConv.otherParticipantName}</h3>
                  <span className="text-[10px] text-success-500 flex items-center gap-1 font-medium">
                    <Circle size={8} fill="currentColor" />
                    {selectedConv.isOnline ? t("chat.online", "Online") : t("chat.offline", "Offline")}
                  </span>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <button className="p-2 text-text-tertiary hover:bg-bg-subtle rounded-full transition-colors">
                  <Slash size={18} />
                </button>
                <button className="p-2 text-text-tertiary hover:bg-bg-subtle rounded-full transition-colors">
                  <MoreVertical size={18} />
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
                  className="flex-1 bg-white border border-border-subtle rounded-2xl p-3 px-4 outline-none focus:ring-2 focus:ring-brand-400 transition-all text-sm resize-none h-12"
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
            <p className="text-text-secondary max-w-xs mx-auto">
              {t("chat.empty_desc", "Select a contact from the sidebar to start chatting or share knowledge.")}
            </p>
          </div>
        )}
      </main>
    </div>
  );
}
