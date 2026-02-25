import { useTranslation } from 'react-i18next';
import { Box, Container, Paper, Typography } from '@mui/material';

export default function Login() {
  const { t } = useTranslation();

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h4" gutterBottom>
          {t('login')}
        </Typography>
        <Box sx={{ mt: 2 }}>
          <Typography color="text.secondary">{t('comingSoon')}</Typography>
        </Box>
      </Paper>
    </Container>
  );
}
