import { Box, Typography, Button, Paper, Avatar } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { useTranslation } from 'react-i18next';
import { Work as WorkIcon, Business as BusinessIcon } from '@mui/icons-material';

export default function UpgradeAccount() {
    const { t } = useTranslation();

    const handleUpgradeClick = (role: string) => {
        // Scaffold for future task
        console.log(`Open upgrade modal for: ${role}`);
    };

    return (
        <Box>
            <Typography variant="h5" gutterBottom>
                {t('profile.upgradeTitle', 'Upgrade Your Account')}
            </Typography>
            <Typography color="text.secondary" paragraph>
                {t('profile.upgradeDescription', 'Unlock more features and give back to the community by upgrading your account role.')}
            </Typography>

            <Grid container spacing={4} sx={{ mt: 4 }}>
                {/* Consultant Card */}
                <Grid size={{ xs: 12, md: 6 }}>
                    <Paper
                        elevation={0}
                        variant="outlined"
                        sx={{
                            p: { xs: 3, md: 5 },
                            height: '100%',
                            display: 'flex',
                            flexDirection: 'column',
                            alignItems: 'center',
                            textAlign: 'center',
                            borderRadius: 4,
                            transition: 'transform 0.2s, box-shadow 0.2s',
                            '&:hover': { transform: 'translateY(-4px)', boxShadow: 4, borderColor: 'primary.main' }
                        }}
                    >
                        <Avatar sx={{ width: 80, height: 80, bgcolor: 'primary.light', color: 'primary.main', mb: 3 }}>
                            <WorkIcon sx={{ fontSize: 40 }} />
                        </Avatar>
                        <Typography variant="h5" fontWeight="bold" gutterBottom>
                            {t('profile.becomeConsultant', 'Become a Consultant')}
                        </Typography>
                        <Typography color="text.secondary" sx={{ flexGrow: 1, mb: 4, px: { xs: 0, sm: 2 } }}>
                            {t('profile.consultantDesc', 'Share your expertise, mentor students, and guide them towards achieving their academic dreams.')}
                        </Typography>
                        <Button variant="contained" color="primary" size="large" fullWidth sx={{ py: 1.5, borderRadius: 2 }} onClick={() => handleUpgradeClick('Consultant')}>
                            {t('profile.applyConsultant', 'Apply for Consultant')}
                        </Button>
                    </Paper>
                </Grid>

                {/* Company Card */}
                <Grid size={{ xs: 12, md: 6 }}>
                    <Paper
                        elevation={0}
                        variant="outlined"
                        sx={{
                            p: { xs: 3, md: 5 },
                            height: '100%',
                            display: 'flex',
                            flexDirection: 'column',
                            alignItems: 'center',
                            textAlign: 'center',
                            borderRadius: 4,
                            transition: 'transform 0.2s, box-shadow 0.2s',
                            '&:hover': { transform: 'translateY(-4px)', boxShadow: 4, borderColor: 'secondary.main' }
                        }}
                    >
                        <Avatar sx={{ width: 80, height: 80, bgcolor: 'secondary.light', color: 'secondary.main', mb: 3 }}>
                            <BusinessIcon sx={{ fontSize: 40 }} />
                        </Avatar>
                        <Typography variant="h5" fontWeight="bold" gutterBottom>
                            {t('profile.becomeCompany', 'Become a Company Owner')}
                        </Typography>
                        <Typography color="text.secondary" sx={{ flexGrow: 1, mb: 4, px: { xs: 0, sm: 2 } }}>
                            {t('profile.companyDesc', 'Post scholarships, fund students, and discover top talent for your organization globally.')}
                        </Typography>
                        <Button variant="contained" color="secondary" size="large" fullWidth sx={{ py: 1.5, borderRadius: 2 }} onClick={() => handleUpgradeClick('Company')}>
                            {t('profile.applyCompany', 'Apply for Company')}
                        </Button>
                    </Paper>
                </Grid>
            </Grid>
        </Box>
    );
}
