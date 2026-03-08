import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  Paper,
  Typography,
  List,
  ListItemButton,
  ListItemText,
  Chip,
  Box,
  Button,
  Skeleton,
  Stack,
} from '@mui/material';
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
  const isAr = i18n.language === 'ar';

  if (isLoading) {
    return (
      <Paper sx={{ p: 3 }}>
        <Skeleton width={200} height={32} sx={{ mb: 2 }} />
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} height={60} sx={{ mb: 1 }} />
        ))}
      </Paper>
    );
  }

  const displayedDeadlines = deadlines.slice(0, 5);
  const hasMore = deadlines.length > 5;

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        {t('dashboard.upcomingDeadlines')}
      </Typography>

      {displayedDeadlines.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ py: 3, textAlign: 'center' }}>
          {t('dashboard.noDeadlines')}
        </Typography>
      ) : (
        <List disablePadding>
          {displayedDeadlines.map((deadline) => (
            <ListItemButton
              key={deadline.scholarshipId}
              onClick={() => navigate(`/scholarships/${deadline.scholarshipId}`)}
              sx={{ borderRadius: 1, mb: 0.5 }}
            >
              <ListItemText
                primary={isAr && deadline.titleAr ? deadline.titleAr : deadline.title}
                secondary={deadline.providerName}
                primaryTypographyProps={{ noWrap: true }}
              />
              <Stack direction="row" spacing={1} alignItems="center" sx={{ ml: 1, flexShrink: 0 }}>
                <Chip
                  label={t('dashboard.daysLeft', { count: deadline.countdownDays })}
                  size="small"
                  color={getCountdownColor(deadline.countdownDays)}
                />
                <Chip
                  label={t(STATUS_LABELS[deadline.status])}
                  size="small"
                  variant="outlined"
                />
              </Stack>
            </ListItemButton>
          ))}
        </List>
      )}

      {hasMore && (
        <Box sx={{ textAlign: 'center', mt: 1 }}>
          <Button size="small" onClick={() => navigate('/dashboard/tracker')}>
            {t('dashboard.viewAll')}
          </Button>
        </Box>
      )}
    </Paper>
  );
}
