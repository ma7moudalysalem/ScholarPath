import { apiClient } from "@/services/api/client";

export type AiFeature = "Recommendation" | "Eligibility" | "Chatbot";
export type EligibilityMatch = "yes" | "partial" | "no" | "unknown";

/**
 * SRS FR-117 — the overall eligibility classification. Derived server-side
 * from the per-criterion verdicts and serialised as the enum name
 * (`JsonStringEnumConverter`).
 */
export type EligibilityVerdict = "Eligible" | "PartiallyEligible" | "NotEligible";

export interface RecommendationItem {
  scholarshipId: string;
  titleEn: string;
  titleAr: string;
  matchScore: number;
  explanationEn: string;
  explanationAr: string;
}

export interface RecommendationsDto {
  items: RecommendationItem[];
  disclaimer: string;
  generatedAt: string;
}

export interface EligibilityCriterion {
  nameEn: string;
  nameAr: string;
  studentValue: string;
  listingRequirement: string;
  match: EligibilityMatch;
}

export interface EligibilityDto {
  scholarshipId: string;
  criteria: EligibilityCriterion[];
  summaryEn: string;
  summaryAr: string;
  /** SRS FR-117 — overall verdict derived server-side from `criteria`. */
  verdict: EligibilityVerdict;
  disclaimer: string;
  generatedAt: string;
}

/** A knowledge-base document the RAG retriever used to ground a chat answer. */
export interface ChatSourceDto {
  title: string;
  sourceType: "Scholarship" | "Faq" | string;
  scholarshipId: string | null;
  /** Cosine similarity in [-1, 1]; higher is more relevant. */
  score: number;
}

export interface ChatAnswerDto {
  sessionId: string;
  message: string;
  disclaimer: string;
  promptTokens: number;
  completionTokens: number;
  estimatedCostUsd: number;
  answeredAt: string;
  /** SRS PB-016 — the RAG citations behind this answer. */
  sources: ChatSourceDto[];
}

export interface AiInteractionRow {
  id: string;
  feature: AiFeature;
  modelName: string | null;
  startedAt: string;
  completedAt: string | null;
  promptTokens: number;
  completionTokens: number;
  costUsd: number;
  succeeded: boolean;
}

export type RecommendationClickSource = "card" | "list" | "modal";

export interface LogRecommendationClickResult {
  eventId: string;
  deduplicated: boolean;
}

export const aiApi = {
  /**
   * GET the user's cached recommendations (last 24h). Resolves to null when
   * the server returns 204 — caller should then call `regenerate` to get fresh.
   */
  async cachedRecommendations(maxAgeHours = 24): Promise<RecommendationsDto | null> {
    const res = await apiClient.get<RecommendationsDto>("/api/ai/recommendations", {
      params: { maxAgeHours },
      // 204 with empty body is a valid signal, not an error
      validateStatus: (s) => s === 200 || s === 204,
    });
    return res.status === 204 ? null : res.data;
  },

  /** Regenerate recommendations (counts against daily cost budget). */
  async recommendations(topN?: number): Promise<RecommendationsDto> {
    const { data } = await apiClient.post<RecommendationsDto>(
      "/api/ai/recommendations",
      null,
      { params: { topN } },
    );
    return data;
  },
  async eligibility(scholarshipId: string): Promise<EligibilityDto> {
    const { data } = await apiClient.post<EligibilityDto>(
      `/api/ai/eligibility/${scholarshipId}`,
    );
    return data;
  },
  async chat(message: string, sessionId?: string): Promise<ChatAnswerDto> {
    const { data } = await apiClient.post<ChatAnswerDto>(
      "/api/ai/chat",
      { message, sessionId: sessionId ?? null },
    );
    return data;
  },
  async interactions(limit = 20): Promise<AiInteractionRow[]> {
    const { data } = await apiClient.get<AiInteractionRow[]>(
      "/api/ai/interactions",
      { params: { limit } },
    );
    return data;
  },
  /**
   * Fires a recommendation click event (PB-017 FR-249). The server debounces
   * same-card repeat taps within 500ms, so callers don't need to debounce
   * themselves. Errors are swallowed — a failed analytics ping shouldn't
   * block the user from opening the scholarship.
   */
  async logRecommendationClick(
    scholarshipId: string,
    aiInteractionId: string | null,
    source: RecommendationClickSource = "card",
  ): Promise<LogRecommendationClickResult | null> {
    try {
      const { data } = await apiClient.post<LogRecommendationClickResult>(
        "/api/ai/recommendations/click",
        { scholarshipId, aiInteractionId, source },
      );
      return data;
    } catch {
      return null;
    }
  },
};
