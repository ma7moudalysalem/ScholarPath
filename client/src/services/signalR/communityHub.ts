import type { HubConnection } from "@microsoft/signalr";
import { createCommunityHubConnection, attachLifecycleHandlers } from "./hubs";
import type { ConnectionLifecycleHandlers } from "./hubs";

export interface PostCreatedPayload {
  postId: string;
  categorySlug: string;
}

export interface ReplyCreatedPayload {
  replyId: string;
  parentPostId: string;
}

export interface CommunityHubClient {
  /**
   * Server event name: "PostCreated". Fired into the
   * `forum-category:{slug}` group when a new root post is created.
   */
  onPostCreated: (callback: (payload: PostCreatedPayload) => void) => void;
  offPostCreated: (callback: (payload: PostCreatedPayload) => void) => void;

  /**
   * Server event name: "ReplyCreated". Fired into the
   * `forum-post:{parentPostId}` group when a new reply is created.
   */
  onReplyCreated: (callback: (payload: ReplyCreatedPayload) => void) => void;
  offReplyCreated: (callback: (payload: ReplyCreatedPayload) => void) => void;

  joinCategory: (categorySlug: string) => Promise<void>;
  leaveCategory: (categorySlug: string) => Promise<void>;

  joinPost: (postId: string) => Promise<void>;
  leavePost: (postId: string) => Promise<void>;

  start: () => Promise<void>;
  stop: () => Promise<void>;
}

export function createTypedCommunityHub(handlers?: ConnectionLifecycleHandlers): CommunityHubClient {
  const connection: HubConnection = createCommunityHubConnection();

  if (handlers) {
    attachLifecycleHandlers(connection, handlers);
  }

  return {
    onPostCreated: (callback) => connection.on("PostCreated", callback),
    offPostCreated: (callback) => connection.off("PostCreated", callback),

    onReplyCreated: (callback) => connection.on("ReplyCreated", callback),
    offReplyCreated: (callback) => connection.off("ReplyCreated", callback),

    joinCategory: (categorySlug) => connection.invoke("JoinCategory", categorySlug),
    leaveCategory: (categorySlug) => connection.invoke("LeaveCategory", categorySlug),

    joinPost: (postId) => connection.invoke("JoinPost", postId),
    leavePost: (postId) => connection.invoke("LeavePost", postId),

    start: () => connection.start(),
    stop: () => connection.stop(),
  };
}
