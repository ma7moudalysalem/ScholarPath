import { useTranslation } from 'react-i18next';
import {
  Alert,
  Box,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Paper,
  Typography,
} from '@mui/material';
import Grid from '@mui/material/Grid2';
import { School, BusinessCenter, Work } from '@mui/icons-material';
import { useAuth } from '@/hooks/useAuth';
import { AccountStatus, UserRole } from '@/types';

export default function Onboarding() {
  const { t } = useTranslation();
  const { user, completeOnboarding, isLoading } = useAuth();

  const accountTypes = [
    {
      role: UserRole.Student,
      icon: <School sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.student'),
      description: t('onboarding.studentDesc'),
    },
    {
      role: UserRole.Consultant,
      icon: <BusinessCenter sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.consultant'),
      description: t('onboarding.consultantDesc'),
    },
    {
      role: UserRole.Company,
      icon: <Work sx={{ fontSize: 56, color: 'primary.main' }} />,
      title: t('onboarding.company'),
      description: t('onboarding.companyDesc'),
    },
  ];

  const handleRoleSelection = async (role: UserRole) => {
    try {
      await completeOnboarding({ selectedRole: role });
    } catch {
      // Error will be handled by the API interceptor (401 -> redirect)
      // For other errors, the user sees no feedback currently
      // This will be improved when the page is fully implemented
    }
  };

  if (user?.isOnboardingComplete && user.accountStatus === AccountStatus.Pending) {
    return (
      <Container maxWidth="sm" sx={{ py: 8 }}>
        <Paper sx={{ p: 4 }}>
          <Alert severity="info">{t('onboarding.pendingApproval')}</Alert>
        </Paper>
      </Container>
    );
  }

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
          {accountTypes.map((type) => (
            <Grid key={type.role} size={{ xs: 12, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%' }}>
                <CardActionArea
                  sx={{ p: 3, textAlign: 'center', height: '100%' }}
                  onClick={() => handleRoleSelection(type.role)}
                  disabled={isLoading}
                >
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

        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
            <CircularProgress size={24} />
          </Box>
        ) : null}
      </Paper>
    </Container>
  );
}
