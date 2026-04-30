import { apiClient } from "@/services/api/client";

export type AiFeature = "Recommendation" | "Eligibility" | "Chatbot";
export type EligibilityMatch = "yes" | "partial" | "no" | "unknown";

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
  name: string;
  studentValue: string;
  listingRequirement: string;
  match: EligibilityMatch;
}

export interface EligibilityDto {
  scholarshipId: string;
  criteria: EligibilityCriterion[];
  summary: string;
  disclaimer: string;
  generatedAt: string;
}

export interface ChatAnswerDto {
  sessionId: string;
  message: string;
  disclaimer: string;
  promptTokens: number;
  completionTokens: number;
  estimatedCostUsd: number;
  answeredAt: string;
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

export const aiApi = {
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
};
