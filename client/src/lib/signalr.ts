import * as signalR from "@microsoft/signalr";
import { useAuthStore } from "@/stores/authStore";

export function createHubConnection(hubPath: string, accessToken: string) {
  // Hub origin must follow the deployed API. The project-wide env var is
  // VITE_API_BASE_URL (the older VITE_API_URL never existed, so the hub URL
  // silently collapsed to the SPA's own origin on Azure). An empty base
  // yields a same-origin path that the Vite dev server proxies.
  const apiBase = import.meta.env.VITE_API_BASE_URL ?? "";
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${apiBase}${hubPath}`, {
      // Read the CURRENT token from the store on every (re)connect rather than
      // capturing it in a closure — otherwise withAutomaticReconnect re-auths
      // with the token that was live at connect time, so after a refresh rotation
      // the reconnect fails and live chat/typing/presence silently stop.
      accessTokenFactory: () =>
        useAuthStore.getState().tokens?.accessToken ?? accessToken,
      // Keep the negotiate step and offer every transport so the connection
      // still works on hosts where raw WebSockets are unavailable.
      transport:
        signalR.HttpTransportType.WebSockets |
        signalR.HttpTransportType.ServerSentEvents |
        signalR.HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}
