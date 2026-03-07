import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Typography,
} from '@mui/material';
import {
  School as ConsultantIcon,
  Business as CompanyIcon,
} from '@mui/icons-material';
import { ConsultantUpgradeForm } from '@/components/upgrade/ConsultantUpgradeForm';
import { CompanyUpgradeForm } from '@/components/upgrade/CompanyUpgradeForm';

type UpgradeView = 'choose' | 'consultant' | 'company';

export default function UpgradeAccountTab() {
  const { t } = useTranslation();
  const [view, setView] = useState<UpgradeView>('choose');

  if (view === 'consultant') {
    return <ConsultantUpgradeForm onBack={() => setView('choose')} />;
  }

  if (view === 'company') {
    return <CompanyUpgradeForm onBack={() => setView('choose')} />;
  }

  return (
    <Box>
      <Typography variant="h6" gutterBottom>
        {t('profile.upgradeAccount')}
      </Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>
        {t('profile.upgradeDesc')}
      </Typography>

      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <Card sx={{ flex: 1, minWidth: 240 }}>
          <CardActionArea onClick={() => setView('consultant')} sx={{ p: 2 }}>
            <CardContent sx={{ textAlign: 'center' }}>
              <ConsultantIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
              <Typography variant="h6">{t('upgrade.becomeConsultant')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {t('onboarding.consultantDesc')}
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>

        <Card sx={{ flex: 1, minWidth: 240 }}>
          <CardActionArea onClick={() => setView('company')} sx={{ p: 2 }}>
            <CardContent sx={{ textAlign: 'center' }}>
              <CompanyIcon sx={{ fontSize: 48, color: 'primary.main', mb: 1 }} />
              <Typography variant="h6">{t('upgrade.becomeCompany')}</Typography>
              <Typography variant="body2" color="text.secondary">
                {t('onboarding.companyDesc')}
              </Typography>
            </CardContent>
          </CardActionArea>
        </Card>
      </Box>
    </Box>
  );
}
