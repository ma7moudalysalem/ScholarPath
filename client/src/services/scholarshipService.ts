import api from './api';
import type {
  ScholarshipDto,
  ScholarshipSearchFilters,
  ScholarshipListItemDto,
  PaginatedResponse,
  RecommendedResponse,
} from '@/types';

export const scholarshipService = {
  async getScholarships(
    filters: ScholarshipSearchFilters = {}
  ): Promise<PaginatedResponse<ScholarshipListItemDto>> {
    const response = await api.get<PaginatedResponse<ScholarshipListItemDto>>(
      '/scholarships',
      { params: filters }
    );
    return response.data;
  },

  async getScholarshipById(id: string): Promise<ScholarshipDto> {
    const response = await api.get<ScholarshipDto>(`/scholarships/${id}`);
    return response.data;
  },

  async getRecommended(): Promise<RecommendedResponse> {
    const response = await api.get<RecommendedResponse>(
      '/scholarships/recommended'
    );
    return response.data;
  },

  async saveScholarship(scholarshipId: string): Promise<void> {
    await api.post(`/scholarships/${scholarshipId}/save`);
  },

  async unsaveScholarship(scholarshipId: string): Promise<void> {
    await api.delete(`/scholarships/${scholarshipId}/save`);
  },

  async getSavedScholarships(
    page = 1,
    pageSize = 20
  ): Promise<PaginatedResponse<ScholarshipListItemDto>> {
    const response = await api.get<PaginatedResponse<ScholarshipListItemDto>>(
      '/saved-scholarships',
      { params: { page, pageSize } }
    );
    return response.data;
  },
};
