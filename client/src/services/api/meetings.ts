import { apiClient } from "@/services/api/client";

/**
 * Credentials for entering a booking's video session — mirrors the server
 * MeetingJoinResult. Calling join also records the participant's attendance,
 * which feeds the automated no-show detection (FR-217).
 */
export interface MeetingJoinResult {
  bookingId: string;
  meetingUrl: string | null;
  roomId: string;
  accessToken: string;
  acsUserId: string;
  tokenExpiresAt: string;
  joinedAt: string;
}

/** A session recording entry — mirrors the server SessionRecordingDto. */
export interface SessionRecording {
  id: string;
  bookingId: string;
  recordedAt: string;
  sizeBytes: number;
  contentType: string;
}

export const meetingsApi = {
  /** Records the join and returns the ACS credentials for the session room. */
  async join(bookingId: string): Promise<MeetingJoinResult> {
    const { data } = await apiClient.post<MeetingJoinResult>(
      `/api/bookings/${bookingId}/meeting/join`,
    );
    return data;
  },

  /** Starts recording the session — idempotent, safe to call from either participant. */
  async startRecording(bookingId: string, serverCallId: string): Promise<void> {
    await apiClient.post(`/api/bookings/${bookingId}/meeting/start-recording`, {
      serverCallId,
    });
  },

  /** The booking's session recordings — visible to its student, consultant, and admins. */
  async listRecordings(bookingId: string): Promise<SessionRecording[]> {
    const { data } = await apiClient.get<SessionRecording[]>(
      `/api/bookings/${bookingId}/meeting/recordings`,
    );
    return data;
  },

  /** Downloads a session recording's bytes as a Blob. */
  async downloadRecording(recordingId: string): Promise<Blob> {
    const { data } = await apiClient.get<Blob>(
      `/api/bookings/meeting/recordings/${recordingId}/download`,
      { responseType: "blob" },
    );
    return data;
  },
};
