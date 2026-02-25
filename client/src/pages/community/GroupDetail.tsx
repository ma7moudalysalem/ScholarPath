import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { Box, Typography, Paper } from '@mui/material';

export default function GroupDetail() {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('groupDetail')}
      </Typography>
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">
          {t('groupId', { id })} - {t('comingSoon')}
        </Typography>
      </Paper>
    </Box>
  );
}
