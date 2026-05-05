import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Switch,
  FormControlLabel,
  Button,
  Badge,
  IconButton,
  Drawer,
  Typography,
  Divider,
  Autocomplete,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { FilterList as FilterListIcon, Close as CloseIcon } from '@mui/icons-material';
import { DegreeLevel, ScholarshipFundingType } from '@/types';
import type { ScholarshipSearchFilters } from '@/types';

interface ScholarshipFiltersProps {
  filters: ScholarshipSearchFilters;
  onFilterChange: (filters: ScholarshipSearchFilters) => void;
}

export function ScholarshipFilters({ filters, onFilterChange }: ScholarshipFiltersProps) {
  const { t } = useTranslation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [searchValue, setSearchValue] = useState(filters.search || '');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Sync search value when filters change externally (e.g., URL reset)
  useEffect(() => {
    setSearchValue(filters.search || '');
  }, [filters.search]);

  const handleSearchChange = useCallback(
    (value: string) => {
      setSearchValue(value);
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
      debounceRef.current = setTimeout(() => {
        onFilterChange({ ...filters, search: value || undefined, page: 1 });
      }, 300);
    },
    [filters, onFilterChange]
  );

  // Cleanup debounce on unmount
  useEffect(() => {
    return () => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
    };
  }, []);

  const updateFilter = <K extends keyof ScholarshipSearchFilters>(
    key: K,
    value: ScholarshipSearchFilters[K]
  ) => {
    onFilterChange({ ...filters, [key]: value, page: 1 });
  };

  const clearFilters = () => {
    setSearchValue('');
    onFilterChange({});
  };

  // Count active filters (excluding page/pageSize/sortBy)
  const activeFilterCount = [
    filters.search,
    filters.country,
    filters.degreeLevel !== undefined ? String(filters.degreeLevel) : undefined,
    filters.fieldOfStudy,
    filters.fundingType !== undefined ? String(filters.fundingType) : undefined,
    filters.deadlineFrom,
    filters.deadlineTo,
    filters.includeExpired ? 'true' : undefined,
  ].filter(Boolean).length;

  const filterContent = (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 2,
        p: isMobile ? 2 : 0,
      }}
    >
      {isMobile && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Typography variant="h6">{t('scholarshipPage.filters')}</Typography>
          <IconButton onClick={() => setDrawerOpen(false)}>
            <CloseIcon />
          </IconButton>
        </Box>
      )}

      <TextField
        label={t('scholarshipPage.search')}
        value={searchValue}
        onChange={(e) => handleSearchChange(e.target.value)}
        size="small"
        fullWidth
      />

      <FormControl size="small" fullWidth>
        <InputLabel>{t('scholarshipPage.degreeLevel')}</InputLabel>
        <Select
          value={filters.degreeLevel !== undefined ? filters.degreeLevel : ''}
          label={t('scholarshipPage.degreeLevel')}
          onChange={(e) => {
            const val = e.target.value;
            updateFilter('degreeLevel', val === '' ? undefined : (val as DegreeLevel));
          }}
        >
          <MenuItem value="">
            <em>-</em>
          </MenuItem>
          <MenuItem value={DegreeLevel.Bachelors}>{t('scholarshipPage.bachelors')}</MenuItem>
          <MenuItem value={DegreeLevel.Masters}>{t('scholarshipPage.masters')}</MenuItem>
          <MenuItem value={DegreeLevel.PhD}>{t('scholarshipPage.phd')}</MenuItem>
          <MenuItem value={DegreeLevel.Diploma}>{t('scholarshipPage.diploma')}</MenuItem>
          <MenuItem value={DegreeLevel.Other}>{t('scholarshipPage.other')}</MenuItem>
        </Select>
      </FormControl>

      <Autocomplete
        freeSolo
        options={[]}
        value={filters.country || ''}
        onInputChange={(_e, value) => {
          updateFilter('country', value || undefined);
        }}
        renderInput={(params) => (
          <TextField {...params} label={t('scholarshipPage.country')} size="small" />
        )}
      />

      <TextField
        label={t('scholarshipPage.fieldOfStudy')}
        value={filters.fieldOfStudy || ''}
        onChange={(e) => updateFilter('fieldOfStudy', e.target.value || undefined)}
        size="small"
        fullWidth
      />

      <FormControl size="small" fullWidth>
        <InputLabel>{t('scholarshipPage.fundingType')}</InputLabel>
        <Select
          value={filters.fundingType !== undefined ? filters.fundingType : ''}
          label={t('scholarshipPage.fundingType')}
          onChange={(e) => {
            const val = e.target.value;
            updateFilter('fundingType', val === '' ? undefined : (val as ScholarshipFundingType));
          }}
        >
          <MenuItem value="">
            <em>-</em>
          </MenuItem>
          <MenuItem value={ScholarshipFundingType.FullyFunded}>
            {t('scholarshipPage.fullyFunded')}
          </MenuItem>
          <MenuItem value={ScholarshipFundingType.PartiallyFunded}>
            {t('scholarshipPage.partiallyFunded')}
          </MenuItem>
          <MenuItem value={ScholarshipFundingType.SelfFunded}>
            {t('scholarshipPage.selfFunded')}
          </MenuItem>
          <MenuItem value={ScholarshipFundingType.Other}>{t('scholarshipPage.other')}</MenuItem>
        </Select>
      </FormControl>

      <Divider />

      <Typography variant="subtitle2" color="text.secondary">
        {t('scholarshipPage.deadlineRange')}
      </Typography>

      <TextField
        label={t('scholarshipPage.from')}
        type="date"
        value={filters.deadlineFrom || ''}
        onChange={(e) => updateFilter('deadlineFrom', e.target.value || undefined)}
        size="small"
        fullWidth
        slotProps={{ inputLabel: { shrink: true } }}
      />

      <TextField
        label={t('scholarshipPage.to')}
        type="date"
        value={filters.deadlineTo || ''}
        onChange={(e) => updateFilter('deadlineTo', e.target.value || undefined)}
        size="small"
        fullWidth
        slotProps={{ inputLabel: { shrink: true } }}
      />

      <FormControlLabel
        control={
          <Switch
            checked={filters.includeExpired || false}
            onChange={(e) => updateFilter('includeExpired', e.target.checked)}
          />
        }
        label={t('scholarshipPage.includeExpired')}
      />

      <Button
        variant="outlined"
        onClick={clearFilters}
        disabled={activeFilterCount === 0}
        fullWidth
      >
        {t('scholarshipPage.clearAll')}
      </Button>
    </Box>
  );

  // Mobile: drawer with filter icon button
  if (isMobile) {
    return (
      <>
        <Badge badgeContent={activeFilterCount} color="primary" sx={{ mr: 1 }}>
          <IconButton onClick={() => setDrawerOpen(true)} color="primary">
            <FilterListIcon />
          </IconButton>
        </Badge>

        <Drawer
          anchor="left"
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          PaperProps={{ sx: { width: 300 } }}
        >
          {filterContent}
        </Drawer>
      </>
    );
  }

  // Desktop: sticky sidebar
  return (
    <Box
      sx={{
        width: 280,
        minWidth: 280,
        position: 'sticky',
        top: 80,
        alignSelf: 'flex-start',
      }}
    >
      <Typography variant="h6" sx={{ mb: 2 }}>
        {t('scholarshipPage.filters')}
      </Typography>
      {filterContent}
    </Box>
  );
}
