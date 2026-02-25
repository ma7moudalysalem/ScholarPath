import { useTranslation } from 'react-i18next';
import { Box, Container, Paper, Typography, Card, CardActionArea, CardContent } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { School, BusinessCenter, Work } from '@mui/icons-material';

export default function Onboarding() {
  const { t } = useTranslation();

  const accountTypes = [
    {
      icon: <School sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.student'),
      description: t('onboarding.studentDesc'),
    },
    {
      icon: <BusinessCenter sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.consultant'),
      description: t('onboarding.consultantDesc'),
    },
    {
      icon: <Work sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.company'),
      description: t('onboarding.companyDesc'),
    },
  ];

  return (
    <Container maxWidth="md" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Typography variant="h4" gutterBottom>
            {t('onboarding.title')}
          </Typography>
          <Typography color="text.secondary">
            {t('onboarding.subtitle')}
          </Typography>
        </Box>

        <Grid container spacing={3}>
          {accountTypes.map((type, index) => (
            <Grid key={index} size={{ xs: 12, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%' }}>
                <CardActionArea sx={{ p: 3, textAlign: 'center', height: '100%' }}>
                  <CardContent>
                    {type.icon}
                    <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
                      {type.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {type.description}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Paper>
    </Container>
  );
}
