import api from './api';
import type { UserProfileDto, UpdateProfileRequest, UpgradeRequestDto } from '@/types';
import { type UserRole } from '@/types';

export const profileService = {
  async getProfile(): Promise<UserProfileDto> {
    const response = await api.get<UserProfileDto>('/profile');
    return response.data;
  },

  async updateProfile(data: UpdateProfileRequest): Promise<UserProfileDto> {
    const response = await api.put<UserProfileDto>('/profile', data);
    return response.data;
  },

  async uploadProfileImage(file: File): Promise<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<{ imageUrl: string }>('/profile/image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  async getUpgradeStatus(): Promise<UpgradeRequestDto | null> {
    const response = await api.get<UpgradeRequestDto | null>('/profile/upgrade-status');
    return response.data;
  },

  async submitUpgradeRequest(data: { requestedRole: UserRole; reason: string }): Promise<void> {
    await api.post('/profile/upgrade-request', data);
  },
};
