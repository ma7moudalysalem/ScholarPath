import { Box, Typography, Container, Paper, useTheme } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { School, People, RecordVoiceOver, WorkOutline } from '@mui/icons-material';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Pagination } from 'swiper/modules';

import 'swiper/css';
import 'swiper/css/pagination';

export function ServicesSection() {
    const { t } = useTranslation();
    const theme = useTheme();

    const services = [
        {
            icon: <School sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />,
            title: t('scholarships', 'Scholarships'),
            description: t('home.services.scholarshipsDesc', 'Discover worldwide scholarships matching your profile and background.'),
        },
        {
            icon: <People sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />,
            title: t('community', 'Community'),
            description: t('home.services.communityDesc', 'Connect, collaborate, and share knowledge with peers.'),
        },
        {
            icon: <RecordVoiceOver sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />,
            title: t('home.services.mentorship', 'Mentorship'),
            description: t('home.services.mentorshipDesc', 'Schedule consultations with expert advisors to review your application.'),
        },
        {
            icon: <WorkOutline sx={{ fontSize: 48, color: 'primary.main', mb: 2 }} />,
            title: t('home.services.opportunities', 'Opportunities'),
            description: t('home.services.opportunitiesDesc', 'Find internships and job placements sponsored by companies.'),
        },
    ];

    if (!services || services.length === 0) {
        return (
            <Box sx={{ py: 8, textAlign: 'center' }}>
                <Typography variant="h6" color="text.secondary">
                    {t('home.services.unavailable', 'Services are currently unavailable. Please check back later.')}
                </Typography>
            </Box>
        );
    }

    return (
        <Box sx={{ py: 8, bgcolor: 'background.default' }}>
            <Container maxWidth="lg">
                <Typography variant="h3" fontWeight={700} textAlign="center" gutterBottom sx={{ mb: 6 }}>
                    {t('home.services.title', 'Our Services')}
                </Typography>

                {/* Desktop View */}
                <Box sx={{ display: { xs: 'none', md: 'grid' }, gridTemplateColumns: 'repeat(4, 1fr)', gap: 3 }}>
                    {services.map((service, index) => (
                        <Paper
                            key={index}
                            elevation={0}
                            sx={{
                                p: 4,
                                textAlign: 'center',
                                height: '100%',
                                bgcolor: 'background.paper',
                                borderRadius: 4,
                                border: '1px solid',
                                borderColor: 'divider',
                                transition: 'transform 0.2s, box-shadow 0.2s',
                                '&:hover': {
                                    transform: 'translateY(-4px)',
                                    boxShadow: theme.shadows[4],
                                },
                            }}
                        >
                            {service.icon}
                            <Typography variant="h6" fontWeight={600} gutterBottom>
                                {service.title}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.6 }}>
                                {service.description}
                            </Typography>
                        </Paper>
                    ))}
                </Box>

                {/* Mobile View - Swipeable */}
                <Box sx={{ display: { xs: 'block', md: 'none' } }}>
                    <Swiper
                        modules={[Pagination]}
                        pagination={{ clickable: true, dynamicBullets: true }}
                        spaceBetween={16}
                        slidesPerView={1.2}
                        centeredSlides={true}
                        style={{ paddingBottom: '40px' }}
                        dir={theme.direction}
                    >
                        {services.map((service, index) => (
                            <SwiperSlide key={index}>
                                <Paper
                                    elevation={0}
                                    sx={{
                                        p: 3,
                                        textAlign: 'center',
                                        height: '100%',
                                        bgcolor: 'background.paper',
                                        borderRadius: 4,
                                        border: '1px solid',
                                        borderColor: 'divider',
                                        minHeight: '220px',
                                    }}
                                >
                                    {service.icon}
                                    <Typography variant="h6" fontWeight={600} gutterBottom>
                                        {service.title}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        {service.description}
                                    </Typography>
                                </Paper>
                            </SwiperSlide>
                        ))}
                    </Swiper>
                </Box>
            </Container>
        </Box>
    );
}
