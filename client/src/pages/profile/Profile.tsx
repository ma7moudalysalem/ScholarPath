import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Avatar,
  Box,
  Card,
  CardContent,
  Chip,
  Container,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import {
  Person as PersonIcon,
  Security as SecurityIcon,
  Upgrade as UpgradeIcon,
} from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types';
import SecurityTab from './Security';
import UpgradeAccountTab from './UpgradeAccount';

export default function Profile() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();
  const user = useAuthStore((s) => s.user);

  const tabParam = searchParams.get('tab');
  const tabMap: Record<string, number> = { overview: 0, security: 1, upgrade: 2 };
  const [activeTab, setActiveTab] = useState(tabMap[tabParam ?? 'overview'] ?? 0);

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
    const tabNames = ['overview', 'security', 'upgrade'];
    setSearchParams({ tab: tabNames[newValue] ?? 'overview' });
  };

  const roleLabel: Record<number, string> = {
    [UserRole.Student]: t('onboarding.student'),
    [UserRole.Consultant]: t('onboarding.consultant'),
    [UserRole.Company]: t('onboarding.company'),
    [UserRole.Admin]: t('nav.admin'),
  };

  const showUpgradeTab = user?.role === UserRole.Student;

  return (
    <Container maxWidth="md">
      {/* User Info Header */}
      <Card sx={{ mb: 3 }}>
        <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Avatar
            src={user?.profileImageUrl ?? undefined}
            sx={{ width: 72, height: 72, bgcolor: 'primary.main', fontSize: 28 }}
          >
            {user?.firstName?.[0]}
          </Avatar>
          <Box>
            <Typography variant="h5" fontWeight={700}>
              {user?.firstName} {user?.lastName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {user?.email}
            </Typography>
            {user?.role !== undefined && (
              <Chip
                label={roleLabel[user.role] ?? ''}
                size="small"
                color="primary"
                variant="outlined"
                sx={{ mt: 0.5 }}
              />
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Tabs */}
      <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 3 }}>
        <Tab icon={<PersonIcon />} label={t('profile.overview')} iconPosition="start" />
        <Tab icon={<SecurityIcon />} label={t('security')} iconPosition="start" />
        {showUpgradeTab && (
          <Tab icon={<UpgradeIcon />} label={t('profile.upgradeAccount')} iconPosition="start" />
        )}
      </Tabs>

      {/* Tab content */}
      {activeTab === 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              {t('profile.overview')}
            </Typography>
            <Typography color="text.secondary">
              {t('profile.overviewDesc')}
            </Typography>
          </CardContent>
        </Card>
      )}
      {activeTab === 1 && <SecurityTab />}
      {activeTab === 2 && showUpgradeTab && <UpgradeAccountTab />}
    </Container>
  );
}
