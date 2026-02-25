import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper } from '@mui/material';

export default function ScholarshipList() {
  const { t } = useTranslation();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('scholarshipList.title')}
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">{t('comingSoon')}</Typography>
      </Paper>
    </Box>
  );
}
