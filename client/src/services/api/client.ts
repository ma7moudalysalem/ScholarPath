import axios from "axios";
import type {
  AxiosError,
  AxiosInstance,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
import { useAuthStore } from "@/stores/authStore";

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
  const lang = localStorage.getItem("scholarpath_lang");
  if (lang) {
    config.headers = config.headers ?? {};
    (config.headers as Record<string, string>)["Accept-Language"] = lang;
  }
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
