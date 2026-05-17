import { apiClient } from "@/services/api/client";

// ─── enums (the API serialises enums as strings on the wire) ─────────────────

export type DocumentCategory =
  | "Other"
  | "Transcript"
  | "Certificate"
  | "RecommendationLetter"
  | "PersonalStatement"
  | "Resume"
  | "IdentityDocument"
  | "ProofOfEnglish"
  | "FinancialDocument"
  | "Portfolio";

export const documentCategories: DocumentCategory[] = [
  "Transcript",
  "Certificate",
  "RecommendationLetter",
  "PersonalStatement",
  "Resume",
  "IdentityDocument",
  "ProofOfEnglish",
  "FinancialDocument",
  "Portfolio",
  "Other",
];

// ─── DTOs ────────────────────────────────────────────────────────────────────

/** A vault document — mirrors the server DocumentDto. */
export interface DocumentItem {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  category: DocumentCategory;
  uploadedAt: string;
  applicationTrackerId: string | null;
}

export interface UploadDocumentParams {
  file: File;
  category: DocumentCategory;
  applicationTrackerId?: string;
}

// ─── API ─────────────────────────────────────────────────────────────────────

export const documentsApi = {
  /** The caller's own vault documents, optionally filtered by category. */
  async list(category?: DocumentCategory): Promise<DocumentItem[]> {
    const { data } = await apiClient.get<DocumentItem[]>("/api/documents", {
      params: category ? { category } : undefined,
    });
    return data;
  },

  /** Uploads a file to the caller's vault. */
  async upload(params: UploadDocumentParams): Promise<DocumentItem> {
    const form = new FormData();
    form.append("file", params.file);
    form.append("category", params.category);
    if (params.applicationTrackerId)
      form.append("applicationTrackerId", params.applicationTrackerId);
    const { data } = await apiClient.post<DocumentItem>("/api/documents", form);
    return data;
  },

  /** Downloads a document's bytes as a Blob. */
  async download(id: string): Promise<Blob> {
    const { data } = await apiClient.get<Blob>(`/api/documents/${id}/download`, {
      responseType: "blob",
    });
    return data;
  },

  /** Deletes a document from the vault. */
  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/documents/${id}`);
  },
};
