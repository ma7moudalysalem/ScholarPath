import api from './api';
import { UpgradeRequestDto } from '@/types';

class UpgradeRequestService {
    async getMyUpgradeRequestStatus(): Promise<UpgradeRequestDto | null> {
        try {
            const response = await api.get<UpgradeRequestDto>('/upgrade-requests/my-status');
            return response.data;
        } catch (err) {
            const error = err as import('axios').AxiosError;
            if (error.response?.status === 404) {
                return null; // No request exists
            }
            throw error;
        }
    }
}

export const upgradeRequestService = new UpgradeRequestService();
