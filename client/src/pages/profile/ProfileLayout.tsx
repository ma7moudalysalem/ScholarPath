import { useState, useEffect } from 'react';
import { Outlet, useLocation, Link as RouterLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper, Avatar, Tabs, Tab, CircularProgress, Container } from '@mui/material';
import { useAuthStore } from '@/stores/authStore';
import { UserRole, UpgradeRequestDto } from '@/types';
import { upgradeRequestService } from '@/services/upgradeRequestService';

export default function ProfileLayout() {
    const { t } = useTranslation();
    const location = useLocation();
    const user = useAuthStore((s) => s.user);

    const [upgradeRequest, setUpgradeRequest] = useState<UpgradeRequestDto | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchStatus = async () => {
            try {
                const data = await upgradeRequestService.getMyUpgradeRequestStatus();
                setUpgradeRequest(data);
            } catch (error) {
                console.error('Failed to fetch upgrade request status:', error);
            } finally {
                setLoading(false);
            }
        };
        fetchStatus();
    }, []);

    if (!user) return null;

    // Determine if User can see Upgrade Tab
    let showUpgradeTab = false;
    if (user.role === UserRole.Student) {
        if (!loading) {
            if (!upgradeRequest || (upgradeRequest.status !== 0 && upgradeRequest.status !== 1)) {
                showUpgradeTab = true;
            }
        }
    }

    const currentTab = location.pathname.endsWith('/security')
        ? 'security'
        : location.pathname.endsWith('/upgrade')
            ? 'upgrade'
            : 'overview';

    return (
        <Container maxWidth="lg" sx={{ pt: 2, pb: 6 }}>
            <Typography variant="h4" fontWeight="bold" gutterBottom>
                {t('nav.profile', 'Profile')}
            </Typography>

            {/* User Info Header */}
            <Paper sx={{ p: 4, mb: 4, display: 'flex', alignItems: 'center', gap: 3, borderRadius: 3 }} elevation={0} variant="outlined">
                <Avatar sx={{ width: 80, height: 80, fontSize: '2.5rem', bgcolor: 'primary.main' }} src={user.profileImageUrl || undefined}>
                    {user.firstName[0]}
                </Avatar>
                <Box>
                    <Typography variant="h5" fontWeight="bold">
                        {user.firstName} {user.lastName}
                    </Typography>
                    <Typography color="text.secondary" gutterBottom>
                        {user.email}
                    </Typography>
                    <Typography variant="subtitle2" color="primary" sx={{ textTransform: 'uppercase', letterSpacing: 1, mt: 0.5 }}>
                        {UserRole[user.role]}
                    </Typography>
                </Box>
            </Paper>

            {/* Navigation Tabs */}
            <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 4 }}>
                <Tabs value={currentTab} variant="scrollable" scrollButtons="auto">
                    <Tab label={t('profile.overview', 'Overview')} value="overview" component={RouterLink} to="/profile" />
                    <Tab label={t('profile.security', 'Security')} value="security" component={RouterLink} to="/profile/security" />
                    {showUpgradeTab && (
                        <Tab label={t('profile.upgrade', 'Upgrade Account')} value="upgrade" component={RouterLink} to="/profile/upgrade" />
                    )}
                    {loading && <Tab disabled icon={<CircularProgress size={16} />} sx={{ minWidth: 40 }} />}
                </Tabs>
            </Box>

            {/* Content */}
            <Box sx={{ minHeight: '400px' }}>
                <Outlet />
            </Box>
        </Container>
    );
}
