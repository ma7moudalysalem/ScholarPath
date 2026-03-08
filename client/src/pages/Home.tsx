import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  Button,
  Card,
  CardContent,
  Container,
  IconButton,
  Link,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import Grid from '@mui/material/Grid2';
import {
  School,
  People,
  TrendingUp,
  WorkspacePremium,
  ChevronLeft,
  ChevronRight,
  Email as EmailIcon,
  GitHub as GitHubIcon,
} from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { useAuthModal } from '@/hooks/useAuthModal';

export default function Home() {
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useAuthModal((s) => s.open);
  const [activeSlide, setActiveSlide] = useState(0);

  const slides = [
    { title: t('home.slide1Title'), subtitle: t('home.slide1Subtitle') },
    { title: t('home.slide2Title'), subtitle: t('home.slide2Subtitle') },
    { title: t('home.slide3Title'), subtitle: t('home.slide3Subtitle') },
  ];

  useEffect(() => {
    const timer = setInterval(() => {
      setActiveSlide((prev) => (prev + 1) % slides.length);
    }, 5000);
    return () => clearInterval(timer);
  }, [slides.length]);

  const services = [
    {
      icon: <School sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('scholarships'),
      description: t('home.featureScholarships'),
    },
    {
      icon: <People sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('community'),
      description: t('home.featureCommunity'),
    },
    {
      icon: <TrendingUp sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('home.mentorship'),
      description: t('home.featureMentorship'),
    },
    {
      icon: <WorkspacePremium sx={{ fontSize: 48, color: 'primary.main' }} />,
      title: t('home.opportunities'),
      description: t('home.featureOpportunities'),
    },
  ];

  const testimonials = [
    { name: t('home.testimonial1Name'), text: t('home.testimonial1Text') },
    { name: t('home.testimonial2Name'), text: t('home.testimonial2Text') },
    { name: t('home.testimonial3Name'), text: t('home.testimonial3Text') },
  ];

  const handleCTA = () => {
    if (isAuthenticated) return;
    openAuthModal('register');
  };

  return (
    <Box>
      {/* Section 1: Intro Slider */}
      <Box
        sx={{
          bgcolor: 'primary.main',
          color: 'primary.contrastText',
          py: { xs: 8, md: 12 },
          position: 'relative',
        }}
      >
        <Container maxWidth="md" sx={{ textAlign: 'center' }}>
          <Typography variant="h2" fontWeight={700} gutterBottom>
            ScholarPath
          </Typography>
          <Typography variant="h3" fontWeight={700} gutterBottom>
            {slides[activeSlide]?.title}
          </Typography>
          <Typography variant="h6" sx={{ mb: 4, opacity: 0.9 }}>
            {slides[activeSlide]?.subtitle}
          </Typography>

          {!isAuthenticated && (
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button
                variant="contained"
                color="secondary"
                size="large"
                onClick={handleCTA}
              >
                {t('home.getStarted')}
              </Button>
              <Button
                variant="outlined"
                size="large"
                sx={{ color: 'white', borderColor: 'white' }}
                onClick={() => openAuthModal('login')}
              >
                {t('login')}
              </Button>
            </Stack>
          )}

          {/* Slider controls */}
          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 1 }}>
            <IconButton
              size="small"
              sx={{ color: 'white' }}
              onClick={() => setActiveSlide((prev) => (prev - 1 + slides.length) % slides.length)}
            >
              <ChevronLeft />
            </IconButton>
            {slides.map((_, i) => (
              <Box
                key={i}
                sx={{
                  width: 10,
                  height: 10,
                  borderRadius: '50%',
                  bgcolor: i === activeSlide ? 'white' : 'rgba(255,255,255,0.4)',
                  cursor: 'pointer',
                }}
                onClick={() => setActiveSlide(i)}
              />
            ))}
            <IconButton
              size="small"
              sx={{ color: 'white' }}
              onClick={() => setActiveSlide((prev) => (prev + 1) % slides.length)}
            >
              <ChevronRight />
            </IconButton>
          </Box>
        </Container>
      </Box>

      {/* Section 2: Services Cards */}
      <Container maxWidth="lg" sx={{ py: 8 }}>
        <Typography variant="h4" fontWeight={700} textAlign="center" gutterBottom>
          {t('home.servicesTitle')}
        </Typography>
        <Typography variant="body1" color="text.secondary" textAlign="center" sx={{ mb: 4 }}>
          {t('home.servicesSubtitle')}
        </Typography>
        <Grid container spacing={3}>
          {services.map((service, index) => (
            <Grid key={index} size={{ xs: 12, sm: 6, md: 3 }}>
              <Paper
                sx={{
                  p: 3,
                  textAlign: 'center',
                  height: '100%',
                  transition: 'transform 0.2s',
                  '&:hover': { transform: 'translateY(-4px)', boxShadow: 4 },
                }}
              >
                {service.icon}
                <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
                  {service.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {service.description}
                </Typography>
              </Paper>
            </Grid>
          ))}
        </Grid>
      </Container>

      {/* Section 3: Trust / Testimonials */}
      <Box sx={{ bgcolor: 'background.paper', py: 8 }}>
        <Container maxWidth="lg">
          <Typography variant="h4" fontWeight={700} textAlign="center" gutterBottom>
            {t('home.trustTitle')}
          </Typography>
          <Grid container spacing={3} sx={{ mt: 2 }}>
            {testimonials.map((item, index) => (
              <Grid key={index} size={{ xs: 12, md: 4 }}>
                <Card sx={{ height: '100%' }}>
                  <CardContent>
                    <Typography variant="body1" sx={{ fontStyle: 'italic', mb: 2 }}>
                      &ldquo;{item.text}&rdquo;
                    </Typography>
                    <Typography variant="subtitle2" fontWeight={600}>
                      — {item.name}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Container>
      </Box>

      {/* Section 4: Footer */}
      <Box
        component="footer"
        sx={{
          bgcolor: 'grey.900',
          color: 'grey.300',
          py: 6,
        }}
      >
        <Container maxWidth="lg">
          <Grid container spacing={4}>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="h6" fontWeight={700} color="white" gutterBottom>
                {t('appName')}
              </Typography>
              <Typography variant="body2">
                {t('home.footerDesc')}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="subtitle1" fontWeight={600} color="white" gutterBottom>
                {t('home.quickLinks')}
              </Typography>
              <Stack spacing={0.5}>
                <Link href="/" color="inherit" underline="hover" variant="body2">
                  {t('nav.home')}
                </Link>
                <Link href="/scholarships" color="inherit" underline="hover" variant="body2">
                  {t('nav.scholarships')}
                </Link>
                <Link href="/community" color="inherit" underline="hover" variant="body2">
                  {t('nav.community')}
                </Link>
              </Stack>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="subtitle1" fontWeight={600} color="white" gutterBottom>
                {t('home.contactUs')}
              </Typography>
              <Stack direction="row" spacing={1}>
                <IconButton size="small" sx={{ color: 'grey.300' }}>
                  <EmailIcon />
                </IconButton>
                <IconButton size="small" sx={{ color: 'grey.300' }}>
                  <GitHubIcon />
                </IconButton>
              </Stack>
            </Grid>
          </Grid>
          <Typography variant="body2" textAlign="center" sx={{ mt: 4, pt: 2, borderTop: 1, borderColor: 'grey.700' }}>
            &copy; {new Date().getFullYear()} {t('appName')}. {t('allRightsReserved')}
          </Typography>
        </Container>
      </Box>
    </Box>
  );
}
