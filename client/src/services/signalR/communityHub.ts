import type { HubConnection } from "@microsoft/signalr";
import { createCommunityHubConnection, attachLifecycleHandlers } from "./hubs";
import type { ConnectionLifecycleHandlers } from "./hubs";

export interface CommunityHubClient {
  onNewPostCreated: (callback: (postId: string) => void) => void;
  offNewPostCreated: (callback: (postId: string) => void) => void;
  
  onNewReplyCreated: (callback: (replyId: string) => void) => void;
  offNewReplyCreated: (callback: (replyId: string) => void) => void;

  joinCategory: (categorySlug: string) => Promise<void>;
  leaveCategory: (categorySlug: string) => Promise<void>;

  start: () => Promise<void>;
  stop: () => Promise<void>;
}

export function createTypedCommunityHub(handlers?: ConnectionLifecycleHandlers): CommunityHubClient {
  const connection: HubConnection = createCommunityHubConnection();

  if (handlers) {
    attachLifecycleHandlers(connection, handlers);
  }

  return {
    onNewPostCreated: (callback) => connection.on("NewPostCreated", callback),
    offNewPostCreated: (callback) => connection.off("NewPostCreated", callback),

    onNewReplyCreated: (callback) => connection.on("NewReplyCreated", callback),
    offNewReplyCreated: (callback) => connection.off("NewReplyCreated", callback),

    joinCategory: (categorySlug) => connection.invoke("JoinCategory", categorySlug),
    leaveCategory: (categorySlug) => connection.invoke("LeaveCategory", categorySlug),

    start: () => connection.start(),
    stop: () => connection.stop(),
  };
}
