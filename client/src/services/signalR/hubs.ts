import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "@/stores/authStore";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

function buildConnection(path: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}${path}`, {
      accessTokenFactory: () => useAuthStore.getState().tokens?.accessToken ?? "",
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: true,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}

export function createChatHubConnection() {
  return buildConnection("/hubs/chat");
}

export function createNotificationHubConnection() {
  return buildConnection("/hubs/notifications");
}

export function createCommunityHubConnection() {
  return buildConnection("/hubs/community");
}

export type ConnectionLifecycleHandlers = {
  onReconnecting?: () => void;
  onReconnected?: () => void;
  onClose?: (error?: Error) => void;
};

export function attachLifecycleHandlers(
  connection: signalR.HubConnection,
  handlers: ConnectionLifecycleHandlers,
) {
  if (handlers.onReconnecting) connection.onreconnecting(() => handlers.onReconnecting?.());
  if (handlers.onReconnected) connection.onreconnected(() => handlers.onReconnected?.());
  if (handlers.onClose) connection.onclose((error) => handlers.onClose?.(error ?? undefined));
}
