import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Alert,
  Button,
  IconButton,
} from '@mui/material';
import {
  VisibilityOff as VisibilityOffIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/authStore';
import { scholarshipService } from '@/services/scholarshipService';
import { ScholarshipCard, ScholarshipCardSkeleton } from './ScholarshipCard';

const STORAGE_KEY = 'scholarpath-hide-recommendations';

export function RecommendedCarousel() {
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [hidden, setHidden] = useState(
    () => localStorage.getItem(STORAGE_KEY) === 'true'
  );

  const { data, isLoading } = useQuery({
    queryKey: ['scholarships', 'recommended'],
    queryFn: () => scholarshipService.getRecommended(),
    enabled: isAuthenticated && !hidden,
    staleTime: 5 * 60 * 1000,
  });

  if (!isAuthenticated) return null;

  const toggleVisibility = () => {
    const newVal = !hidden;
    setHidden(newVal);
    localStorage.setItem(STORAGE_KEY, String(newVal));
  };

  // Hidden by user
  if (hidden) {
    return (
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
        <IconButton onClick={toggleVisibility} size="small">
          <VisibilityIcon />
        </IconButton>
        <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
          {t('scholarshipPage.recommended')}
        </Typography>
      </Box>
    );
  }

  // Loading state
  if (isLoading) {
    return (
      <Box sx={{ mb: 4 }}>
        <Typography variant="h5" sx={{ mb: 2, fontWeight: 600 }}>
          {t('scholarshipPage.recommended')}
        </Typography>
        <Box
          sx={{
            display: 'flex',
            gap: 2,
            overflowX: 'auto',
            pb: 1,
            '&::-webkit-scrollbar': { height: 6 },
            '&::-webkit-scrollbar-thumb': {
              borderRadius: 3,
              bgcolor: 'action.hover',
            },
          }}
        >
          {[0, 1, 2].map((i) => (
            <Box key={i} sx={{ minWidth: 300, maxWidth: 340 }}>
              <ScholarshipCardSkeleton />
            </Box>
          ))}
        </Box>
      </Box>
    );
  }

  // No data or no items
  if (!data || data.items.length === 0) return null;

  return (
    <Box sx={{ mb: 4 }}>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 2,
        }}
      >
        <Typography variant="h5" sx={{ fontWeight: 600 }}>
          {t('scholarshipPage.recommended')}
        </Typography>
        <IconButton onClick={toggleVisibility} size="small">
          <VisibilityOffIcon />
        </IconButton>
      </Box>

      {data.profileIncomplete && (
        <Alert
          severity="info"
          sx={{ mb: 2 }}
          action={
            <Button
              component={RouterLink}
              to="/profile"
              size="small"
              color="inherit"
            >
              {t('scholarshipPage.completeProfileCta')}
            </Button>
          }
        >
          {t('scholarshipPage.completeProfile')}
        </Alert>
      )}

      <Box
        sx={{
          display: 'flex',
          gap: 2,
          overflowX: 'auto',
          pb: 1,
          '&::-webkit-scrollbar': { height: 6 },
          '&::-webkit-scrollbar-thumb': {
            borderRadius: 3,
            bgcolor: 'action.hover',
          },
        }}
      >
        {data.items.map((scholarship) => (
          <Box
            key={scholarship.id}
            sx={{ minWidth: 300, maxWidth: 340, flexShrink: 0 }}
          >
            <ScholarshipCard
              scholarship={scholarship}
              matchReasons={scholarship.matchReasons}
            />
          </Box>
        ))}
      </Box>
    </Box>
  );
}
