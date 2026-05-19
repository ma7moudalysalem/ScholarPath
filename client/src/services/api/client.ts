import axios from "axios";
import type {
  AxiosError,
  AxiosInstance,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
import { useAuthStore } from "@/stores/authStore";
import i18n from "@/lib/i18n";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

export interface ApiErrorPayload {
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  status: number;
  payload: ApiErrorPayload;
  constructor(payload: ApiErrorPayload) {
    super(payload.title);
    this.status = payload.status;
    this.payload = payload;
  }
}

/**
 * The most specific human-readable message for an API error: the problem
 * `detail`, else the first field-validation message (so a 422 surfaces the
 * actual failed rule, not the generic "validation failures" title), else the
 * `title`, else the supplied fallback.
 */
export function apiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError) {
    if (err.payload.detail) return err.payload.detail;
    const firstFieldError = err.payload.errors
      ? Object.values(err.payload.errors)
          .flat()
          .find((m) => typeof m === "string" && m.length > 0)
      : undefined;
    if (firstFieldError) return firstFieldError;
    if (err.payload.title) return err.payload.title;
  }
  return fallback;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL || "/",
  withCredentials: false,
  timeout: 30_000,
  headers: { Accept: "application/json" },
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const tokens = useAuthStore.getState().tokens;
  if (tokens?.accessToken) {
    config.headers = config.headers ?? {};
    (config.headers as Record<string, string>)["Authorization"] = `Bearer ${tokens.accessToken}`;
  }
  // Read the active UI language straight from i18n so the server-localised
  // responses always match what the user sees (no localStorage drift).
  config.headers = config.headers ?? {};
  (config.headers as Record<string, string>)["Accept-Language"] = i18n.language || "ar";
  return config;
});

let refreshing: Promise<string | null> | null = null;

apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError<ApiErrorPayload>) => {
    const status = error.response?.status;
    const original = error.config;

    if (
      status === 401 &&
      original &&
      !(original as InternalAxiosRequestConfig & { _retry?: boolean })._retry
    ) {
      (original as InternalAxiosRequestConfig & { _retry?: boolean })._retry = true;
      const newToken = await (refreshing ??= refreshTokens());
      refreshing = null;
      if (newToken) {
        original.headers = original.headers ?? {};
        (original.headers as Record<string, string>)["Authorization"] = `Bearer ${newToken}`;
        return apiClient(original);
      }
      useAuthStore.getState().clear();
    }

    if (error.response?.data) {
      throw new ApiError(error.response.data);
    }
    throw new ApiError({
      title: error.message || "Unknown error",
      status: status ?? 0,
    });
  },
);

async function refreshTokens(): Promise<string | null> {
  const tokens = useAuthStore.getState().tokens;
  if (!tokens?.refreshToken) return null;
  try {
    const res = await axios.post<{
      accessToken: string;
      refreshToken: string;
      accessTokenExpiresAt: string;
      refreshTokenExpiresAt: string;
    }>(`${API_BASE_URL || ""}/api/auth/refresh`, { refreshToken: tokens.refreshToken });
    useAuthStore.getState().setTokens(res.data);
    return res.data.accessToken;
  } catch {
    return null;
  }
}
