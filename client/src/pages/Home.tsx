import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { alpha } from '@mui/material/styles';
import {
  Box,
  Button,
  Container,
  IconButton,
  Stack,
  Typography,
  useTheme,
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
  ArrowForward,
  KeyboardArrowDown,
} from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { useAuthModal } from '@/hooks/useAuthModal';

export default function Home() {
  const { t } = useTranslation();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const isRTL = theme.direction === 'rtl';
  // Use the theme's heading font (direction-aware: Cormorant Garamond for LTR, Cairo for RTL)
  const displayFont = theme.typography.h1.fontFamily as string;

  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useAuthModal((s) => s.open);
  const [activeSlide, setActiveSlide] = useState(0);

  const slides = [
    { title: t('home.slide1Title'), subtitle: t('home.slide1Subtitle') },
    { title: t('home.slide2Title'), subtitle: t('home.slide2Subtitle') },
    { title: t('home.slide3Title'), subtitle: t('home.slide3Subtitle') },
  ];

  useEffect(() => {
    const timer = setInterval(() => setActiveSlide((p) => (p + 1) % slides.length), 5000);
    return () => clearInterval(timer);
  }, [slides.length]);

  const features = [
    { icon: <School />, title: t('scholarships'), desc: t('home.featureScholarships') },
    { icon: <People />, title: t('community'), desc: t('home.featureCommunity') },
    { icon: <TrendingUp />, title: t('home.mentorship'), desc: t('home.featureMentorship') },
    { icon: <WorkspacePremium />, title: t('home.opportunities'), desc: t('home.featureOpportunities') },
  ];

  const testimonials = [
    { name: t('home.testimonial1Name'), text: t('home.testimonial1Text') },
    { name: t('home.testimonial2Name'), text: t('home.testimonial2Text') },
    { name: t('home.testimonial3Name'), text: t('home.testimonial3Text') },
  ];

  const stats = [
    { value: '10K+', label: t('home.statsScholarships') },
    { value: '50+',  label: t('home.statsCountries') },
    { value: '95%',  label: t('home.statsSuccess') },
    { value: isRTL ? '١٠٠٪' : 'Free', label: t('home.statsFree') },
  ];

  const steps = [
    { num: '01', title: t('home.stepTitle1'), desc: t('home.stepDesc1') },
    { num: '02', title: t('home.stepTitle2'), desc: t('home.stepDesc2') },
    { num: '03', title: t('home.stepTitle3'), desc: t('home.stepDesc3') },
  ];

  const primary = theme.palette.primary.main;
  const textPrimary = isDark ? '#EDE8DF' : '#0F1628';
  const textSecondary = isDark ? '#7D8FA8' : '#5A6878';
  const bgDefault = isDark ? '#070C18' : '#F8F5EF';
  const bgPaper = isDark ? '#0E1524' : '#FFFFFF';

  return (
    <Box>
      {/* ─── HERO ─── */}
      <Box
        sx={{
          position: 'relative',
          minHeight: '100vh',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          overflow: 'hidden',
          bgcolor: bgDefault,
          backgroundImage: `radial-gradient(circle, ${alpha(primary, 0.065)} 1px, transparent 1px)`,
          backgroundSize: '30px 30px',
        }}
      >
        {/* Atmospheric orbs */}
        <Box sx={{
          position: 'absolute', top: '-15%', right: isRTL ? 'auto' : '-8%', left: isRTL ? '-8%' : 'auto',
          width: '55vw', height: '55vw', maxWidth: 720, maxHeight: 720,
          borderRadius: '50%',
          background: `radial-gradient(circle, ${alpha(primary, 0.09)} 0%, transparent 65%)`,
          pointerEvents: 'none',
        }} />
        <Box sx={{
          position: 'absolute', bottom: '-20%', left: isRTL ? 'auto' : '-8%', right: isRTL ? '-8%' : 'auto',
          width: '48vw', height: '48vw', maxWidth: 600, maxHeight: 600,
          borderRadius: '50%',
          background: isDark
            ? 'radial-gradient(circle, rgba(79,126,245,0.07) 0%, transparent 65%)'
            : 'radial-gradient(circle, rgba(45,91,227,0.06) 0%, transparent 65%)',
          pointerEvents: 'none',
        }} />

        {/* Top gold accent line */}
        <Box sx={{
          position: 'absolute', top: 0, left: 0, right: 0, height: 2,
          background: `linear-gradient(90deg, transparent 0%, ${primary} 40%, ${primary} 60%, transparent 100%)`,
        }} />

        <Container maxWidth="lg" sx={{ position: 'relative', zIndex: 1, py: { xs: 10, md: 14 } }}>
          <Grid container spacing={{ xs: 6, md: 8 }} alignItems="center">
            {/* Left: text */}
            <Grid size={{ xs: 12, md: 7 }}>
              {/* Eyebrow */}
              <Box sx={{
                display: 'inline-flex', alignItems: 'center', gap: 1.5,
                mb: 3, px: 2, py: 0.75,
                border: `1px solid ${alpha(primary, 0.28)}`,
                borderRadius: 50,
                '@keyframes sp-fadedown': {
                  from: { opacity: 0, transform: 'translateY(-10px)' },
                  to: { opacity: 1, transform: 'translateY(0)' },
                },
                animation: 'sp-fadedown 0.7s ease both',
              }}>
                <Box sx={{
                  width: 6, height: 6, borderRadius: '50%', bgcolor: primary,
                  '@keyframes sp-pulse': {
                    '0%, 100%': { opacity: 1 }, '50%': { opacity: 0.4 },
                  },
                  animation: 'sp-pulse 2s ease-in-out infinite',
                }} />
                <Typography variant="caption" sx={{
                  color: primary, fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase',
                }}>
                  {t('home.eyebrowHero')}
                </Typography>
              </Box>

              {/* Main headline */}
              <Box sx={{
                '@keyframes sp-fadein': {
                  from: { opacity: 0, transform: 'translateY(32px)' },
                  to: { opacity: 1, transform: 'translateY(0)' },
                },
                animation: 'sp-fadein 0.9s ease both',
                animationDelay: '0.1s',
              }}>
                <Typography sx={{
                  fontFamily: displayFont,
                  fontSize: { xs: '2.6rem', sm: '3.4rem', md: '4.5rem', lg: '5.2rem' },
                  fontWeight: 600,
                  lineHeight: 1.06,
                  color: primary,
                  mb: 0.5,
                }}>
                  {slides[activeSlide]?.title}
                </Typography>
                <Typography sx={{
                  fontFamily: displayFont,
                  fontSize: { xs: '2.6rem', sm: '3.4rem', md: '4.5rem', lg: '5.2rem' },
                  fontWeight: 600,
                  lineHeight: 1.06,
                  color: textPrimary,
                  mb: 3,
                }}>
                  {t('appName')}
                </Typography>
              </Box>

              {/* Subtitle */}
              <Box sx={{
                '@keyframes sp-fadein2': {
                  from: { opacity: 0, transform: 'translateY(20px)' },
                  to: { opacity: 1, transform: 'translateY(0)' },
                },
                animation: 'sp-fadein2 0.9s ease both',
                animationDelay: '0.28s',
              }}>
                <Typography variant="h6" sx={{
                  color: textSecondary, fontWeight: 400, lineHeight: 1.7,
                  mb: 4.5, maxWidth: 520, fontFamily: 'inherit',
                }}>
                  {slides[activeSlide]?.subtitle}
                </Typography>
              </Box>

              {/* CTAs */}
              <Box sx={{
                '@keyframes sp-fadein3': {
                  from: { opacity: 0, transform: 'translateY(16px)' },
                  to: { opacity: 1, transform: 'translateY(0)' },
                },
                animation: 'sp-fadein3 0.9s ease both',
                animationDelay: '0.45s',
              }}>
                {!isAuthenticated && (
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 5 }}>
                    <Button
                      variant="contained"
                      size="large"
                      endIcon={<ArrowForward />}
                      onClick={() => openAuthModal('register')}
                      sx={{ borderRadius: 50, px: 4 }}
                    >
                      {t('home.getStarted')}
                    </Button>
                    <Button
                      variant="outlined"
                      size="large"
                      onClick={() => openAuthModal('login')}
                      sx={{ borderRadius: 50, px: 4 }}
                    >
                      {t('login')}
                    </Button>
                  </Stack>
                )}
              </Box>

              {/* Slider controls */}
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <IconButton
                  size="small"
                  onClick={() => setActiveSlide((p) => (p - 1 + slides.length) % slides.length)}
                  sx={{
                    color: primary,
                    border: `1px solid ${alpha(primary, 0.28)}`,
                    '&:hover': { bgcolor: alpha(primary, 0.1) },
                  }}
                >
                  {isRTL ? <ChevronRight fontSize="small" /> : <ChevronLeft fontSize="small" />}
                </IconButton>
                {slides.map((_, i) => (
                  <Box
                    key={i}
                    onClick={() => setActiveSlide(i)}
                    sx={{
                      width: i === activeSlide ? 26 : 8, height: 8,
                      borderRadius: 4,
                      bgcolor: i === activeSlide ? primary : alpha(primary, 0.22),
                      cursor: 'pointer',
                      transition: 'all 0.35s ease',
                    }}
                  />
                ))}
                <IconButton
                  size="small"
                  onClick={() => setActiveSlide((p) => (p + 1) % slides.length)}
                  sx={{
                    color: primary,
                    border: `1px solid ${alpha(primary, 0.28)}`,
                    '&:hover': { bgcolor: alpha(primary, 0.1) },
                  }}
                >
                  {isRTL ? <ChevronLeft fontSize="small" /> : <ChevronRight fontSize="small" />}
                </IconButton>
              </Box>
            </Grid>

            {/* Right: stats card */}
            <Grid size={{ xs: 12, md: 5 }} sx={{ display: { xs: 'none', md: 'block' } }}>
              <Box sx={{
                borderRadius: 4, p: 4,
                border: `1px solid ${alpha(primary, 0.14)}`,
                background: isDark
                  ? 'linear-gradient(145deg, rgba(14,21,36,0.92), rgba(19,30,53,0.85))'
                  : 'linear-gradient(145deg, rgba(255,255,255,0.96), rgba(240,236,227,0.9))',
                backdropFilter: 'blur(24px)',
                '@keyframes sp-float': {
                  '0%, 100%': { transform: 'translateY(0px)' },
                  '50%': { transform: 'translateY(-14px)' },
                },
                animation: 'sp-float 7s ease-in-out infinite',
                boxShadow: isDark
                  ? `0 24px 64px rgba(0,0,0,0.55), 0 0 0 1px ${alpha(primary, 0.08)}`
                  : '0 24px 64px rgba(0,0,0,0.08)',
              }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 4 }}>
                  <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: '#10B981' }} />
                  <Typography variant="overline" sx={{ color: primary, letterSpacing: '0.14em' }}>
                    {t('home.liveStats')}
                  </Typography>
                </Box>
                {stats.map((stat, i) => (
                  <Box
                    key={stat.label}
                    sx={{
                      mb: i < stats.length - 1 ? 3 : 0,
                      pb: i < stats.length - 1 ? 3 : 0,
                      borderBottom: i < stats.length - 1
                        ? `1px solid ${alpha(primary, 0.1)}`
                        : 'none',
                      display: 'flex',
                      alignItems: 'baseline',
                      justifyContent: 'space-between',
                    }}
                  >
                    <Typography sx={{
                      fontFamily: displayFont,
                      fontSize: '2.6rem',
                      fontWeight: 700,
                      color: primary,
                      lineHeight: 1,
                    }}>
                      {stat.value}
                    </Typography>
                    <Typography variant="body2" sx={{ color: textSecondary }}>
                      {stat.label}
                    </Typography>
                  </Box>
                ))}
              </Box>
            </Grid>
          </Grid>
        </Container>

        {/* Scroll indicator */}
        <Box sx={{
          position: 'absolute', bottom: 28, left: '50%', transform: 'translateX(-50%)',
          display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 0.5,
          color: textSecondary,
          '@keyframes sp-bounce': {
            '0%, 100%': { transform: 'translateX(-50%) translateY(0)' },
            '50%': { transform: 'translateX(-50%) translateY(7px)' },
          },
          animation: 'sp-bounce 2.2s ease-in-out infinite',
        }}>
          <Typography variant="caption" sx={{ letterSpacing: '0.1em', textTransform: 'uppercase', fontSize: '0.65rem' }}>
            {t('home.scrollDown')}
          </Typography>
          <KeyboardArrowDown fontSize="small" />
        </Box>
      </Box>

      {/* ─── FEATURES ─── */}
      <Box sx={{ py: { xs: 9, md: 14 }, bgcolor: bgPaper }}>
        <Container maxWidth="lg">
          <Box sx={{ textAlign: 'center', mb: { xs: 6, md: 10 } }}>
            <Typography variant="overline" sx={{ color: primary, letterSpacing: '0.15em', mb: 1.5, display: 'block' }}>
              {t('home.servicesEyebrow')}
            </Typography>
            <Typography variant="h2" sx={{ color: textPrimary, mb: 2 }}>
              {t('home.servicesTitle')}
            </Typography>
            <Typography variant="body1" sx={{ color: textSecondary, maxWidth: 480, mx: 'auto', lineHeight: 1.7 }}>
              {t('home.servicesSubtitle')}
            </Typography>
          </Box>

          <Grid container spacing={3}>
            {features.map((feat, i) => (
              <Grid key={i} size={{ xs: 12, sm: 6 }}>
                <Box sx={{
                  p: { xs: 3, md: 4 }, height: '100%', borderRadius: 3.5,
                  border: `1px solid ${alpha(primary, 0.09)}`,
                  background: bgDefault, transition: 'all 0.3s ease', cursor: 'default',
                  '&:hover': {
                    borderColor: alpha(primary, 0.32),
                    background: bgPaper, transform: 'translateY(-5px)',
                    boxShadow: isDark ? '0 20px 48px rgba(0,0,0,0.45)' : '0 16px 40px rgba(0,0,0,0.08)',
                  },
                }}>
                  <Box sx={{
                    width: 52, height: 52, borderRadius: 2.5, mb: 3,
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    background: alpha(primary, 0.1),
                    color: primary,
                    '& .MuiSvgIcon-root': { fontSize: 26 },
                  }}>
                    {feat.icon}
                  </Box>
                  <Typography variant="h5" sx={{ color: textPrimary, mb: 1.5 }}>{feat.title}</Typography>
                  <Typography variant="body2" sx={{ color: textSecondary, lineHeight: 1.75 }}>{feat.desc}</Typography>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Container>
      </Box>

      {/* ─── HOW IT WORKS ─── */}
      <Box sx={{ py: { xs: 9, md: 14 }, bgcolor: bgDefault }}>
        <Container maxWidth="lg">
          <Box sx={{ textAlign: 'center', mb: { xs: 6, md: 10 } }}>
            <Typography variant="overline" sx={{ color: primary, letterSpacing: '0.15em', mb: 1.5, display: 'block' }}>
              {t('home.howItWorksEyebrow')}
            </Typography>
            <Typography variant="h2" sx={{ color: textPrimary }}>
              {t('home.howItWorksTitle')}
            </Typography>
          </Box>
          <Grid container spacing={4}>
            {steps.map((step, i) => (
              <Grid key={i} size={{ xs: 12, md: 4 }}>
                <Box sx={{ textAlign: 'center', px: { xs: 2, md: 3 } }}>
                  <Typography sx={{
                    fontFamily: displayFont,
                    fontSize: '6rem', fontWeight: 700, lineHeight: 1, mb: 2,
                    color: alpha(primary, 0.12),
                    userSelect: 'none',
                  }}>
                    {step.num}
                  </Typography>
                  <Typography variant="h5" sx={{ color: textPrimary, mb: 1.5 }}>{step.title}</Typography>
                  <Typography variant="body2" sx={{ color: textSecondary, lineHeight: 1.75 }}>{step.desc}</Typography>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Container>
      </Box>

      {/* ─── TESTIMONIALS ─── */}
      <Box sx={{ py: { xs: 9, md: 14 }, bgcolor: bgPaper }}>
        <Container maxWidth="lg">
          <Box sx={{ textAlign: 'center', mb: { xs: 6, md: 10 } }}>
            <Typography variant="overline" sx={{ color: primary, letterSpacing: '0.15em', mb: 1.5, display: 'block' }}>
              {t('home.testimonialsEyebrow')}
            </Typography>
            <Typography variant="h2" sx={{ color: textPrimary }}>
              {t('home.trustTitle')}
            </Typography>
          </Box>
          <Grid container spacing={3}>
            {testimonials.map((item, i) => (
              <Grid key={i} size={{ xs: 12, md: 4 }}>
                <Box sx={{
                  p: 4, height: '100%', borderRadius: 3.5,
                  border: `1px solid ${alpha(primary, 0.09)}`,
                  background: bgDefault, position: 'relative', overflow: 'hidden',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    borderColor: alpha(primary, 0.28),
                    transform: 'translateY(-4px)',
                    boxShadow: isDark ? '0 16px 40px rgba(0,0,0,0.4)' : '0 12px 32px rgba(0,0,0,0.08)',
                  },
                }}>
                  {/* Decorative quote mark — always uses Latin glyph, so OK with serif */}
                  <Typography sx={{
                    position: 'absolute', top: 12,
                    right: isRTL ? 'auto' : 20, left: isRTL ? 20 : 'auto',
                    fontFamily: '"Cormorant Garamond", "Georgia", serif',
                    fontSize: '6rem', fontWeight: 700, lineHeight: 1,
                    color: alpha(primary, 0.07),
                    pointerEvents: 'none', userSelect: 'none',
                  }}>
                    {isRTL ? '\u201C' : '"'}
                  </Typography>
                  <Typography variant="body1" sx={{
                    color: textSecondary, fontStyle: 'italic', lineHeight: 1.8,
                    mb: 3, position: 'relative', zIndex: 1,
                  }}>
                    &ldquo;{item.text}&rdquo;
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                    <Box sx={{
                      width: 36, height: 36, borderRadius: '50%',
                      background: `linear-gradient(135deg, ${primary}, ${alpha(primary, 0.6)})`,
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      color: isDark ? '#07090F' : '#fff',
                      fontSize: '0.875rem', fontWeight: 700, flexShrink: 0,
                    }}>
                      {item.name[0]}
                    </Box>
                    <Typography variant="subtitle2" sx={{ color: textPrimary, fontWeight: 600 }}>
                      {item.name}
                    </Typography>
                  </Box>
                </Box>
              </Grid>
            ))}
          </Grid>
        </Container>
      </Box>

      {/* ─── CTA SECTION ─── */}
      {!isAuthenticated && (
        <Box sx={{
          py: { xs: 10, md: 16 },
          background: isDark
            ? 'linear-gradient(135deg, #0B1222 0%, #162040 50%, #0B1222 100%)'
            : 'linear-gradient(135deg, #162040 0%, #253060 50%, #162040 100%)',
          position: 'relative', overflow: 'hidden', textAlign: 'center',
        }}>
          <Box sx={{
            position: 'absolute', top: 0, left: 0, right: 0, height: 1,
            background: `linear-gradient(90deg, transparent, ${primary} 40%, ${primary} 60%, transparent)`,
          }} />
          <Box sx={{
            position: 'absolute', bottom: 0, left: 0, right: 0, height: 1,
            background: `linear-gradient(90deg, transparent, ${primary} 40%, ${primary} 60%, transparent)`,
          }} />
          <Box sx={{
            position: 'absolute', top: '50%', left: '50%',
            transform: 'translate(-50%, -50%)',
            width: '60vw', height: '60vw', maxWidth: 600, maxHeight: 600, borderRadius: '50%',
            background: `radial-gradient(circle, ${alpha(primary, 0.06)} 0%, transparent 60%)`,
            pointerEvents: 'none',
          }} />
          <Container maxWidth="md" sx={{ position: 'relative', zIndex: 1 }}>
            <Typography sx={{
              fontFamily: displayFont,
              fontSize: { xs: '2.2rem', md: '3.2rem' },
              fontWeight: 600, color: '#EDE8DF', mb: 2.5,
            }}>
              {t('home.ctaTitle')}
            </Typography>
            <Typography variant="body1" sx={{
              color: 'rgba(237,232,223,0.65)', mb: 5,
              maxWidth: 460, mx: 'auto', lineHeight: 1.75,
            }}>
              {t('home.ctaSubtitle')}
            </Typography>
            <Button
              variant="contained"
              size="large"
              endIcon={<ArrowForward />}
              onClick={() => openAuthModal('register')}
              sx={{ borderRadius: 50, px: 5, py: 1.75, fontSize: '1rem' }}
            >
              {t('home.getStarted')}
            </Button>
          </Container>
        </Box>
      )}

      {/* ─── FOOTER ─── */}
      <Box
        component="footer"
        sx={{
          bgcolor: isDark ? '#040810' : '#0B1222',
          color: 'rgba(255,255,255,0.55)',
          py: { xs: 7, md: 10 },
          borderTop: `1px solid ${alpha(primary, 0.1)}`,
        }}
      >
        <Container maxWidth="lg">
          <Grid container spacing={6} sx={{ mb: 6 }}>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography sx={{
                fontFamily: displayFont,
                fontSize: '2rem', fontWeight: 600, color: primary, mb: 2, letterSpacing: '-0.01em',
              }}>
                {t('appName')}
              </Typography>
              <Typography variant="body2" sx={{ lineHeight: 1.8, maxWidth: 280 }}>
                {t('home.footerDesc')}
              </Typography>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="subtitle2" sx={{
                color: 'rgba(255,255,255,0.85)', fontWeight: 600, mb: 3,
                letterSpacing: '0.06em', textTransform: 'uppercase', fontSize: '0.75rem',
              }}>
                {t('home.quickLinks')}
              </Typography>
              <Stack spacing={1.5}>
                {[
                  { href: '/', label: t('nav.home') },
                  { href: '/scholarships', label: t('nav.scholarships') },
                  { href: '/community', label: t('nav.community') },
                ].map((link) => (
                  <Box
                    key={link.href}
                    component="a"
                    href={link.href}
                    sx={{
                      color: 'rgba(255,255,255,0.55)', textDecoration: 'none',
                      fontSize: '0.875rem', transition: 'color 0.2s',
                      '&:hover': { color: primary },
                    }}
                  >
                    {link.label}
                  </Box>
                ))}
              </Stack>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Typography variant="subtitle2" sx={{
                color: 'rgba(255,255,255,0.85)', fontWeight: 600, mb: 3,
                letterSpacing: '0.06em', textTransform: 'uppercase', fontSize: '0.75rem',
              }}>
                {t('home.contactUs')}
              </Typography>
              <Stack direction="row" spacing={1.5}>
                {[EmailIcon, GitHubIcon].map((Icon, i) => (
                  <IconButton
                    key={i}
                    size="small"
                    sx={{
                      color: 'rgba(255,255,255,0.55)',
                      border: '1px solid rgba(255,255,255,0.12)',
                      borderRadius: 2, transition: 'all 0.2s',
                      '&:hover': { color: primary, borderColor: primary, bgcolor: alpha(primary, 0.08) },
                    }}
                  >
                    <Icon fontSize="small" />
                  </IconButton>
                ))}
              </Stack>
            </Grid>
          </Grid>
          <Box sx={{
            pt: 4, borderTop: '1px solid rgba(255,255,255,0.07)', textAlign: 'center',
          }}>
            <Typography variant="caption" sx={{ letterSpacing: '0.06em', color: 'rgba(255,255,255,0.35)' }}>
              &copy; {new Date().getFullYear()} {t('appName')}. {t('allRightsReserved')}
            </Typography>
          </Box>
        </Container>
      </Box>
    </Box>
  );
}
