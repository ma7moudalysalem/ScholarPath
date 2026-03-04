import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper } from '@mui/material';

export default function UpgradeRequests() {
  const { t } = useTranslation();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('admin.upgradeRequests')}
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">{t('admin.noRequests')}</Typography>
      </Paper>
    </Box>
  );
}
