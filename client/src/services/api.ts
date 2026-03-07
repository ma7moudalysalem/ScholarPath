import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { AuthResponse, ApiError } from '@/types';
import { useAuthStore } from '@/stores/authStore';

const API_URL = import.meta.env.VITE_API_URL || '/api/v1';

const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else if (token) {
      promise.resolve(token);
    }
  });
  failedQueue = [];
}

function clearAuth() {
  useAuthStore.getState().logout();
  sessionStorage.setItem('sessionExpired', 'true');
  window.location.href = '/';
}

// Request interceptor: attach Bearer token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const { accessToken } = useAuthStore.getState();
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: handle 401 with token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    const originalRequest = error.config;

    if (!originalRequest || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    // Avoid infinite loop on refresh endpoint
    if (originalRequest.url?.includes('/auth/refresh')) {
      clearAuth();
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      }).then((token) => {
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return api(originalRequest);
      });
    }

    isRefreshing = true;

    try {
      const { refreshToken } = useAuthStore.getState();
      if (!refreshToken) {
        clearAuth();
        return Promise.reject(error);
      }

      const { data } = await axios.post<AuthResponse>(`${API_URL}/auth/refresh`, {
        refreshToken,
      });

      useAuthStore.getState().updateTokens(data.accessToken, data.refreshToken);
      processQueue(null, data.accessToken);

      originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError, null);
      clearAuth();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

export default api;
