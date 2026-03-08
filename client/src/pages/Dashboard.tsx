import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Paper,
  Button,
  Stack,
} from '@mui/material';
import Grid from '@mui/material/Grid2';
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined';
import PersonOutlineIcon from '@mui/icons-material/PersonOutline';
import { useAuthStore } from '@/stores/authStore';
import { dashboardService } from '@/services/dashboardService';
import StatusTiles from '@/components/dashboard/StatusTiles';
import DeadlinesWidget from '@/components/dashboard/DeadlinesWidget';
import ActionsWidget from '@/components/dashboard/ActionsWidget';

export default function Dashboard() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => dashboardService.getSummary(),
  });

  const statusCounts = data?.statusCounts ?? {};
  const deadlines = data?.deadlinesSoon ?? [];
  const actions = data?.recommendedActions ?? [];

  const allCountsZero =
    !isLoading &&
    Object.values(statusCounts).every((c) => c === 0) &&
    actions.length === 0;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('dashboard.title')}
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        {t('welcomeBack', { name: user?.firstName })}
      </Typography>

      {/* Status Tiles */}
      <StatusTiles statusCounts={statusCounts} isLoading={isLoading} />

      {/* Empty state */}
      {allCountsZero ? (
        <Box sx={{ mt: 4, textAlign: 'center' }}>
          <Typography variant="h6" gutterBottom>
            {t('dashboard.noDataYet')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            {t('dashboard.noDataDesc')}
          </Typography>
          <Grid container spacing={3} justifyContent="center">
            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
              <Paper sx={{ p: 4, textAlign: 'center' }}>
                <SchoolOutlinedIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
                <Typography variant="h6" gutterBottom>
                  {t('dashboard.browseScholarships')}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  {t('dashboard.browseScholarshipsDesc')}
                </Typography>
                <Button variant="contained" onClick={() => navigate('/scholarships')}>
                  {t('dashboard.browseScholarships')}
                </Button>
              </Paper>
            </Grid>
            <Grid size={{ xs: 12, sm: 6, md: 4 }}>
              <Paper sx={{ p: 4, textAlign: 'center' }}>
                <PersonOutlineIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
                <Typography variant="h6" gutterBottom>
                  {t('dashboard.completeProfile')}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  {t('dashboard.completeProfileDesc')}
                </Typography>
                <Button variant="outlined" onClick={() => navigate('/profile')}>
                  {t('dashboard.completeProfile')}
                </Button>
              </Paper>
            </Grid>
          </Grid>
        </Box>
      ) : (
        /* Deadlines + Actions two-column layout */
        <Stack spacing={3} sx={{ mt: 3 }}>
          <Grid container spacing={3}>
            <Grid size={{ xs: 12, md: 8 }}>
              <DeadlinesWidget deadlines={deadlines} isLoading={isLoading} />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <ActionsWidget actions={actions} isLoading={isLoading} />
            </Grid>
          </Grid>
        </Stack>
      )}
    </Box>
  );
}
