import { useTranslation } from 'react-i18next';
import { Box, Typography } from '@mui/material';

export default function Overview() {
    const { t } = useTranslation();

    return (
        <Box>
            <Typography variant="h5" gutterBottom>
                {t('profile.overview', 'Overview')}
            </Typography>
            <Typography color="text.secondary">{t('comingSoon')}</Typography>
        </Box>
    );
}
