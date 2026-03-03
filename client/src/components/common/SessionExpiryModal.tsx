import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    Button,
    TextField,
    Box,
    Alert,
    CircularProgress,
} from '@mui/material';
import { useAuthStore } from '@/stores/authStore';
import { authService } from '@/services/authService';

export function SessionExpiryModal() {
    const { t } = useTranslation();
    const isSessionExpired = useAuthStore((s) => s.isSessionExpired);
    const setSessionExpired = useAuthStore((s) => s.setSessionExpired);
    const user = useAuthStore((s) => s.user);
    const setAuth = useAuthStore((s) => s.setAuth);

    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    // If the user isn't fully logged in according to state (e.g. they logged out somehow), don't show the overlay
    // Or if no user email is known, we can't do a quick re-auth
    if (!isSessionExpired || !user?.email) {
        return null;
    }

    const handleReAuth = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!password) {
            setError(t('auth.passwordRequired', 'Password is required'));
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const response = await authService.login({
                identifier: user.email,
                password,
            });
            // Updating auth state will clear interceptor queues and allow pending requests to retry
            setAuth(response.user, response.accessToken, response.refreshToken);
            setSessionExpired(false);
            setPassword('');
            // Note: In api.ts, the failed requests queue logic should ideally be triggered when this succeeds,
            // but standard Axios interceptor queues are bound to the refresh cycle. 
            // Re-auth from UI may simply allow subsequent actions to work, and the user must click "save" again.
        } catch (err) {
            const error = err as import('axios').AxiosError<{ message: string }>;
            setError(error.response?.data?.message || t('auth.loginFailed', 'Invalid password. Please try again.'));
        } finally {
            setIsLoading(false);
        }
    };

    const handleForceLogout = () => {
        // If they choose not to re-authenticate, we wipe state and force home
        useAuthStore.getState().logout();
        setSessionExpired(false);
        window.location.href = '/login';
    };

    return (
        <Dialog
            open={isSessionExpired}
            // Prevent closing by clicking outside or pressing Escape
            disableEscapeKeyDown
            onClose={(_event, reason) => {
                if (reason !== 'backdropClick' && reason !== 'escapeKeyDown') {
                    // Allow close if requested via standard means (unlikely here)
                }
            }}
            BackdropProps={{
                sx: { backgroundColor: 'rgba(0,0,0,0.8)' } // Darker backdrop to focus attention
            }}
        >
            <DialogTitle color="error.main">
                {t('auth.sessionExpiredTitle', 'Session Expired')}
            </DialogTitle>

            <form onSubmit={handleReAuth}>
                <DialogContent>
                    <DialogContentText sx={{ mb: 3 }}>
                        {t(
                            'auth.sessionExpiredMessage',
                            'Your session has expired due to inactivity. Please enter your password to continue working without losing your progress.'
                        )}
                    </DialogContentText>

                    <Box sx={{ mb: 2 }}>
                        {/* Read-only email context */}
                        <TextField
                            fullWidth
                            disabled
                            label={t('email', 'Email')}
                            value={user.email}
                            margin="normal"
                            size="small"
                        />
                        <TextField
                            fullWidth
                            type="password"
                            label={t('password', 'Password')}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            margin="normal"
                            autoFocus
                            error={!!error}
                            disabled={isLoading}
                        />
                    </Box>

                    {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}

                </DialogContent>
                <DialogActions sx={{ px: 3, pb: 3, justifyContent: 'space-between' }}>
                    <Button onClick={handleForceLogout} color="inherit" disabled={isLoading}>
                        {t('auth.logoutInstead', 'Logout entirely')}
                    </Button>
                    <Button
                        type="submit"
                        variant="contained"
                        color="primary"
                        disabled={isLoading || !password}
                        startIcon={isLoading ? <CircularProgress size={20} color="inherit" /> : null}
                    >
                        {t('auth.reconnect', 'Reconnect')}
                    </Button>
                </DialogActions>
            </form>
        </Dialog>
    );
}
