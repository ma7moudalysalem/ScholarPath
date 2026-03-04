import api from './api';
import type { ScholarshipDto, ScholarshipFilters, PaginatedResponse } from '@/types';

export const scholarshipService = {
  async getScholarships(
    filters: ScholarshipFilters = {}
  ): Promise<PaginatedResponse<ScholarshipDto>> {
    const params = new URLSearchParams();

    if (filters.search) params.append('search', filters.search);
    if (filters.country) params.append('country', filters.country);
    if (filters.fieldOfStudy) params.append('fieldOfStudy', filters.fieldOfStudy);
    if (filters.fundingType !== undefined)
      params.append('fundingType', String(filters.fundingType));
    if (filters.degreeLevel !== undefined)
      params.append('degreeLevel', String(filters.degreeLevel));
    if (filters.page !== undefined) params.append('page', String(filters.page));
    if (filters.pageSize !== undefined) params.append('pageSize', String(filters.pageSize));

    const response = await api.get<PaginatedResponse<ScholarshipDto>>(
      `/scholarships?${params.toString()}`
    );
    return response.data;
  },

  async getScholarshipById(id: string): Promise<ScholarshipDto> {
    const response = await api.get<ScholarshipDto>(`/scholarships/${id}`);
    return response.data;
  },

  async saveScholarship(scholarshipId: string): Promise<void> {
    await api.post(`/scholarships/${scholarshipId}/save`);
  },

  async unsaveScholarship(scholarshipId: string): Promise<void> {
    await api.delete(`/scholarships/${scholarshipId}/save`);
  },
};
