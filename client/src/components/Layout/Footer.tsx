import { Box, Container, Typography, Stack, IconButton, Divider } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { useTranslation } from 'react-i18next';
import { Facebook, Twitter, LinkedIn, Instagram } from '@mui/icons-material';
import { Link as RouterLink } from 'react-router-dom';

export function Footer() {
    const { t } = useTranslation();

    const socialLinks = [
        { icon: <Facebook />, label: 'Facebook', url: '#' },
        { icon: <Twitter />, label: 'Twitter', url: '#' },
        { icon: <LinkedIn />, label: 'LinkedIn', url: '#' },
        { icon: <Instagram />, label: 'Instagram', url: '#' },
    ];

    const quickLinks = [
        { label: t('Home', 'Home'), url: '/' },
        { label: t('about', 'About Us'), url: '/about' },
        { label: t('scholarships', 'Scholarships'), url: '/scholarships' },
        { label: t('contact', 'Contact'), url: '/contact' },
    ];

    const policyLinks = [
        { label: t('terms', 'Terms of Service'), url: '/terms' },
        { label: t('privacy', 'Privacy Policy'), url: '/privacy' },
    ];

    if (!quickLinks || !policyLinks) {
        return (
            <Box component="footer" sx={{ py: 3, px: 2, mt: 'auto', backgroundColor: (theme) => theme.palette.mode === 'dark' ? 'background.paper' : 'grey.900', color: 'common.white', textAlign: 'center' }}>
                <Typography variant="body2">
                    &copy; {new Date().getFullYear()} ScholarPath. All rights reserved.
                </Typography>
            </Box>
        );
    }

    return (
        <Box
            component="footer"
            sx={{
                bgcolor: 'primary.main',
                color: 'white',
                pt: { xs: 6, md: 8 },
                pb: 4,
                mt: 'auto',
                borderTop: (theme) => theme.palette.mode === 'dark' ? '1px solid' : 'none',
                borderColor: 'divider',
            }}
        >
            <Container maxWidth="lg">
                <Grid container spacing={{ xs: 4, md: 8 }}>
                    {/* Brand & Social */}
                    <Grid size={{ xs: 12, md: 4 }}>
                        <Box sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 1.5 }}>
                            <img
                                src="/logo.svg"
                                alt={t('appName', 'ScholarPath')}
                                style={{ height: '40px', objectFit: 'contain', filter: 'brightness(0) invert(1)' }}
                            />
                            <Typography variant="h6" fontWeight={700} color="inherit">
                                {t('appName', 'ScholarPath')}
                            </Typography>
                        </Box>
                        <Typography variant="body2" sx={{ mb: 3, maxWidth: '300px', opacity: 0.9 }}>
                            {t('footer.description', 'Empowering students globally with AI-driven scholarship discovery, mentorship, and community support.')}
                        </Typography>
                        <Stack direction="row" spacing={1}>
                            {socialLinks.map((social, index) => (
                                <IconButton key={index} aria-label={social.label} size="small" sx={{ color: 'white', '&:hover': { color: 'grey.300' } }}>
                                    {social.icon}
                                </IconButton>
                            ))}
                        </Stack>
                    </Grid>

                    {/* Quick Links */}
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                            {t('footer.quickLinks', 'Quick Links')}
                        </Typography>
                        <Stack spacing={1.5}>
                            {quickLinks.map((link, index) => (
                                <Typography
                                    key={index}
                                    component={RouterLink}
                                    to={link.url}
                                    variant="body2"
                                    color="inherit"
                                    sx={{ textDecoration: 'none', opacity: 0.9, '&:hover': { opacity: 1 } }}
                                >
                                    {link.label}
                                </Typography>
                            ))}
                        </Stack>
                    </Grid>

                    {/* Contact & Policy */}
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                            {t('footer.legal', 'Legal & Contact')}
                        </Typography>
                        <Stack spacing={1.5}>
                            <Typography variant="body2" sx={{ opacity: 0.9 }}>
                                {t('footer.email', 'Email: support@scholarpath.com')}
                            </Typography>
                            <Typography variant="body2" sx={{ opacity: 0.9 }}>
                                {t('footer.phone', 'Phone: +1 234 567 890')}
                            </Typography>
                            <Box sx={{ mt: '16px !important' }}>
                                {policyLinks.map((link, index) => (
                                    <Typography
                                        key={index}
                                        component={RouterLink}
                                        to={link.url}
                                        variant="body2"
                                        color="inherit"
                                        sx={{ textDecoration: 'none', display: 'block', mb: 1, opacity: 0.9, '&:hover': { opacity: 1 } }}
                                    >
                                        {link.label}
                                    </Typography>
                                ))}
                            </Box>
                        </Stack>
                    </Grid>
                </Grid>

                <Divider sx={{ my: 4, borderColor: 'rgba(255,255,255,0.2)' }} />

                {/* Copyright */}
                <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                    <Typography variant="body2" sx={{ opacity: 0.9 }}>
                        &copy; {new Date().getFullYear()} {t('appName', 'ScholarPath')}. {t('allRightsReserved', 'All rights reserved.')}
                    </Typography>
                </Box>
            </Container>
        </Box>
    );
}
