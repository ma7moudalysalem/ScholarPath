import { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { alpha } from '@mui/material/styles';
import {
  Alert,
  AppBar,
  Avatar,
  Badge,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  School as SchoolIcon,
  People as PeopleIcon,
  Notifications as NotificationsIcon,
  Person as PersonIcon,
  AdminPanelSettings as AdminIcon,
  Logout as LogoutIcon,
  Translate as TranslateIcon,
  DarkMode as DarkModeIcon,
  LightMode as LightModeIcon,
  Info as InfoIcon,
  KeyboardArrowDown,
} from '@mui/icons-material';
import { useAuthStore, selectIsAdmin } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuth } from '@/hooks/useAuth';
import { UpgradeRequestStatus, UserRole } from '@/types';
import type { UpgradeRequestDto } from '@/types';
import { upgradeService } from '@/services/upgradeService';

const DRAWER_WIDTH = 264;

export function AuthenticatedLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const primary = theme.palette.primary.main;
  const displayFont = theme.typography.h2.fontFamily as string;

  const user = useAuthStore((s) => s.user);
  const isAdmin = useAuthStore(selectIsAdmin);
  const { sidebarOpen, toggleSidebar, toggleLanguage, themeMode, toggleTheme } = useUiStore();
  const { logout } = useAuth();

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [logoutDialogOpen, setLogoutDialogOpen] = useState(false);
  const [upgradeStatus, setUpgradeStatus] = useState<UpgradeRequestDto | null>(null);

  useEffect(() => {
    if (user && user.role !== UserRole.Admin) {
      upgradeService
        .getMyStatus()
        .then(setUpgradeStatus)
        .catch(() => {});
    }
  }, [user]);

  const handleLogout = async () => {
    setLogoutDialogOpen(false);
    setAnchorEl(null);
    await logout();
  };

  const handleNavigate = (path: string) => {
    navigate(path);
    if (isMobile) toggleSidebar();
  };

  const navItems = [
    { path: '/dashboard', label: t('nav.dashboard'), icon: <DashboardIcon /> },
    { path: '/scholarships', label: t('nav.scholarships'), icon: <SchoolIcon /> },
    { path: '/community', label: t('nav.community'), icon: <PeopleIcon /> },
    { path: '/notifications', label: t('nav.notifications'), icon: <NotificationsIcon /> },
  ];

  if (isAdmin) {
    navItems.push({ path: '/admin/upgrade-requests', label: t('nav.admin'), icon: <AdminIcon /> });
  }

  const getUpgradeBanner = () => {
    if (!upgradeStatus) return null;
    const bannerConfig: Record<
      number,
      {
        severity: 'info' | 'success' | 'error' | 'warning';
        message: string;
        action?: React.ReactNode;
      }
    > = {
      [UpgradeRequestStatus.Pending]: { severity: 'info', message: t('upgrade.statusPending') },
      [UpgradeRequestStatus.Approved]: {
        severity: 'success',
        message: t('upgrade.statusApproved'),
      },
      [UpgradeRequestStatus.Rejected]: { severity: 'error', message: t('upgrade.statusRejected') },
      [UpgradeRequestStatus.NeedsMoreInfo]: {
        severity: 'warning',
        message: t('upgrade.statusNeedsInfo'),
        action: (
          <Button color="inherit" size="small" onClick={() => navigate('/profile?tab=upgrade')}>
            {t('upgrade.reapply')}
          </Button>
        ),
      },
    };
    const config = bannerConfig[upgradeStatus.status];
    if (!config) return null;
    return (
      <Alert
        severity={config.severity}
        icon={<InfoIcon />}
        action={config.action}
        sx={{ borderRadius: 0 }}
      >
        {config.message}
      </Alert>
    );
  };

  /* ─── SIDEBAR CONTENT ─── */
  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Logo */}
      <Box
        onClick={() => navigate('/')}
        sx={{
          px: 3,
          py: 3,
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          borderBottom: `1px solid ${alpha(primary, 0.12)}`,
          cursor: 'pointer',
          transition: 'opacity 0.2s',
          '&:hover': { opacity: 0.8 },
        }}
      >
        <Box
          sx={{
            width: 32,
            height: 32,
            borderRadius: 2,
            background: `linear-gradient(135deg, ${primary}, ${alpha(primary, 0.55)})`,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0,
          }}
        >
          <SchoolIcon sx={{ fontSize: 18, color: theme.palette.primary.contrastText }} />
        </Box>
        <Typography
          sx={{
            fontFamily: displayFont,
            fontSize: '1.35rem',
            fontWeight: 600,
            color: primary,
            lineHeight: 1,
            letterSpacing: '-0.01em',
          }}
        >
          {t('appName')}
        </Typography>
      </Box>

      {/* Section label */}
      <Box sx={{ px: 3, pt: 3, pb: 1 }}>
        <Typography
          variant="caption"
          sx={{
            color: 'rgba(255,255,255,0.3)',
            letterSpacing: '0.12em',
            textTransform: 'uppercase',
            fontSize: '0.65rem',
            fontWeight: 600,
          }}
        >
          {t('nav.sidebarLabel')}
        </Typography>
      </Box>

      {/* Nav items */}
      <List sx={{ flex: 1, px: 1.5, pt: 0 }}>
        {navItems.map((item) => {
          const isActive = location.pathname.startsWith(item.path);
          return (
            <ListItemButton
              key={item.path}
              selected={isActive}
              onClick={() => handleNavigate(item.path)}
              sx={{
                borderRadius: 2,
                mb: 0.5,
                color: isActive ? primary : 'rgba(255,255,255,0.55)',
                '& .MuiListItemIcon-root': { color: isActive ? primary : 'rgba(255,255,255,0.4)' },
                '&:hover': {
                  color: alpha(primary, 0.9),
                  bgcolor: alpha(primary, 0.07),
                  '& .MuiListItemIcon-root': { color: alpha(primary, 0.8) },
                },
                '&.Mui-selected': {
                  bgcolor: alpha(primary, 0.1),
                  '&:hover': { bgcolor: alpha(primary, 0.15) },
                },
              }}
            >
              <ListItemIcon sx={{ minWidth: 38 }}>{item.icon}</ListItemIcon>
              <ListItemText
                primary={item.label}
                primaryTypographyProps={{ fontSize: '0.9rem', fontWeight: isActive ? 600 : 400 }}
              />
              {isActive && (
                <Box
                  sx={{ width: 5, height: 5, borderRadius: '50%', bgcolor: primary, flexShrink: 0 }}
                />
              )}
            </ListItemButton>
          );
        })}
      </List>

      {/* User card */}
      <Box
        sx={{
          p: 2,
          mx: 1.5,
          mb: 2,
          borderRadius: 2.5,
          border: `1px solid ${alpha(primary, 0.15)}`,
          bgcolor: alpha(primary, 0.05),
          cursor: 'pointer',
          transition: 'all 0.2s',
          '&:hover': { borderColor: alpha(primary, 0.3), bgcolor: alpha(primary, 0.1) },
        }}
        onClick={() => navigate('/profile')}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <Avatar
            src={user?.profileImageUrl ?? undefined}
            sx={{ width: 36, height: 36, fontSize: '0.875rem', flexShrink: 0 }}
          >
            {user?.firstName?.[0]}
          </Avatar>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography
              sx={{
                fontSize: '0.875rem',
                fontWeight: 600,
                color: 'rgba(255,255,255,0.9)',
                lineHeight: 1.2,
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {user?.firstName} {user?.lastName}
            </Typography>
            <Typography
              sx={{
                fontSize: '0.72rem',
                color: 'rgba(255,255,255,0.35)',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {user?.email}
            </Typography>
          </Box>
        </Box>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* ─── SIDEBAR ─── */}
      <Drawer
        variant={isMobile ? 'temporary' : 'persistent'}
        open={isMobile ? sidebarOpen : true}
        onClose={toggleSidebar}
        sx={{
          width: isMobile ? 0 : DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
        }}
      >
        {drawerContent}
      </Drawer>

      {/* ─── MAIN AREA ─── */}
      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        {/* Top bar — colors come from MuiAppBar theme override */}
        <AppBar
          position="sticky"
          sx={{
            width: '100%',
            backdropFilter: 'blur(20px)',
            WebkitBackdropFilter: 'blur(20px)',
            boxShadow: 'none',
          }}
        >
          <Toolbar sx={{ minHeight: 60 }}>
            {isMobile && (
              <IconButton
                edge="start"
                onClick={toggleSidebar}
                sx={{ mr: 1 }}
                aria-label={t('nav.menu')}
              >
                <MenuIcon />
              </IconButton>
            )}

            {isMobile && (
              <Typography
                onClick={() => navigate('/')}
                sx={{
                  fontFamily: displayFont,
                  fontSize: '1.3rem',
                  fontWeight: 600,
                  color: primary,
                  mr: 'auto',
                  cursor: 'pointer',
                  transition: 'opacity 0.2s',
                  '&:hover': { opacity: 0.8 },
                }}
              >
                {t('appName')}
              </Typography>
            )}

            <Box sx={{ flex: 1 }} />

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <IconButton
                onClick={toggleTheme}
                aria-label={t('nav.toggleTheme')}
                size="small"
                sx={{ opacity: 0.6, '&:hover': { opacity: 1 } }}
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
                size="small"
                sx={{ opacity: 0.6, '&:hover': { opacity: 1 }, mr: 0.5 }}
              >
                <TranslateIcon fontSize="small" />
              </IconButton>

              <IconButton
                size="small"
                onClick={() => navigate('/notifications')}
                aria-label={t('nav.notifications')}
                sx={{ mr: 0.5 }}
              >
                <Badge badgeContent={0} showZero={false} color="error">
                  <NotificationsIcon fontSize="small" />
                </Badge>
              </IconButton>

              {/* Profile button */}
              <Box
                onClick={(e) => setAnchorEl(e.currentTarget)}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                  pl: 1,
                  pr: 0.5,
                  py: 0.5,
                  borderRadius: 50,
                  cursor: 'pointer',
                  border: `1px solid ${alpha(primary, 0.18)}`,
                  transition: 'all 0.2s',
                  '&:hover': { borderColor: alpha(primary, 0.4), bgcolor: alpha(primary, 0.06) },
                }}
              >
                <Avatar
                  src={user?.profileImageUrl ?? undefined}
                  sx={{ width: 28, height: 28, fontSize: '0.75rem' }}
                >
                  {user?.firstName?.[0]}
                </Avatar>
                {!isMobile && (
                  <Typography
                    variant="body2"
                    sx={{
                      fontWeight: 500,
                      maxWidth: 100,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {user?.firstName}
                  </Typography>
                )}
                <KeyboardArrowDown sx={{ fontSize: 16, opacity: 0.5 }} />
              </Box>
            </Box>

            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={() => setAnchorEl(null)}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
              sx={{ mt: 1 }}
            >
              <MenuItem
                onClick={() => {
                  setAnchorEl(null);
                  navigate('/profile');
                }}
              >
                <ListItemIcon>
                  <PersonIcon fontSize="small" />
                </ListItemIcon>
                {t('nav.profile')}
              </MenuItem>
              <Divider sx={{ my: 0.5 }} />
              <MenuItem
                onClick={() => {
                  setAnchorEl(null);
                  setLogoutDialogOpen(true);
                }}
                sx={{ color: 'error.main' }}
              >
                <ListItemIcon>
                  <LogoutIcon fontSize="small" sx={{ color: 'error.main' }} />
                </ListItemIcon>
                {t('logout')}
              </MenuItem>
            </Menu>
          </Toolbar>
        </AppBar>

        {getUpgradeBanner()}

        <Box
          component="main"
          sx={{ flex: 1, p: { xs: 2.5, md: 3.5 }, bgcolor: 'background.default' }}
        >
          <Outlet />
        </Box>
      </Box>

      {/* Logout dialog */}
      <Dialog
        open={logoutDialogOpen}
        onClose={() => setLogoutDialogOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle sx={{ fontFamily: displayFont, fontSize: '1.5rem', fontWeight: 600 }}>
          {t('auth.logoutConfirmTitle')}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>{t('auth.logoutConfirmMessage')}</DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3, gap: 1 }}>
          <Button
            onClick={() => setLogoutDialogOpen(false)}
            variant="outlined"
            sx={{ borderRadius: 50 }}
          >
            {t('cancel')}
          </Button>
          <Button
            onClick={handleLogout}
            color="error"
            variant="contained"
            sx={{ borderRadius: 50 }}
          >
            {t('logout')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
