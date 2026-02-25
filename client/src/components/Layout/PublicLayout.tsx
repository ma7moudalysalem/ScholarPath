import { Outlet, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AppBar,
  Box,
  Button,
  Container,
  IconButton,
  Toolbar,
  Typography,
} from '@mui/material';
import { Translate as TranslateIcon } from '@mui/icons-material';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';

export function PublicLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { toggleLanguage } = useUiStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      {/* Header */}
      <AppBar position="sticky" color="inherit" elevation={0}>
        <Container maxWidth="lg">
          <Toolbar disableGutters>
            <Typography
              variant="h6"
              fontWeight={700}
              color="primary"
              sx={{ cursor: 'pointer', mr: 'auto' }}
              onClick={() => navigate('/')}
            >
              {t('appName')}
            </Typography>

            <IconButton onClick={toggleLanguage} sx={{ mr: 1 }} aria-label={t('nav.toggleLanguage')}>
              <TranslateIcon />
            </IconButton>

            {isAuthenticated ? (
              <Button variant="contained" onClick={() => navigate('/dashboard')}>
                {t('dashboard')}
              </Button>
            ) : (
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Button variant="outlined" onClick={() => navigate('/login')}>
                  {t('login')}
                </Button>
                <Button variant="contained" onClick={() => navigate('/register')}>
                  {t('register')}
                </Button>
              </Box>
            )}
          </Toolbar>
        </Container>
      </AppBar>

      {/* Content */}
      <Box sx={{ flex: 1 }}>
        <Outlet />
      </Box>

      {/* Footer */}
      <Box
        component="footer"
        sx={{
          py: 3,
          px: 2,
          mt: 'auto',
          backgroundColor: 'background.paper',
          textAlign: 'center',
        }}
      >
        <Typography variant="body2" color="text.secondary">
          &copy; {new Date().getFullYear()} {t('appName')}. {t('allRightsReserved')}
        </Typography>
      </Box>
    </Box>
  );
}
