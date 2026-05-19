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

export const meetingsApi = {
  /** Records the join and returns the ACS credentials for the session room. */
  async join(bookingId: string): Promise<MeetingJoinResult> {
    const { data } = await apiClient.post<MeetingJoinResult>(
      `/api/bookings/${bookingId}/meeting/join`,
    );
    return data;
  },
};
