import { apiClient } from "@/services/api/client";

export interface CreateIntentRequest {
  amountCents: number;
  currency: string;
  captureMethod: "manual" | "automatic";
  bookingId?: string;
  applicationId?: string;
}

export interface CreateIntentResponse {
  paymentIntentId: string;
  clientSecret: string;
  status: string;
}

export const paymentsApi = {
  async createIntent(req: CreateIntentRequest): Promise<CreateIntentResponse> {
    const { data } = await apiClient.post<CreateIntentResponse>("/api/payments/intent", req);
    return data;
  },
};
