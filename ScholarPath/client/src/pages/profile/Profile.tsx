import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper } from '@mui/material';

export default function Profile() {
  const { t } = useTranslation();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('profile')}
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">{t('comingSoon')}</Typography>
      </Paper>
    </Box>
  );
}
