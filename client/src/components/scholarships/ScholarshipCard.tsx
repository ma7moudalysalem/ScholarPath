import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import {
  Card,
  CardContent,
  CardActions,
  Typography,
  Chip,
  IconButton,
  Button,
  Box,
  Tooltip,
  Snackbar,
  Alert,
  Skeleton,
} from '@mui/material';
import {
  Bookmark,
  BookmarkBorder,
  AccessTime,
  LocationOn,
  School,
} from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuthModal } from '@/hooks/useAuthModal';
import { scholarshipService } from '@/services/scholarshipService';
import { ScholarshipFundingType, DegreeLevel } from '@/types';
import type { ScholarshipListItemDto } from '@/types';

interface ScholarshipCardProps {
  scholarship: ScholarshipListItemDto;
  matchReasons?: string[];
  onBookmarkChange?: (id: string, saved: boolean) => void;
}

function getFundingTypeKey(type: ScholarshipFundingType): string {
  switch (type) {
    case ScholarshipFundingType.FullyFunded:
      return 'scholarshipPage.fullyFunded';
    case ScholarshipFundingType.PartiallyFunded:
      return 'scholarshipPage.partiallyFunded';
    case ScholarshipFundingType.SelfFunded:
      return 'scholarshipPage.selfFunded';
    default:
      return 'scholarshipPage.other';
  }
}

function getDegreeLevelKey(level: DegreeLevel): string {
  switch (level) {
    case DegreeLevel.Bachelors:
      return 'scholarshipPage.bachelors';
    case DegreeLevel.Masters:
      return 'scholarshipPage.masters';
    case DegreeLevel.PhD:
      return 'scholarshipPage.phd';
    case DegreeLevel.Diploma:
      return 'scholarshipPage.diploma';
    default:
      return 'scholarshipPage.other';
  }
}

function getFundingColor(
  type: ScholarshipFundingType
): 'success' | 'warning' | 'info' | 'default' {
  switch (type) {
    case ScholarshipFundingType.FullyFunded:
      return 'success';
    case ScholarshipFundingType.PartiallyFunded:
      return 'warning';
    case ScholarshipFundingType.SelfFunded:
      return 'info';
    default:
      return 'default';
  }
}

export function ScholarshipCard({
  scholarship,
  matchReasons,
  onBookmarkChange,
}: ScholarshipCardProps) {
  const { t } = useTranslation();
  const language = useUiStore((s) => s.language);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useAuthModal((s) => s.open);

  const [isSaved, setIsSaved] = useState(scholarship.isSaved);
  const [saving, setSaving] = useState(false);
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'info';
  }>({ open: false, message: '', severity: 'success' });

  const title =
    language === 'ar' && scholarship.titleAr
      ? scholarship.titleAr
      : scholarship.title;
  const providerName =
    language === 'ar' && scholarship.providerNameAr
      ? scholarship.providerNameAr
      : scholarship.providerName;

  const handleBookmarkClick = async () => {
    if (!isAuthenticated) {
      openAuthModal('login');
      setSnackbar({
        open: true,
        message: t('scholarshipPage.loginToSave'),
        severity: 'info',
      });
      return;
    }

    // Optimistic update
    const previousState = isSaved;
    setIsSaved(!isSaved);
    setSaving(true);

    try {
      if (previousState) {
        await scholarshipService.unsaveScholarship(scholarship.id);
        setSnackbar({
          open: true,
          message: t('scholarshipPage.removedFromSaved'),
          severity: 'success',
        });
      } else {
        await scholarshipService.saveScholarship(scholarship.id);
        setSnackbar({
          open: true,
          message: t('scholarshipPage.savedToList'),
          severity: 'success',
        });
      }
      onBookmarkChange?.(scholarship.id, !previousState);
    } catch {
      // Revert on error
      setIsSaved(previousState);
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Card
        sx={{
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          transition: 'box-shadow 0.2s, transform 0.2s',
          '&:hover': {
            boxShadow: 6,
            transform: 'translateY(-2px)',
          },
        }}
      >
        <CardContent sx={{ flex: 1, pb: 1 }}>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'flex-start',
              mb: 1,
            }}
          >
            <Typography
              variant="h6"
              component="h3"
              sx={{
                fontWeight: 600,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
                flex: 1,
                mr: 1,
              }}
            >
              {title}
            </Typography>
            <Tooltip
              title={
                isSaved
                  ? t('scholarshipPage.removedFromSaved')
                  : t('scholarshipPage.savedToList')
              }
            >
              <IconButton
                onClick={handleBookmarkClick}
                disabled={saving}
                color={isSaved ? 'primary' : 'default'}
                size="small"
              >
                {isSaved ? <Bookmark /> : <BookmarkBorder />}
              </IconButton>
            </Tooltip>
          </Box>

          {providerName && (
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 0.5 }}>
              <School sx={{ fontSize: 16, mr: 0.5, color: 'text.secondary' }} />
              <Typography variant="body2" color="text.secondary">
                {providerName}
              </Typography>
            </Box>
          )}

          {scholarship.country && (
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 0.5 }}>
              <LocationOn
                sx={{ fontSize: 16, mr: 0.5, color: 'text.secondary' }}
              />
              <Typography variant="body2" color="text.secondary">
                {scholarship.country}
              </Typography>
            </Box>
          )}

          {scholarship.deadline && (
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
              <AccessTime
                sx={{ fontSize: 16, mr: 0.5, color: 'text.secondary' }}
              />
              <Typography variant="body2" color="text.secondary">
                {t('scholarshipPage.deadline')}:{' '}
                {new Date(scholarship.deadline).toLocaleDateString()}
              </Typography>
              {scholarship.deadlineCountdownDays != null &&
                scholarship.deadlineCountdownDays > 0 && (
                  <Chip
                    label={t('scholarshipPage.daysLeft', {
                      count: scholarship.deadlineCountdownDays,
                    })}
                    size="small"
                    color={scholarship.isExpiringSoon ? 'warning' : 'default'}
                    sx={{ ml: 1, height: 20, fontSize: '0.7rem' }}
                  />
                )}
            </Box>
          )}

          {scholarship.awardAmount != null && (
            <Typography
              variant="body2"
              color="primary"
              sx={{ fontWeight: 600, mb: 1 }}
            >
              {scholarship.currency || 'USD'}{' '}
              {scholarship.awardAmount.toLocaleString()}
            </Typography>
          )}

          <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 1 }}>
            <Chip
              label={t(getFundingTypeKey(scholarship.fundingType))}
              size="small"
              color={getFundingColor(scholarship.fundingType)}
              variant="outlined"
            />
            <Chip
              label={t(getDegreeLevelKey(scholarship.degreeLevel))}
              size="small"
              variant="outlined"
            />
            {scholarship.isExpiringSoon && (
              <Chip
                label={t('scholarshipPage.expiringSoon')}
                size="small"
                color="warning"
              />
            )}
          </Box>

          {matchReasons && matchReasons.length > 0 && (
            <Tooltip
              title={
                <Box component="ul" sx={{ m: 0, pl: 2 }}>
                  {matchReasons.map((reason, idx) => (
                    <li key={idx}>{reason}</li>
                  ))}
                </Box>
              }
            >
              <Chip
                label={t('scholarshipPage.whyRecommended')}
                size="small"
                color="info"
                sx={{ mt: 1, cursor: 'pointer' }}
              />
            </Tooltip>
          )}
        </CardContent>

        <CardActions sx={{ pt: 0, px: 2, pb: 2 }}>
          <Button
            component={RouterLink}
            to={`/scholarships/${scholarship.id}`}
            size="small"
            variant="outlined"
            fullWidth
          >
            {t('scholarshipPage.viewDetails')}
          </Button>
        </CardActions>
      </Card>

      <Snackbar
        open={snackbar.open}
        autoHideDuration={3000}
        onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={snackbar.severity}
          onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
          variant="filled"
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </>
  );
}

export function ScholarshipCardSkeleton() {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            mb: 1,
          }}
        >
          <Skeleton variant="text" width="70%" height={32} />
          <Skeleton variant="circular" width={32} height={32} />
        </Box>
        <Skeleton variant="text" width="50%" height={20} />
        <Skeleton variant="text" width="40%" height={20} />
        <Skeleton variant="text" width="60%" height={20} />
        <Box sx={{ display: 'flex', gap: 0.5, mt: 1 }}>
          <Skeleton variant="rounded" width={80} height={24} />
          <Skeleton variant="rounded" width={60} height={24} />
        </Box>
      </CardContent>
      <CardActions sx={{ px: 2, pb: 2 }}>
        <Skeleton variant="rounded" width="100%" height={32} />
      </CardActions>
    </Card>
  );
}

export type { ScholarshipCardProps };
