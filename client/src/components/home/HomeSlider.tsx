import { Box, Button, Container, Typography, Stack, useTheme, Fade } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Navigation, Pagination, Autoplay, EffectFade } from 'swiper/modules';
import { useAuthStore } from '@/stores/authStore';
import { useProtectedAction } from '@/hooks/useProtectedAction';
import { ArrowForward, LoginOutlined } from '@mui/icons-material';

import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
import 'swiper/css/effect-fade';

export function HomeSlider() {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const theme = useTheme();
    const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
    const executeProtectedAction = useProtectedAction();

    const slides = [
        {
            title: t('home.slider.slide1.title', 'Discover Your Dream Scholarship'),
            description: t('home.slider.slide1.description', 'AI-powered match-making for global opportunities tailored to your profile and goals.'),
            // Modern mesh gradient approach
            bgGradient: 'radial-gradient(circle at 20% 50%, rgba(25, 118, 210, 0.85) 0%, rgba(13, 71, 161, 0.95) 100%), url("https://images.unsplash.com/photo-1523050854058-8df90110c9f1?q=80&w=2070&auto=format&fit=crop") center/cover',
        },
        {
            title: t('home.slider.slide2.title', 'Join a Thriving Community'),
            description: t('home.slider.slide2.description', 'Connect with peers, alumni, and mentors worldwide to accelerate your academic journey.'),
            bgGradient: 'radial-gradient(circle at 80% 50%, rgba(156, 39, 176, 0.85) 0%, rgba(74, 20, 140, 0.95) 100%), url("https://images.unsplash.com/photo-1523240795612-9a054b0db644?q=80&w=2070&auto=format&fit=crop") center/cover',
        },
        {
            title: t('home.slider.slide3.title', 'Expert Mentorship'),
            description: t('home.slider.slide3.description', 'Get personalized guidance and feedback from verified advisors on your scholarship applications.'),
            bgGradient: 'radial-gradient(circle at 50% 100%, rgba(46, 125, 50, 0.85) 0%, rgba(27, 94, 32, 0.95) 100%), url("https://images.unsplash.com/photo-1517486808906-6ca8b3f04846?q=80&w=1949&auto=format&fit=crop") center/cover',
        },
    ];

    if (!slides || slides.length === 0) {
        return (
            <Box sx={{ py: 10, textAlign: 'center', bgcolor: 'grey.100' }}>
                <Typography variant="h6" color="text.secondary">
                    {t('home.slider.unavailable', 'Intro content is currently unavailable. Please check back later.')}
                </Typography>
            </Box>
        );
    }

    const handleCtaClick = () => {
        executeProtectedAction(() => {
            navigate('/dashboard');
        }, '/dashboard');
    };

    return (
        <Box
            sx={{
                width: '100%',
                position: 'relative',
                '& .swiper-button-next, & .swiper-button-prev': {
                    color: 'white',
                    background: 'rgba(255, 255, 255, 0.1)',
                    backdropFilter: 'blur(10px)',
                    width: '50px',
                    height: '50px',
                    borderRadius: '50%',
                    transition: 'all 0.3s ease',
                    '&:hover': {
                        background: 'rgba(255, 255, 255, 0.25)',
                        transform: 'scale(1.1)',
                    },
                    '&::after': {
                        fontSize: '1.2rem',
                        fontWeight: 'bold',
                    }
                },
                '& .swiper-pagination-bullet': {
                    backgroundColor: 'rgba(255,255,255,0.5)',
                    width: '10px',
                    height: '10px',
                    transition: 'all 0.3s ease',
                },
                '& .swiper-pagination-bullet-active': {
                    backgroundColor: 'white',
                    width: '30px',
                    borderRadius: '5px',
                }
            }}
        >
            <Swiper
                modules={[Navigation, Pagination, Autoplay, EffectFade]}
                effect={"fade"}
                navigation
                pagination={{ clickable: true }}
                autoplay={{ delay: 6000, disableOnInteraction: false }}
                loop
                dir={theme.direction}
            >
                {slides.map((slide, index) => (
                    <SwiperSlide key={index}>
                        <Box
                            sx={{
                                background: slide.bgGradient,
                                color: 'white',
                                pt: { xs: 12, md: 20 },
                                pb: { xs: 10, md: 16 },
                                minHeight: { xs: '80vh', md: '600px' },
                                display: 'flex',
                                alignItems: 'center',
                                position: 'relative',
                                overflow: 'hidden'
                            }}
                        >
                            {/* Decorative background elements */}
                            <Box
                                sx={{
                                    position: 'absolute',
                                    top: '-20%',
                                    left: '-10%',
                                    width: '300px',
                                    height: '300px',
                                    background: 'rgba(255, 255, 255, 0.1)',
                                    filter: 'blur(80px)',
                                    borderRadius: '50%',
                                }}
                            />

                            <Container maxWidth="lg" sx={{ position: 'relative', zIndex: 1 }}>
                                <Fade in={true} timeout={1000}>
                                    <Box
                                        sx={{
                                            maxWidth: '800px',
                                            mx: 'auto',
                                            textAlign: 'center',
                                            display: 'flex',
                                            flexDirection: 'column',
                                            alignItems: 'center',
                                            background: 'rgba(255, 255, 255, 0.05)',
                                            backdropFilter: 'blur(20px)',
                                            WebkitBackdropFilter: 'blur(20px)',
                                            border: '1px solid rgba(255, 255, 255, 0.1)',
                                            borderRadius: '24px',
                                            p: { xs: 4, md: 6 },
                                            boxShadow: '0 8px 32px 0 rgba(0, 0, 0, 0.3)',
                                            transform: 'translateY(0)',
                                            animation: 'float 6s ease-in-out infinite',
                                            '@keyframes float': {
                                                '0%': { transform: 'translateY(0px)' },
                                                '50%': { transform: 'translateY(-10px)' },
                                                '100%': { transform: 'translateY(0px)' },
                                            }
                                        }}
                                    >
                                        <Typography
                                            variant="h1"
                                            fontWeight={800}
                                            gutterBottom
                                            sx={{
                                                fontSize: { xs: '2.5rem', sm: '3.5rem', md: '4.5rem' },
                                                lineHeight: 1.1,
                                                letterSpacing: '-0.02em',
                                                textShadow: '0 2px 10px rgba(0,0,0,0.3)'
                                            }}
                                        >
                                            {slide.title}
                                        </Typography>
                                        <Typography
                                            variant="h5"
                                            sx={{
                                                mb: 5,
                                                opacity: 0.9,
                                                fontWeight: 400,
                                                fontSize: { xs: '1.1rem', md: '1.35rem' },
                                                lineHeight: 1.6,
                                                maxWidth: '650px',
                                                textShadow: '0 1px 5px rgba(0,0,0,0.3)'
                                            }}
                                        >
                                            {slide.description}
                                        </Typography>
                                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="center" width={{ xs: '100%', sm: 'auto' }}>
                                            <Button
                                                variant="contained"
                                                color="secondary"
                                                size="large"
                                                onClick={handleCtaClick}
                                                endIcon={isAuthenticated ? <ArrowForward /> : <LoginOutlined />}
                                                sx={{
                                                    px: { xs: 4, md: 5 },
                                                    py: { xs: 1.5, md: 2 },
                                                    fontSize: '1.1rem',
                                                    fontWeight: 700,
                                                    borderRadius: '50px',
                                                    textTransform: 'none',
                                                    boxShadow: '0 4px 14px 0 rgba(0,0,0,0.25)',
                                                    transition: 'all 0.3s ease',
                                                    '&:hover': {
                                                        transform: 'translateY(-2px)',
                                                        boxShadow: '0 6px 20px rgba(0,0,0,0.4)',
                                                    }
                                                }}
                                            >
                                                {isAuthenticated
                                                    ? t('home.slider.ctaAuthenticated', 'Go to Dashboard')
                                                    : t('home.slider.ctaUnauthenticated', 'Get Started')}
                                            </Button>

                                            {!isAuthenticated && (
                                                <Button
                                                    variant="outlined"
                                                    size="large"
                                                    onClick={() => navigate('/scholarships')}
                                                    sx={{
                                                        px: { xs: 4, md: 5 },
                                                        py: { xs: 1.5, md: 2 },
                                                        fontSize: '1.1rem',
                                                        fontWeight: 600,
                                                        borderRadius: '50px',
                                                        color: 'white',
                                                        borderColor: 'rgba(255,255,255,0.5)',
                                                        textTransform: 'none',
                                                        '&:hover': {
                                                            borderColor: 'white',
                                                            bgcolor: 'rgba(255,255,255,0.1)'
                                                        }
                                                    }}
                                                >
                                                    {t('home.slider.explore', 'Explore Scholarships')}
                                                </Button>
                                            )}
                                        </Stack>
                                    </Box>
                                </Fade>
                            </Container>
                        </Box>
                    </SwiperSlide>
                ))}
            </Swiper>
        </Box>
    );
}
