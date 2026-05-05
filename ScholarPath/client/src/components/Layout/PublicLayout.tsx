import { useState, useEffect } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { alpha } from '@mui/material/styles';
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
  Divider,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Translate as TranslateIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
  Close as CloseIcon,
} from '@mui/icons-material';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { useAuthModal } from '@/hooks/useAuthModal';

export function PublicLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const isDark = theme.palette.mode === 'dark';
  const displayFont = theme.typography.h2.fontFamily as string;
  const primary = theme.palette.primary.main;

  const { toggleLanguage, themeMode, toggleTheme } = useUiStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);
  const openAuthModal = useAuthModal((s) => s.open);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [sessionExpired, setSessionExpired] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    if (sessionStorage.getItem('sessionExpired') === 'true') {
      setSessionExpired(true);
      sessionStorage.removeItem('sessionExpired');
    }
  }, []);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      {/* ─── HEADER ─── */}
      <AppBar
        position="fixed"
        sx={{
          backgroundColor: scrolled ? undefined : 'transparent',
          backdropFilter: scrolled ? 'blur(20px)' : 'none',
          WebkitBackdropFilter: scrolled ? 'blur(20px)' : 'none',
          borderBottom: scrolled ? undefined : '1px solid transparent',
          transition: 'all 0.3s ease',
          boxShadow: 'none',
        }}
      >
        <Container maxWidth="lg">
          <Toolbar disableGutters sx={{ minHeight: { xs: 64, md: 72 } }}>
            {isMobile && (
              <IconButton edge="start" onClick={() => setMobileMenuOpen(true)} sx={{ mr: 1 }}>
                <MenuIcon />
              </IconButton>
            )}

            {/* Logo */}
            <Box
              onClick={() => navigate('/')}
              sx={{
                cursor: 'pointer',
                mr: 'auto',
                display: 'flex',
                alignItems: 'baseline',
                gap: 0.5,
              }}
            >
              <Typography
                sx={{
                  fontFamily: displayFont,
                  fontSize: '1.7rem',
                  fontWeight: 600,
                  color: primary,
                  lineHeight: 1,
                  letterSpacing: '-0.01em',
                }}
              >
                {t('appName')}
              </Typography>
            </Box>

            {/* Desktop nav */}
            {!isMobile && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mr: 2 }}>
                <Button
                  color="inherit"
                  onClick={() => navigate('/')}
                  sx={{ fontWeight: 500, opacity: 0.75, '&:hover': { opacity: 1 } }}
                >
                  {t('nav.home')}
                </Button>
              </Box>
            )}

            {/* Controls */}
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <IconButton
                onClick={toggleTheme}
                aria-label={t('nav.toggleTheme')}
                sx={{ opacity: 0.65, '&:hover': { opacity: 1, color: primary } }}
              >
                {themeMode === 'light' ? (
                  <DarkModeIcon fontSize="small" />
                ) : (
                  <LightModeIcon fontSize="small" />
                )}
              </IconButton>

              <IconButton
                onClick={toggleLanguage}
                aria-label={t('nav.toggleLanguage')}
                sx={{ opacity: 0.65, '&:hover': { opacity: 1, color: primary }, mr: 1 }}
              >
                <TranslateIcon fontSize="small" />
              </IconButton>

              {isAuthenticated ? (
                <IconButton onClick={() => navigate('/dashboard')} sx={{ p: 0.5 }}>
                  <Avatar
                    src={user?.profileImageUrl ?? undefined}
                    sx={{ width: 34, height: 34, fontSize: '0.875rem' }}
                  >
                    {user?.firstName?.[0]}
                  </Avatar>
                </IconButton>
              ) : (
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Button
                    variant="outlined"
                    size="small"
                    onClick={() => openAuthModal('login')}
                    sx={{ borderRadius: 50, px: 2.5 }}
                  >
                    {t('login')}
                  </Button>
                  {!isMobile && (
                    <Button
                      variant="contained"
                      size="small"
                      onClick={() => openAuthModal('register')}
                      sx={{ borderRadius: 50, px: 2.5 }}
                    >
                      {t('register')}
                    </Button>
                  )}
                </Box>
              )}
            </Box>
          </Toolbar>
        </Container>
      </AppBar>

      {/* Spacer for fixed AppBar */}
      <Box sx={{ height: { xs: 64, md: 72 } }} />

      {/* ─── MOBILE DRAWER ─── */}
      <Drawer
        open={mobileMenuOpen}
        onClose={() => setMobileMenuOpen(false)}
        anchor="left"
        PaperProps={{
          sx: {
            width: 280,
            // The MuiDrawer theme override applies sidebarBg here
            pt: 2,
          },
        }}
      >
        <Box
          sx={{
            px: 2,
            pb: 2,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Typography
            sx={{ fontFamily: displayFont, fontSize: '1.6rem', fontWeight: 600, color: primary }}
          >
            {t('appName')}
          </Typography>
          <IconButton
            onClick={() => setMobileMenuOpen(false)}
            sx={{ opacity: 0.5, '&:hover': { opacity: 1 } }}
          >
            <CloseIcon />
          </IconButton>
        </Box>
        <Divider sx={{ mb: 1 }} />
        <List sx={{ px: 1 }}>
          <ListItemButton
            onClick={() => {
              navigate('/');
              setMobileMenuOpen(false);
            }}
            sx={{
              borderRadius: 2,
              color: 'rgba(255,255,255,0.75)',
              '&:hover': { color: primary, bgcolor: alpha(primary, 0.08) },
            }}
          >
            <ListItemText primary={t('nav.home')} />
          </ListItemButton>
          {!isAuthenticated && (
            <>
              <ListItemButton
                onClick={() => {
                  openAuthModal('login');
                  setMobileMenuOpen(false);
                }}
                sx={{
                  borderRadius: 2,
                  color: 'rgba(255,255,255,0.75)',
                  '&:hover': { color: primary, bgcolor: alpha(primary, 0.08) },
                }}
              >
                <ListItemText primary={t('login')} />
              </ListItemButton>
              <ListItemButton
                onClick={() => {
                  openAuthModal('register');
                  setMobileMenuOpen(false);
                }}
                sx={{
                  borderRadius: 2,
                  color: 'rgba(255,255,255,0.75)',
                  '&:hover': { color: primary, bgcolor: alpha(primary, 0.08) },
                }}
              >
                <ListItemText primary={t('register')} />
              </ListItemButton>
            </>
          )}
          {isAuthenticated && (
            <ListItemButton
              onClick={() => {
                navigate('/dashboard');
                setMobileMenuOpen(false);
              }}
              sx={{
                borderRadius: 2,
                color: 'rgba(255,255,255,0.75)',
                '&:hover': { color: primary, bgcolor: alpha(primary, 0.08) },
              }}
            >
              <ListItemText primary={t('dashboard')} />
            </ListItemButton>
          )}
        </List>
      </Drawer>

      {/* ─── SESSION EXPIRED ─── */}
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

      {/* ─── CONTENT ─── */}
      <Box sx={{ flex: 1, bgcolor: isDark ? 'background.default' : undefined }}>
        <Outlet />
      </Box>
    </Box>
  );
}
