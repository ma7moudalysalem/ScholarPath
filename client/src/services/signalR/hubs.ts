import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "@/stores/authStore";

// Hub origin is derived from the same base URL as the REST API so that the
// SignalR connection follows the deployed API (Azure App Service) instead of
// the SPA's own origin. An empty base falls back to a same-origin "/hubs/..."
// path, which the Vite dev server proxies (see vite.config.ts).
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

function buildConnection(path: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}${path}`, {
      // The factory is read on every (re)connect, so it always sends the
      // current access token even after a token refresh.
      accessTokenFactory: () => useAuthStore.getState().tokens?.accessToken ?? "",
      // Allow all transports and keep the negotiate step: skipping negotiation
      // forces raw WebSockets, which fails on hosts where WebSockets are not
      // enabled. Negotiation lets SignalR fall back to SSE / long-polling.
      transport:
        signalR.HttpTransportType.WebSockets |
        signalR.HttpTransportType.ServerSentEvents |
        signalR.HttpTransportType.LongPolling,
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
