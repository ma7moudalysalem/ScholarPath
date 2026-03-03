import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
    AppBar,
    Box,
    Button,
    Container,
    Toolbar,
    Typography,
    IconButton,
    Avatar,
    Menu,
    MenuItem,
    Divider,
    Drawer,
    List,
    ListItem,
    ListItemButton,
    ListItemText,
} from '@mui/material';
import { Translate as TranslateIcon, Menu as MenuIcon, Dashboard as DashboardIcon, Person as PersonIcon, Logout as LogoutIcon, DarkMode as DarkModeIcon, LightMode as LightModeIcon } from '@mui/icons-material';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types';
import { LogoutConfirmationDialog } from '@/components/common/LogoutConfirmationDialog';

export function Header() {
    const { t, i18n } = useTranslation();
    const navigate = useNavigate();
    const location = useLocation();
    const { toggleLanguage, themeMode, toggleTheme } = useUiStore();
    const { isAuthenticated, user, logout } = useAuthStore();

    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [mobileOpen, setMobileOpen] = useState(false);
    const [logoutDialogOpen, setLogoutDialogOpen] = useState(false);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    const handleDrawerToggle = () => {
        setMobileOpen(!mobileOpen);
    };

    const handleLogoutClick = () => {
        handleMenuClose();
        setLogoutDialogOpen(true);
    };

    const handleConfirmLogout = () => {
        logout();
        setLogoutDialogOpen(false);
        navigate('/login');
    };

    const getDashboardRoute = () => {
        if (!user) return '/dashboard';
        switch (user.role) {
            case UserRole.Admin: return '/admin';
            case UserRole.Consultant: return '/consultant';
            case UserRole.Student: return '/student-dashboard'; // Fallback to normal dashboard if no specific route
            case UserRole.Company: return '/company-dashboard';
            default: return '/dashboard';
        }
    };

    const navLinks = [
        { label: t('scholarships', 'Scholarships'), path: '/scholarships' },
        { label: t('community', 'Community'), path: '/community' },
    ];

    const renderLogo = () => (
        <Box
            onClick={() => navigate('/')}
            sx={{ cursor: 'pointer', display: 'flex', alignItems: 'center', flexGrow: { xs: 1, md: 0 } }}
        >
            <img
                src="/logo.svg"
                alt={t('appName', 'ScholarPath')}
                style={{ height: '32px', objectFit: 'contain', marginRight: '8px' }}
            />
            <Typography variant="h6" fontWeight={700} color="primary">
                {t('appName', 'ScholarPath')}
            </Typography>
        </Box>
    );

    const renderAuthButtons = () => {
        if (isAuthenticated && user) {
            return (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <IconButton onClick={toggleTheme} color="inherit" aria-label={t('nav.toggleTheme', 'Toggle theme')}>
                        {themeMode === 'dark' ? <LightModeIcon /> : <DarkModeIcon />}
                    </IconButton>
                    <Button
                        color="inherit"
                        onClick={toggleLanguage}
                        startIcon={<TranslateIcon sx={{ fontSize: '1.2rem !important' }} />}
                        sx={{
                            fontWeight: 600,
                            textTransform: 'none',
                            borderRadius: '50px',
                            px: 2,
                            py: 0.5,
                            color: 'text.secondary',
                            display: { xs: 'none', sm: 'flex' }
                        }}
                        aria-label={t('nav.toggleLanguage')}
                    >
                        {i18n.language === 'en' ? 'عربي' : 'English'}
                    </Button>
                    <IconButton onClick={handleMenuOpen} sx={{ p: 0, ml: 1 }}>
                        <Avatar alt={user.firstName} src={user.profileImageUrl || ''} sx={{ bgcolor: 'primary.main' }}>
                            {user.firstName?.charAt(0) || 'U'}
                        </Avatar>
                    </IconButton>

                    <Menu
                        anchorEl={anchorEl}
                        open={Boolean(anchorEl)}
                        onClose={handleMenuClose}
                        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                    >
                        <Box sx={{ px: 2, py: 1 }}>
                            <Typography variant="subtitle2" fontWeight={700}>{user.firstName} {user.lastName}</Typography>
                            <Typography variant="body2" color="text.secondary">{user.email}</Typography>
                        </Box>
                        <Divider />
                        <MenuItem onClick={() => { navigate(getDashboardRoute()); handleMenuClose(); }}>
                            <DashboardIcon sx={{ mr: 2, fontSize: '1.2rem' }} /> {t('dashboard', 'Dashboard')}
                        </MenuItem>
                        <MenuItem onClick={() => { navigate('/profile'); handleMenuClose(); }}>
                            <PersonIcon sx={{ mr: 2, fontSize: '1.2rem' }} /> {t('profile', 'Profile')}
                        </MenuItem>
                        <Divider />
                        <MenuItem onClick={handleLogoutClick} sx={{ color: 'error.main' }}>
                            <LogoutIcon sx={{ mr: 2, fontSize: '1.2rem' }} /> {t('logout', 'Logout')}
                        </MenuItem>
                    </Menu>
                </Box>
            );
        }

        return (
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                <IconButton onClick={toggleTheme} color="inherit" aria-label={t('nav.toggleTheme', 'Toggle theme')}>
                    {themeMode === 'dark' ? <LightModeIcon /> : <DarkModeIcon />}
                </IconButton>
                <Button
                    onClick={toggleLanguage}
                    startIcon={<TranslateIcon sx={{ fontSize: '1.2rem !important' }} />}
                    sx={{
                        mr: { xs: 0, sm: 2 },
                        fontWeight: 600,
                        textTransform: 'none',
                        borderRadius: '50px',
                        px: { xs: 1, sm: 2 },
                        py: 0.5,
                        color: 'text.secondary',
                        bgcolor: 'background.paper',
                        border: '1px solid',
                        borderColor: 'divider',
                        transition: 'all 0.2s',
                        '&:hover': {
                            bgcolor: 'primary.main',
                            color: 'primary.contrastText',
                            borderColor: 'primary.main',
                        },
                    }}
                    aria-label={t('nav.toggleLanguage')}
                >
                    {i18n.language === 'en' ? 'عربي' : 'English'}
                </Button>
                <Box sx={{ display: { xs: 'none', sm: 'flex' }, gap: 1 }}>
                    <Button variant="outlined" onClick={() => navigate('/login')}>
                        {t('login', 'Login')}
                    </Button>
                    <Button variant="contained" onClick={() => navigate('/register')}>
                        {t('register', 'Register')}
                    </Button>
                </Box>
            </Box>
        );
    };

    const mobileDrawer = (
        <Box onClick={handleDrawerToggle} sx={{ textAlign: 'center', display: 'flex', flexDirection: 'column', height: '100%' }}>
            <Box sx={{ my: 2, display: 'flex', justifyContent: 'center' }}>
                {renderLogo()}
            </Box>
            <Divider />
            <List sx={{ flexGrow: 1 }}>
                {navLinks.map((item) => (
                    <ListItem key={item.path} disablePadding>
                        <ListItemButton sx={{ textAlign: 'center' }} onClick={() => navigate(item.path)}>
                            <ListItemText
                                primary={item.label}
                                primaryTypographyProps={{
                                    fontWeight: location.pathname === item.path ? 700 : 400,
                                    color: location.pathname === item.path ? 'primary.main' : 'text.primary'
                                }}
                            />
                        </ListItemButton>
                    </ListItem>
                ))}
            </List>
            <Divider />
            {!isAuthenticated && (
                <Box sx={{ p: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
                    <Button variant="outlined" fullWidth onClick={() => navigate('/login')}>
                        {t('login', 'Login')}
                    </Button>
                    <Button variant="contained" fullWidth onClick={() => navigate('/register')}>
                        {t('register', 'Register')}
                    </Button>
                </Box>
            )}
        </Box>
    );

    return (
        <AppBar position="sticky" color="inherit" elevation={0} sx={{ borderBottom: '1px solid', borderColor: 'divider' }}>
            <Container maxWidth="lg">
                <Toolbar disableGutters sx={{ justifyContent: 'space-between' }}>

                    {/* Mobile Menu Icon */}
                    <IconButton
                        color="inherit"
                        aria-label="open drawer"
                        edge="start"
                        onClick={handleDrawerToggle}
                        sx={{ mr: 2, display: { md: 'none' } }}
                    >
                        <MenuIcon />
                    </IconButton>

                    {/* Logo */}
                    {renderLogo()}

                    {/* Desktop Navigation Links */}
                    <Box sx={{ display: { xs: 'none', md: 'flex' }, ml: 4, gap: 3 }}>
                        {navLinks.map((link) => (
                            <Button
                                key={link.path}
                                onClick={() => navigate(link.path)}
                                sx={{
                                    color: location.pathname === link.path ? 'primary.main' : 'text.secondary',
                                    fontWeight: location.pathname === link.path ? 700 : 500,
                                    textTransform: 'none',
                                    fontSize: '1rem',
                                    '&:hover': { color: 'primary.main', bgcolor: 'transparent' }
                                }}
                            >
                                {link.label}
                            </Button>
                        ))}
                    </Box>

                    <Box sx={{ flexGrow: 1, display: { xs: 'none', md: 'flex' } }} />

                    {/* Right Side: Auth / Language */}
                    {renderAuthButtons()}
                </Toolbar>
            </Container>

            {/* Mobile Navigation Drawer */}
            <Drawer
                variant="temporary"
                open={mobileOpen}
                onClose={handleDrawerToggle}
                ModalProps={{ keepMounted: true }}
                sx={{
                    display: { xs: 'block', md: 'none' },
                    '& .MuiDrawer-paper': { boxSizing: 'border-box', width: 240 },
                }}
            >
                {mobileDrawer}
            </Drawer>

            <LogoutConfirmationDialog
                open={logoutDialogOpen}
                onClose={() => setLogoutDialogOpen(false)}
                onConfirm={handleConfirmLogout}
            />
        </AppBar>
    );
}
