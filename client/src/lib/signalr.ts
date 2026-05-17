import * as signalR from "@microsoft/signalr";

export function createHubConnection(hubPath: string, accessToken: string) {
  // Hub origin must follow the deployed API. The project-wide env var is
  // VITE_API_BASE_URL (the older VITE_API_URL never existed, so the hub URL
  // silently collapsed to the SPA's own origin on Azure). An empty base
  // yields a same-origin path that the Vite dev server proxies.
  const apiBase = import.meta.env.VITE_API_BASE_URL ?? "";
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${apiBase}${hubPath}`, {
      accessTokenFactory: () => accessToken,
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
