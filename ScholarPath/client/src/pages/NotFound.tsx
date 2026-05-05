import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Container, Typography, Button, Paper } from '@mui/material';

export default function NotFound() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="h1" color="primary" sx={{ fontSize: '6rem', fontWeight: 700 }}>
          404
        </Typography>
        <Typography variant="h5" gutterBottom>
          {t('notFound.title')}
        </Typography>
        <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
          {t('notFound.description')}
        </Typography>
        <Button variant="contained" onClick={() => navigate('/')}>
          {t('notFound.goHome')}
        </Button>
      </Paper>
    </Container>
  );
}
