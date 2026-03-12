import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { alpha } from '@mui/material/styles';
import {
  Box,
  Typography,
  Button,
  useTheme,
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
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const user = useAuthStore((s) => s.user);
  const primary = theme.palette.primary.main;
  const displayFont = theme.typography.h2.fontFamily as string;

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', 'summary'],
    queryFn: () => dashboardService.getSummary(),
  });

  const statusCounts = data?.statusCounts ?? {};
  const deadlines = data?.deadlinesSoon ?? [];
  const actions = data?.recommendedActions ?? [];

  const savedCount = statusCounts['Saved'] ?? 0;
  const trackerTotal = Object.entries(statusCounts)
    .filter(([key]) => key !== 'Saved')
    .reduce((sum, [, v]) => sum + v, 0);

  // Show the empty/onboarding state only when the user has nothing at all
  const isNewUser = !isLoading && savedCount === 0 && trackerTotal === 0 && deadlines.length === 0;

  return (
    <Box>
      {/* Page header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 600, mb: 0.5 }}>
          {t('dashboard.title')}
        </Typography>
        <Typography variant="body1" color="text.secondary">
          {t('welcomeBack', { name: user?.firstName })}
        </Typography>
      </Box>

      {/* Status tiles */}
      <StatusTiles statusCounts={statusCounts} isLoading={isLoading} />

      {/* Empty state — shown only to brand-new users with nothing saved or tracked */}
      {isNewUser ? (
        <Box sx={{ mt: 5 }}>
          <Grid container spacing={3} justifyContent="center">
            {[
              {
                icon: <SchoolOutlinedIcon sx={{ fontSize: 40, color: primary }} />,
                title: t('dashboard.browseScholarships'),
                desc: t('dashboard.browseScholarshipsDesc'),
                action: () => navigate('/scholarships'),
                btnLabel: t('dashboard.browseScholarships'),
                variant: 'contained' as const,
              },
              {
                icon: <PersonOutlineIcon sx={{ fontSize: 40, color: primary }} />,
                title: t('dashboard.completeProfile'),
                desc: t('dashboard.completeProfileDesc'),
                action: () => navigate('/profile'),
                btnLabel: t('dashboard.completeProfile'),
                variant: 'outlined' as const,
              },
            ].map((card) => (
              <Grid key={card.title} size={{ xs: 12, sm: 6, md: 4 }}>
                <Box sx={{
                  p: 4,
                  textAlign: 'center',
                  borderRadius: 3,
                  border: `1px solid ${alpha(primary, 0.1)}`,
                  bgcolor: 'background.paper',
                  transition: 'all 0.25s ease',
                  '&:hover': {
                    borderColor: alpha(primary, 0.28),
                    transform: 'translateY(-3px)',
                    boxShadow: isDark ? '0 12px 32px rgba(0,0,0,0.45)' : '0 8px 24px rgba(0,0,0,0.08)',
                  },
                }}>
                  <Box sx={{ mb: 2 }}>{card.icon}</Box>
                  <Typography sx={{
                    fontFamily: displayFont,
                    fontSize: '1.5rem',
                    fontWeight: 600,
                    mb: 1,
                    color: 'text.primary',
                  }}>
                    {card.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 3, lineHeight: 1.7 }}>
                    {card.desc}
                  </Typography>
                  <Button
                    variant={card.variant}
                    onClick={card.action}
                    sx={{ borderRadius: 50, px: 3 }}
                  >
                    {card.btnLabel}
                  </Button>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Box>
      ) : (
        <Grid container spacing={3} sx={{ mt: 3 }}>
          <Grid size={{ xs: 12, md: 8 }}>
            <DeadlinesWidget deadlines={deadlines} isLoading={isLoading} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <ActionsWidget actions={actions} isLoading={isLoading} />
          </Grid>
        </Grid>
      )}
    </Box>
  );
}
