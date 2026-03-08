import api from './api';
import type { TrackApplicationRequest, TrackApplicationResponse } from '@/types';

export const applicationService = {
  async trackApplication(request: TrackApplicationRequest): Promise<TrackApplicationResponse> {
    const response = await api.post<TrackApplicationResponse>('/applications/track', request);
    return response.data;
  },
};
