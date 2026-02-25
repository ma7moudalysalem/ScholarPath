import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Box, Button, Container, Typography, Stack, Paper } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { School, People, TrendingUp } from '@mui/icons-material';

export default function Home() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const features = [
    {
      icon: <School sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('scholarships'),
      description: t('home.featureScholarships'),
    },
    {
      icon: <People sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('community'),
      description: t('home.featureCommunity'),
    },
    {
      icon: <TrendingUp sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('dashboard'),
      description: t('home.featureTracking'),
    },
  ];

  return (
    <Box>
      {/* Hero Section */}
      <Box
        sx={{
          bgcolor: 'primary.main',
          color: 'primary.contrastText',
          py: { xs: 8, md: 12 },
        }}
      >
        <Container maxWidth="md" sx={{ textAlign: 'center' }}>
          <Typography variant="h2" fontWeight={700} gutterBottom>
            {t('appName')}
          </Typography>
          <Typography variant="h5" sx={{ mb: 4, opacity: 0.9 }}>
            {t('home.hero')}
          </Typography>
          <Stack direction="row" spacing={2} justifyContent="center">
            <Button
              variant="contained"
              color="secondary"
              size="large"
              onClick={() => navigate('/register')}
            >
              {t('register')}
            </Button>
            <Button
              variant="outlined"
              size="large"
              sx={{ color: 'white', borderColor: 'white' }}
              onClick={() => navigate('/login')}
            >
              {t('login')}
            </Button>
          </Stack>
        </Container>
      </Box>

      {/* Features Section */}
      <Container maxWidth="lg" sx={{ py: 8 }}>
        <Grid container spacing={4}>
          {features.map((feature, index) => (
            <Grid key={index} size={{ xs: 12, md: 4 }}>
              <Paper sx={{ p: 4, textAlign: 'center', height: '100%' }}>
                {feature.icon}
                <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
                  {feature.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {feature.description}
                </Typography>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  );
}
