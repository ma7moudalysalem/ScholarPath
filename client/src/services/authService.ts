import api from './api';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  OnboardingRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  ExternalLoginRequest,
  UserDto,
} from '@/types';

export const authService = {
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', data);
    return response.data;
  },

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', data);
    return response.data;
  },

  async logout(): Promise<void> {
    await api.post('/auth/logout');
  },

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/refresh', { refreshToken });
    return response.data;
  },

  async forgotPassword(data: ForgotPasswordRequest): Promise<void> {
    await api.post('/auth/forgot-password', data);
  },

  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    await api.post('/auth/reset-password', data);
  },

  async changePassword(data: ChangePasswordRequest): Promise<void> {
    await api.post('/auth/change-password', data);
  },

  async completeOnboarding(data: OnboardingRequest): Promise<UserDto> {
    const response = await api.post<UserDto>('/auth/onboarding', data);
    return response.data;
  },

  async getMe(): Promise<UserDto> {
    const response = await api.get<UserDto>('/auth/me');
    return response.data;
  },

  async externalLogin(data: ExternalLoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/external/login', data);
    return response.data;
  },
};
