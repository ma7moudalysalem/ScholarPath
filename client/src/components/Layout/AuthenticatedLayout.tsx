import { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
} from '@mui/icons-material';
import { useAuthStore, selectIsAdmin } from '@/stores/authStore';
import { useUiStore } from '@/stores/uiStore';
import { useAuth } from '@/hooks/useAuth';
import { UpgradeRequestStatus, UserRole } from '@/types';
import type { UpgradeRequestDto } from '@/types';
import { upgradeService } from '@/services/upgradeService';

const DRAWER_WIDTH = 260;

export function AuthenticatedLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const user = useAuthStore((s) => s.user);
  const isAdmin = useAuthStore(selectIsAdmin);
  const { sidebarOpen, toggleSidebar, toggleLanguage, themeMode, toggleTheme } = useUiStore();
  const { logout } = useAuth();

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [logoutDialogOpen, setLogoutDialogOpen] = useState(false);
  const [upgradeStatus, setUpgradeStatus] = useState<UpgradeRequestDto | null>(null);

  // Fetch upgrade status for non-admin users
  useEffect(() => {
    if (user && user.role !== UserRole.Admin) {
      upgradeService.getMyStatus().then(setUpgradeStatus).catch(() => {});
    }
  }, [user]);

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    setLogoutDialogOpen(false);
    handleProfileMenuClose();
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
    navItems.push({
      path: '/admin/upgrade-requests',
      label: t('nav.admin'),
      icon: <AdminIcon />,
    });
  }

  const getUpgradeBanner = () => {
    if (!upgradeStatus) return null;
    const { status } = upgradeStatus;

    const bannerConfig: Record<number, { severity: 'info' | 'success' | 'error' | 'warning'; message: string; action?: React.ReactNode }> = {
      [UpgradeRequestStatus.Pending]: {
        severity: 'info',
        message: t('upgrade.statusPending'),
      },
      [UpgradeRequestStatus.Approved]: {
        severity: 'success',
        message: t('upgrade.statusApproved'),
      },
      [UpgradeRequestStatus.Rejected]: {
        severity: 'error',
        message: t('upgrade.statusRejected'),
      },
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

    const config = bannerConfig[status];
    if (!config) return null;

    return (
      <Alert severity={config.severity} icon={<InfoIcon />} action={config.action} sx={{ borderRadius: 0 }}>
        {config.message}
      </Alert>
    );
  };

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ justifyContent: 'center' }}>
        <Typography variant="h6" fontWeight={700} color="primary">
          {t('appName')}
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ flex: 1, px: 1, pt: 1 }}>
        {navItems.map((item) => (
          <ListItemButton
            key={item.path}
            selected={location.pathname.startsWith(item.path)}
            onClick={() => handleNavigate(item.path)}
            sx={{ borderRadius: 2, mb: 0.5 }}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* Sidebar */}
      <Drawer
        variant={isMobile ? 'temporary' : 'persistent'}
        open={isMobile ? sidebarOpen : true}
        onClose={toggleSidebar}
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
        }}
      >
        {drawerContent}
      </Drawer>

      {/* Main content area */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Top bar */}
        <AppBar
          position="sticky"
          color="inherit"
          sx={{ ml: isMobile ? 0 : `${DRAWER_WIDTH}px`, width: isMobile ? '100%' : `calc(100% - ${DRAWER_WIDTH}px)` }}
        >
          <Toolbar>
            {isMobile && (
              <IconButton edge="start" onClick={toggleSidebar} sx={{ mr: 1 }} aria-label={t('nav.menu')}>
                <MenuIcon />
              </IconButton>
            )}

            <Box sx={{ flex: 1 }} />

            <IconButton onClick={toggleTheme} sx={{ mr: 1 }} aria-label={t('nav.toggleTheme')}>
              {themeMode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
            </IconButton>

            <IconButton onClick={toggleLanguage} sx={{ mr: 1 }} aria-label={t('nav.toggleLanguage')}>
              <TranslateIcon />
            </IconButton>

            <IconButton sx={{ mr: 1 }} onClick={() => navigate('/notifications')} aria-label={t('nav.notifications')}>
              <Badge badgeContent={0} showZero={false} color="error">
                <NotificationsIcon />
              </Badge>
            </IconButton>

            <IconButton onClick={handleProfileMenuOpen} aria-label={t('nav.profile')}>
              <Avatar
                src={user?.profileImageUrl ?? undefined}
                sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}
              >
                {user?.firstName?.[0]}
              </Avatar>
            </IconButton>

            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={handleProfileMenuClose}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
            >
              <MenuItem onClick={() => { handleProfileMenuClose(); navigate('/profile'); }}>
                <ListItemIcon><PersonIcon fontSize="small" /></ListItemIcon>
                {t('nav.profile')}
              </MenuItem>
              <Divider />
              <MenuItem onClick={() => { handleProfileMenuClose(); setLogoutDialogOpen(true); }}>
                <ListItemIcon><LogoutIcon fontSize="small" /></ListItemIcon>
                {t('logout')}
              </MenuItem>
            </Menu>
          </Toolbar>
        </AppBar>

        {/* Upgrade status banner */}
        <Box sx={{ ml: isMobile ? 0 : `${DRAWER_WIDTH}px`, width: isMobile ? '100%' : `calc(100% - ${DRAWER_WIDTH}px)` }}>
          {getUpgradeBanner()}
        </Box>

        {/* Page content */}
        <Box
          component="main"
          sx={{
            flex: 1,
            p: 3,
            ml: isMobile ? 0 : `${DRAWER_WIDTH}px`,
            width: isMobile ? '100%' : `calc(100% - ${DRAWER_WIDTH}px)`,
          }}
        >
          <Outlet />
        </Box>
      </Box>

      {/* Logout confirmation dialog */}
      <Dialog open={logoutDialogOpen} onClose={() => setLogoutDialogOpen(false)}>
        <DialogTitle>{t('auth.logoutConfirmTitle')}</DialogTitle>
        <DialogContent>
          <DialogContentText>{t('auth.logoutConfirmMessage')}</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setLogoutDialogOpen(false)}>{t('cancel')}</Button>
          <Button onClick={handleLogout} color="error" variant="contained">
            {t('logout')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
