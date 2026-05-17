import { apiClient } from "@/services/api/client";
import { useAuthStore, type CurrentUser } from "@/stores/authStore";

/** Token pair + user — mirrors AuthTokensDto (PB-001). */
export interface AuthTokensResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: CurrentUser;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export type SsoProvider = "google" | "microsoft";

const apiBase = (): string => import.meta.env.VITE_API_BASE_URL ?? "";
const ssoRedirectUri = (): string => `${window.location.origin}/auth/sso-callback`;
const SSO_PROVIDER_KEY = "sp_sso_provider";

export const authApi = {
  async register(req: RegisterRequest): Promise<AuthTokensResponse> {
    const { data } = await apiClient.post<AuthTokensResponse>("/api/auth/register", req);
    return data;
  },
  async login(req: LoginRequest): Promise<AuthTokensResponse> {
    const { data } = await apiClient.post<AuthTokensResponse>("/api/auth/login", req);
    return data;
  },
  async forgotPassword(email: string): Promise<void> {
    await apiClient.post("/api/auth/forgot-password", { email });
  },
  async resetPassword(token: string, newPassword: string): Promise<void> {
    await apiClient.post("/api/auth/reset-password", { token, newPassword });
  },
  async logout(refreshToken: string): Promise<void> {
    await apiClient.post("/api/auth/logout", { refreshToken });
  },
  /** One-time first-role selection for a newly-registered (Unassigned) account. */
  async selectRole(role: "Student" | "Company" | "Consultant"): Promise<AuthTokensResponse> {
    const { data } = await apiClient.post<AuthTokensResponse>("/api/auth/select-role", { role });
    return data;
  },
  /** Redirects the browser to the provider's consent screen. */
  beginSso(provider: SsoProvider): void {
    sessionStorage.setItem(SSO_PROVIDER_KEY, provider);
    const redirectUri = encodeURIComponent(ssoRedirectUri());
    window.location.href = `${apiBase()}/api/auth/${provider}/authorize?redirectUri=${redirectUri}`;
  },
  /** Reads the provider stashed before the redirect. */
  pendingSsoProvider(): SsoProvider | null {
    const value = sessionStorage.getItem(SSO_PROVIDER_KEY);
    return value === "google" || value === "microsoft" ? value : null;
  },
  async completeSso(provider: SsoProvider, code: string): Promise<AuthTokensResponse> {
    sessionStorage.removeItem(SSO_PROVIDER_KEY);
    const { data } = await apiClient.get<AuthTokensResponse>(`/api/auth/${provider}/callback`, {
      params: { code, redirectUri: ssoRedirectUri() },
    });
    return data;
  },
};

/** Persists a successful auth response into the global auth store. */
export function applyAuthSession(res: AuthTokensResponse): CurrentUser {
  useAuthStore.getState().setSession({
    user: res.user,
    tokens: {
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      accessTokenExpiresAt: res.accessTokenExpiresAt,
      refreshTokenExpiresAt: res.refreshTokenExpiresAt,
    },
  });
  return res.user;
}

/** Where to send the user after a successful sign-in. */
export function postAuthPath(user: CurrentUser): string {
  if (!user.isOnboardingComplete) return "/onboarding";
  switch (user.activeRole) {
    case "Student":
      return "/student";
    case "Company":
      return "/company";
    case "Consultant":
      return "/consultant";
    case "Admin":
    case "SuperAdmin":
      return "/admin";
    default:
      return "/onboarding";
  }
}
