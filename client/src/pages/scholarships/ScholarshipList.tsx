import { useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import {
  Box,
  Typography,
  Pagination,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  Button,
  Alert,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import Grid from '@mui/material/Grid2';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/authStore';
import { scholarshipService } from '@/services/scholarshipService';
import {
  ScholarshipCard,
  ScholarshipCardSkeleton,
} from '@/components/scholarships/ScholarshipCard';
import { ScholarshipFilters } from '@/components/scholarships/ScholarshipFilters';
import { RecommendedCarousel } from '@/components/scholarships/RecommendedCarousel';
import {
  ScholarshipSortBy,
  DegreeLevel,
  ScholarshipFundingType,
} from '@/types';
import type { ScholarshipSearchFilters } from '@/types';

function parseFiltersFromParams(
  params: URLSearchParams
): ScholarshipSearchFilters {
  const filters: ScholarshipSearchFilters = {};

  const search = params.get('search');
  if (search) filters.search = search;

  const country = params.get('country');
  if (country) filters.country = country;

  const degreeLevel = params.get('degreeLevel');
  if (degreeLevel !== null && degreeLevel !== '') {
    filters.degreeLevel = Number(degreeLevel) as DegreeLevel;
  }

  const fieldOfStudy = params.get('fieldOfStudy');
  if (fieldOfStudy) filters.fieldOfStudy = fieldOfStudy;

  const fundingType = params.get('fundingType');
  if (fundingType !== null && fundingType !== '') {
    filters.fundingType = Number(fundingType) as ScholarshipFundingType;
  }

  const deadlineFrom = params.get('deadlineFrom');
  if (deadlineFrom) filters.deadlineFrom = deadlineFrom;

  const deadlineTo = params.get('deadlineTo');
  if (deadlineTo) filters.deadlineTo = deadlineTo;

  const page = params.get('page');
  if (page) filters.page = Number(page);

  const sortBy = params.get('sortBy');
  if (sortBy !== null && sortBy !== '') {
    filters.sortBy = Number(sortBy) as ScholarshipSortBy;
  }

  const includeExpired = params.get('includeExpired');
  if (includeExpired === 'true') filters.includeExpired = true;

  return filters;
}

function filtersToParams(filters: ScholarshipSearchFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.search) params.set('search', filters.search);
  if (filters.country) params.set('country', filters.country);
  if (filters.degreeLevel !== undefined)
    params.set('degreeLevel', String(filters.degreeLevel));
  if (filters.fieldOfStudy) params.set('fieldOfStudy', filters.fieldOfStudy);
  if (filters.fundingType !== undefined)
    params.set('fundingType', String(filters.fundingType));
  if (filters.deadlineFrom) params.set('deadlineFrom', filters.deadlineFrom);
  if (filters.deadlineTo) params.set('deadlineTo', filters.deadlineTo);
  if (filters.page && filters.page > 1)
    params.set('page', String(filters.page));
  if (filters.sortBy !== undefined)
    params.set('sortBy', String(filters.sortBy));
  if (filters.includeExpired) params.set('includeExpired', 'true');
  return params;
}

const PAGE_SIZE = 12;

export default function ScholarshipList() {
  const { t } = useTranslation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo(
    () => parseFiltersFromParams(searchParams),
    [searchParams]
  );

  const savedOnly = searchParams.get('savedOnly') === 'true';

  const setFilters = useCallback(
    (newFilters: ScholarshipSearchFilters) => {
      const params = filtersToParams(newFilters);
      if (savedOnly) params.set('savedOnly', 'true');
      setSearchParams(params, { replace: true });
    },
    [savedOnly, setSearchParams]
  );

  const toggleSavedOnly = useCallback(() => {
    const params = filtersToParams(filters);
    if (!savedOnly) {
      params.set('savedOnly', 'true');
    }
    // Remove page when switching modes
    params.delete('page');
    setSearchParams(params, { replace: true });
  }, [savedOnly, filters, setSearchParams]);

  const queryFilters = useMemo(
    () => ({
      ...filters,
      page: filters.page || 1,
      pageSize: PAGE_SIZE,
    }),
    [filters]
  );

  // Main scholarships query
  const {
    data,
    isLoading,
    isError,
    refetch,
  } = useQuery({
    queryKey: savedOnly
      ? ['scholarships', 'saved', queryFilters.page]
      : ['scholarships', 'list', queryFilters],
    queryFn: () =>
      savedOnly
        ? scholarshipService.getSavedScholarships(
            queryFilters.page,
            queryFilters.pageSize
          )
        : scholarshipService.getScholarships(queryFilters),
    staleTime: 2 * 60 * 1000,
  });

  const handlePageChange = (_event: React.ChangeEvent<unknown>, page: number) => {
    setFilters({ ...filters, page });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handleSortChange = (sortBy: ScholarshipSortBy) => {
    setFilters({ ...filters, sortBy, page: 1 });
  };

  const resetFilters = () => {
    setSearchParams({}, { replace: true });
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ mb: 3, fontWeight: 700 }}>
        {t('scholarshipPage.title')}
      </Typography>

      {/* Recommended carousel (only for authenticated users) */}
      {isAuthenticated && <RecommendedCarousel />}

      {/* Main layout */}
      <Box
        sx={{
          display: 'flex',
          gap: 3,
          flexDirection: isMobile ? 'column' : 'row',
        }}
      >
        {/* Filters */}
        <ScholarshipFilters filters={filters} onFilterChange={setFilters} />

        {/* Results area */}
        <Box sx={{ flex: 1, minWidth: 0 }}>
          {/* Toolbar: sort + saved only */}
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              mb: 2,
              flexWrap: 'wrap',
              gap: 1,
            }}
          >
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              {isAuthenticated && (
                <Chip
                  label={t('scholarshipPage.savedOnly')}
                  color={savedOnly ? 'primary' : 'default'}
                  variant={savedOnly ? 'filled' : 'outlined'}
                  onClick={toggleSavedOnly}
                  clickable
                />
              )}
              {data && !isLoading && (
                <Typography variant="body2" color="text.secondary">
                  {data.totalCount}{' '}
                  {t('scholarshipPage.title').toLowerCase()}
                </Typography>
              )}
            </Box>

            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>{t('scholarshipPage.sortBy')}</InputLabel>
              <Select
                value={filters.sortBy ?? ScholarshipSortBy.Relevance}
                label={t('scholarshipPage.sortBy')}
                onChange={(e) =>
                  handleSortChange(e.target.value as ScholarshipSortBy)
                }
              >
                <MenuItem value={ScholarshipSortBy.Relevance}>
                  {t('scholarshipPage.relevance')}
                </MenuItem>
                <MenuItem value={ScholarshipSortBy.DeadlineSoonest}>
                  {t('scholarshipPage.deadlineSoonest')}
                </MenuItem>
                <MenuItem value={ScholarshipSortBy.Newest}>
                  {t('scholarshipPage.newest')}
                </MenuItem>
                <MenuItem value={ScholarshipSortBy.HighestFunding}>
                  {t('scholarshipPage.highestFunding')}
                </MenuItem>
              </Select>
            </FormControl>
          </Box>

          {/* Loading state */}
          {isLoading && (
            <Grid container spacing={2}>
              {[0, 1, 2, 3, 4, 5].map((i) => (
                <Grid size={{ xs: 12, sm: 6, lg: 4 }} key={i}>
                  <ScholarshipCardSkeleton />
                </Grid>
              ))}
            </Grid>
          )}

          {/* Error state */}
          {isError && !isLoading && (
            <Alert
              severity="error"
              sx={{ mb: 2 }}
              action={
                <Button
                  color="inherit"
                  size="small"
                  onClick={() => void refetch()}
                >
                  {t('retry')}
                </Button>
              }
            >
              {t('error')}
            </Alert>
          )}

          {/* Empty state */}
          {!isLoading && !isError && data && data.items.length === 0 && (
            <Box
              sx={{
                textAlign: 'center',
                py: 8,
              }}
            >
              <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
                {t('scholarshipPage.noResults')}
              </Typography>
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{ mb: 3 }}
              >
                {t('scholarshipPage.noResultsHint')}
              </Typography>
              <Button variant="outlined" onClick={resetFilters}>
                {t('scholarshipPage.resetFilters')}
              </Button>
            </Box>
          )}

          {/* Results grid */}
          {!isLoading && !isError && data && data.items.length > 0 && (
            <>
              <Grid container spacing={2}>
                {data.items.map((scholarship) => (
                  <Grid size={{ xs: 12, sm: 6, lg: 4 }} key={scholarship.id}>
                    <ScholarshipCard scholarship={scholarship} />
                  </Grid>
                ))}
              </Grid>

              {/* Pagination */}
              {data.totalPages > 1 && (
                <Box
                  sx={{
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    mt: 4,
                    gap: 2,
                    flexDirection: 'column',
                  }}
                >
                  <Pagination
                    count={data.totalPages}
                    page={data.page}
                    onChange={handlePageChange}
                    color="primary"
                    showFirstButton
                    showLastButton
                  />
                  <Typography variant="caption" color="text.secondary">
                    {t('scholarshipPage.page', {
                      page: data.page,
                      totalPages: data.totalPages,
                    })}
                  </Typography>
                </Box>
              )}
            </>
          )}
        </Box>
      </Box>
    </Box>
  );
}
