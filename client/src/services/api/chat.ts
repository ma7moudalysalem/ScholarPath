import { apiClient } from "@/services/api/client";

export interface ChatConversation {
  id: string;
  otherParticipantId: string;
  otherParticipantName: string;
  otherParticipantAvatarUrl?: string;
  lastMessageBody?: string;
  lastMessageAt?: string;
  isOnline: boolean;
}

export interface ChatMessage {
  id: string;
  senderId: string;
  body: string;
  sentAt: string;
  readAt?: string;
}

export const chatApi = {
  async getConversations(): Promise<ChatConversation[]> {
    const { data } = await apiClient.get<ChatConversation[]>("/api/chat/conversations");
    return data;
  },

  async getMessages(conversationId: string, params?: { limit?: number; before?: string }): Promise<ChatMessage[]> {
    const { data } = await apiClient.get<ChatMessage[]>(`/api/chat/conversations/${conversationId}/messages`, { params });
    return data;
  },

  async sendMessage(req: { recipientId: string; body: string }): Promise<string> {
    const { data } = await apiClient.post<string>("/api/chat/messages", req);
    return data;
  },

  async blockUser(req: { userId: string; reason?: string }): Promise<void> {
    await apiClient.post("/api/chat/blocks", req);
  },

  async unblockUser(userId: string): Promise<void> {
    await apiClient.delete(`/api/chat/blocks/${userId}`);
  },
};
