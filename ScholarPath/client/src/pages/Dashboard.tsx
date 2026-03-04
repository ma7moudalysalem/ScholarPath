import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { useAuthStore } from '@/stores/authStore';

export default function Dashboard() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('dashboard')}
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        {t('welcomeBack', { name: user?.firstName })}
      </Typography>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3, textAlign: 'center' }}>
            <Typography variant="h3" color="primary">
              0
            </Typography>
            <Typography color="text.secondary">{t('scholarships')}</Typography>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3, textAlign: 'center' }}>
            <Typography variant="h3" color="primary">
              0
            </Typography>
            <Typography color="text.secondary">{t('notifications')}</Typography>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3, textAlign: 'center' }}>
            <Typography variant="h3" color="primary">
              0
            </Typography>
            <Typography color="text.secondary">{t('community')}</Typography>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}
