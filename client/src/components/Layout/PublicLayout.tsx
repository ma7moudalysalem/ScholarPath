import { useState, useEffect } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  AppBar,
  Avatar,
  Box,
  Button,
  Container,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Snackbar,
  Alert,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Translate as TranslateIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
} from '@mui/icons-material';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { useAuthModal } from '@/hooks/useAuthModal';

export function PublicLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const { toggleLanguage, themeMode, toggleTheme } = useUiStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);
  const openAuthModal = useAuthModal((s) => s.open);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [sessionExpired, setSessionExpired] = useState(false);

  useEffect(() => {
    if (sessionStorage.getItem('sessionExpired') === 'true') {
      setSessionExpired(true);
      sessionStorage.removeItem('sessionExpired');
    }
  }, []);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      {/* Header */}
      <AppBar position="sticky" color="inherit" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Container maxWidth="lg">
          <Toolbar disableGutters>
            {isMobile && (
              <IconButton edge="start" onClick={() => setMobileMenuOpen(true)} sx={{ mr: 1 }}>
                <MenuIcon />
              </IconButton>
            )}

            <Typography
              variant="h6"
              fontWeight={700}
              color="primary"
              sx={{ cursor: 'pointer', mr: 'auto' }}
              onClick={() => navigate('/')}
            >
              {t('appName')}
            </Typography>

            {!isMobile && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mr: 2 }}>
                <Button color="inherit" onClick={() => navigate('/')}>
                  {t('nav.home')}
                </Button>
              </Box>
            )}

            <IconButton onClick={toggleTheme} sx={{ mr: 0.5 }} aria-label={t('nav.toggleTheme')}>
              {themeMode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
            </IconButton>

            <IconButton onClick={toggleLanguage} sx={{ mr: 1 }} aria-label={t('nav.toggleLanguage')}>
              <TranslateIcon />
            </IconButton>

            {isAuthenticated ? (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <IconButton onClick={() => navigate('/dashboard')}>
                  <Avatar
                    src={user?.profileImageUrl ?? undefined}
                    sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}
                  >
                    {user?.firstName?.[0]}
                  </Avatar>
                </IconButton>
              </Box>
            ) : (
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Button variant="outlined" onClick={() => openAuthModal('login')}>
                  {t('login')}
                </Button>
                {!isMobile && (
                  <Button variant="contained" onClick={() => openAuthModal('register')}>
                    {t('register')}
                  </Button>
                )}
              </Box>
            )}
          </Toolbar>
        </Container>
      </AppBar>

      {/* Mobile drawer */}
      <Drawer
        open={mobileMenuOpen}
        onClose={() => setMobileMenuOpen(false)}
        anchor="left"
      >
        <Box sx={{ width: 250, pt: 2 }}>
          <List>
            <ListItemButton onClick={() => { navigate('/'); setMobileMenuOpen(false); }}>
              <ListItemText primary={t('nav.home')} />
            </ListItemButton>
            {!isAuthenticated && (
              <>
                <ListItemButton onClick={() => { openAuthModal('login'); setMobileMenuOpen(false); }}>
                  <ListItemText primary={t('login')} />
                </ListItemButton>
                <ListItemButton onClick={() => { openAuthModal('register'); setMobileMenuOpen(false); }}>
                  <ListItemText primary={t('register')} />
                </ListItemButton>
              </>
            )}
            {isAuthenticated && (
              <ListItemButton onClick={() => { navigate('/dashboard'); setMobileMenuOpen(false); }}>
                <ListItemText primary={t('dashboard')} />
              </ListItemButton>
            )}
          </List>
        </Box>
      </Drawer>

      {/* Session expired banner */}
      <Snackbar
        open={sessionExpired}
        autoHideDuration={6000}
        onClose={() => setSessionExpired(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity="warning" onClose={() => setSessionExpired(false)} variant="filled">
          {t('sessionExpired')}
        </Alert>
      </Snackbar>

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
          borderTop: 1,
          borderColor: 'divider',
        }}
      >
        <Typography variant="body2" color="text.secondary">
          &copy; {new Date().getFullYear()} {t('appName')}. {t('allRightsReserved')}
        </Typography>
      </Box>
    </Box>
  );
}
