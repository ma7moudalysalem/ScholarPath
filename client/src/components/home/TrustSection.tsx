import { Box, Typography, Container, Paper, Avatar, Rating } from '@mui/material';
import Grid from '@mui/material/Grid2';
import { useTranslation } from 'react-i18next';

export function TrustSection() {
    const { t } = useTranslation();

    const testimonials = [
        {
            name: 'Sarah Ahmed',
            role: 'Computer Science Student',
            comment: 'ScholarPath helped me find a fully funded scholarship in Europe. The matchmaking process is incredibly accurate!',
            rating: 5,
            avatar: 'S',
        },
        {
            name: 'Mohamed Ali',
            role: 'Engineering Graduate',
            comment: 'The mentorship feature is a game-changer. My advisor guided me step-by-step through the tricky application essays.',
            rating: 5,
            avatar: 'M',
        },
        {
            name: 'Fatima Zahra',
            role: 'Medical Student',
            comment: 'I love the community discussions. It feels great to connect with other students who share similar goals and struggles.',
            rating: 4,
            avatar: 'F',
        },
    ];

    if (!testimonials || testimonials.length === 0) {
        return (
            <Box sx={{ py: 6, textAlign: 'center', bgcolor: (theme) => theme.palette.mode === 'dark' ? 'background.default' : 'grey.50' }}>
                <Typography variant="h6" color="text.secondary">
                    {t('home.trust.unavailable', 'Testimonials are currently unavailable.')}
                </Typography>
            </Box>
        );
    }

    return (
        <Box sx={{ py: { xs: 8, md: 10 }, bgcolor: 'background.paper' }}>
            <Container maxWidth="lg">
                <Typography variant="h3" fontWeight={700} textAlign="center" gutterBottom sx={{ mb: 2 }}>
                    {t('home.trust.title', 'Trusted by Thousands')}
                </Typography>
                <Typography variant="h6" color="text.secondary" textAlign="center" sx={{ mb: 8, maxWidth: '600px', mx: 'auto' }}>
                    {t('home.trust.subtitle', 'Join the fastest-growing community of students achieving their academic dreams.')}
                </Typography>

                <Grid container spacing={4}>
                    {testimonials.map((item, index) => (
                        <Grid size={{ xs: 12, md: 4 }} key={index}>
                            <Paper
                                elevation={0}
                                sx={{
                                    p: 4,
                                    height: '100%',
                                    bgcolor: (theme) => theme.palette.mode === 'dark' ? 'background.default' : 'grey.50',
                                    borderRadius: 4,
                                    display: 'flex',
                                    flexDirection: 'column',
                                }}
                            >
                                <Rating value={item.rating} readOnly size="small" sx={{ mb: 3 }} />
                                <Typography variant="body1" sx={{ flexGrow: 1, mb: 4, fontStyle: 'italic', color: 'text.secondary' }}>
                                    "{item.comment}"
                                </Typography>
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                                    <Avatar sx={{ bgcolor: 'primary.main', fontWeight: 'bold' }}>
                                        {item.avatar}
                                    </Avatar>
                                    <Box>
                                        <Typography variant="subtitle2" fontWeight={700}>
                                            {item.name}
                                        </Typography>
                                        <Typography variant="caption" color="text.secondary">
                                            {item.role}
                                        </Typography>
                                    </Box>
                                </Box>
                            </Paper>
                        </Grid>
                    ))}
                </Grid>
            </Container>
        </Box>
    );
}
