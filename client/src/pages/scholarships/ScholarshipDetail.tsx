import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { Box, Typography, Paper } from '@mui/material';

export default function ScholarshipDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('scholarshipDetail.title')}
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">
          {t('scholarshipId', { id })} - {t('comingSoon')}
        </Typography>
      </Paper>
    </Box>
  );
}
