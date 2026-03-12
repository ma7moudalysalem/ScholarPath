import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { alpha } from '@mui/material/styles';
import { Box, Typography, Chip, Button, Skeleton, useTheme } from '@mui/material';
import { AccessTime, ArrowForward } from '@mui/icons-material';
import { ApplicationStatus } from '@/types';
import type { UpcomingDeadlineDto } from '@/types';

const STATUS_LABELS: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Planned]: 'dashboard.planned',
  [ApplicationStatus.Applied]: 'dashboard.applied',
  [ApplicationStatus.Pending]: 'dashboard.pending',
  [ApplicationStatus.Accepted]: 'dashboard.accepted',
  [ApplicationStatus.Rejected]: 'dashboard.rejected',
};

function getCountdownColor(days: number): 'error' | 'warning' | 'default' {
  if (days <= 3) return 'error';
  if (days <= 7) return 'warning';
  return 'default';
}

interface DeadlinesWidgetProps {
  deadlines: UpcomingDeadlineDto[];
  isLoading: boolean;
}

export default function DeadlinesWidget({ deadlines, isLoading }: DeadlinesWidgetProps) {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const primary = theme.palette.primary.main;
  const isAr = i18n.language === 'ar';

  if (isLoading) {
    return (
      <Box
        sx={{
          p: 3,
          borderRadius: 3,
          border: `1px solid ${alpha(primary, 0.09)}`,
          bgcolor: 'background.paper',
        }}
      >
        <Skeleton width={180} height={28} sx={{ mb: 2.5 }} />
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} height={60} sx={{ mb: 1, borderRadius: 2 }} />
        ))}
      </Box>
    );
  }

  const displayedDeadlines = deadlines.slice(0, 5);
  const hasMore = deadlines.length > 5;

  return (
    <Box
      sx={{
        p: 3,
        borderRadius: 3,
        border: `1px solid ${alpha(primary, 0.09)}`,
        bgcolor: 'background.paper',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <AccessTime sx={{ fontSize: 18, color: 'text.secondary', opacity: 0.6 }} />
          <Typography variant="h6" sx={{ fontWeight: 600 }}>
            {t('dashboard.upcomingDeadlines')}
          </Typography>
        </Box>
        {hasMore && (
          <Button
            size="small"
            endIcon={<ArrowForward sx={{ fontSize: 14 }} />}
            onClick={() => navigate('/dashboard/tracker')}
            sx={{ fontSize: '0.75rem' }}
          >
            {t('dashboard.viewAll')}
          </Button>
        )}
      </Box>

      {displayedDeadlines.length === 0 ? (
        <Box sx={{ py: 4, textAlign: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {t('dashboard.noDeadlines')}
          </Typography>
        </Box>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          {displayedDeadlines.map((deadline) => (
            <Box
              key={deadline.scholarshipId}
              onClick={() => navigate(`/scholarships/${deadline.scholarshipId}`)}
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                p: 2,
                borderRadius: 2.5,
                cursor: 'pointer',
                border: `1px solid ${isDark ? 'rgba(255,255,255,0.04)' : 'rgba(0,0,0,0.05)'}`,
                transition: 'all 0.2s ease',
                '&:hover': {
                  bgcolor: alpha(primary, 0.06),
                  borderColor: alpha(primary, 0.2),
                  transform: 'translateX(2px)',
                },
              }}
            >
              <Box sx={{ minWidth: 0, flex: 1, mr: 2 }}>
                <Typography
                  variant="body2"
                  sx={{
                    fontWeight: 500,
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {isAr && deadline.titleAr ? deadline.titleAr : deadline.title}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {deadline.providerName}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexShrink: 0 }}>
                <Chip
                  label={t('dashboard.daysLeft', { count: deadline.countdownDays })}
                  size="small"
                  color={getCountdownColor(deadline.countdownDays)}
                  sx={{ fontSize: '0.7rem', height: 22 }}
                />
                <Chip
                  label={t(STATUS_LABELS[deadline.status])}
                  size="small"
                  variant="outlined"
                  sx={{ fontSize: '0.7rem', height: 22 }}
                />
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
}
