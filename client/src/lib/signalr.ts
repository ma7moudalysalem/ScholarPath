import * as signalR from "@microsoft/signalr";

export function createHubConnection(hubPath: string, accessToken: string) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_URL || ""}${hubPath}`, {
      accessTokenFactory: () => accessToken,
      skipNegotiation: false,
      transport: signalR.HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}
