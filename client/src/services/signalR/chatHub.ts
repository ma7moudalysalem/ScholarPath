import type { HubConnection } from "@microsoft/signalr";
import { createChatHubConnection, attachLifecycleHandlers } from "./hubs";
import type { ConnectionLifecycleHandlers } from "./hubs";

export interface ChatHubClient {
  onUserOnline: (callback: (userId: string) => void) => void;
  offUserOnline: (callback: (userId: string) => void) => void;

  onUserOffline: (callback: (userId: string) => void) => void;
  offUserOffline: (callback: (userId: string) => void) => void;

  onNewMessage: (callback: (messageId: string, conversationId: string, senderId: string, body: string, sentAt: string) => void) => void;
  offNewMessage: (callback: (messageId: string, conversationId: string, senderId: string, body: string, sentAt: string) => void) => void;

  onTypingStart: (callback: (conversationId: string, userId: string) => void) => void;
  offTypingStart: (callback: (conversationId: string, userId: string) => void) => void;

  onTypingStop: (callback: (conversationId: string, userId: string) => void) => void;
  offTypingStop: (callback: (conversationId: string, userId: string) => void) => void;

  joinConversation: (conversationId: string) => Promise<void>;
  leaveConversation: (conversationId: string) => Promise<void>;
  
  typingStart: (conversationId: string) => Promise<void>;
  typingStop: (conversationId: string) => Promise<void>;

  start: () => Promise<void>;
  stop: () => Promise<void>;
}

export function createTypedChatHub(handlers?: ConnectionLifecycleHandlers): ChatHubClient {
  const connection: HubConnection = createChatHubConnection();

  if (handlers) {
    attachLifecycleHandlers(connection, handlers);
  }

  return {
    onUserOnline: (callback) => connection.on("UserOnline", callback),
    offUserOnline: (callback) => connection.off("UserOnline", callback),

    onUserOffline: (callback) => connection.on("UserOffline", callback),
    offUserOffline: (callback) => connection.off("UserOffline", callback),

    onNewMessage: (callback) => connection.on("NewMessage", callback),
    offNewMessage: (callback) => connection.off("NewMessage", callback),

    onTypingStart: (callback) => connection.on("TypingStart", callback),
    offTypingStart: (callback) => connection.off("TypingStart", callback),

    onTypingStop: (callback) => connection.on("TypingStop", callback),
    offTypingStop: (callback) => connection.off("TypingStop", callback),

    joinConversation: (conversationId) => connection.invoke("JoinConversation", conversationId),
    leaveConversation: (conversationId) => connection.invoke("LeaveConversation", conversationId),
    
    typingStart: (conversationId) => connection.invoke("TypingStart", conversationId),
    typingStop: (conversationId) => connection.invoke("TypingStop", conversationId),

    start: () => connection.start(),
    stop: () => connection.stop(),
  };
}
