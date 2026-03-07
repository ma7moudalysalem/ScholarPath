import { useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Container,
  Paper,
  Box,
  TextField,
  Button,
  Typography,
  Alert,
  IconButton,
  InputAdornment,
  CircularProgress,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { authService } from '@/services/authService';
import { translateError } from '@/utils/errorUtils';
import type { AxiosError } from 'axios';

export default function ResetPassword() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;

    if (newPassword !== confirmPassword) {
      setError(t('errors.validation.passwordsMismatch'));
      return;
    }

    setError(null);
    setLoading(true);

    try {
      await authService.resetPassword({
        token,
        newPassword,
        confirmNewPassword: confirmPassword,
      });
      setSuccess(true);
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string }>;
      setError(translateError(axiosErr?.response?.data?.error ?? t('error')));
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <Container maxWidth="xs" sx={{ py: 8 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h5" fontWeight={700} gutterBottom>
            {t('auth.passwordResetSuccess')}
          </Typography>
          <Alert severity="success" sx={{ mb: 2 }}>
            {t('auth.passwordResetSuccessDesc')}
          </Alert>
          <Button variant="contained" onClick={() => navigate('/login')}>
            {t('login')}
          </Button>
        </Paper>
      </Container>
    );
  }

  if (!token) {
    return (
      <Container maxWidth="xs" sx={{ py: 8 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Alert severity="error">{t('auth.invalidResetLink')}</Alert>
          <Button variant="text" sx={{ mt: 2 }} onClick={() => navigate('/forgot-password')}>
            {t('auth.requestNewLink')}
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Box component="form" onSubmit={handleSubmit} noValidate>
          <Typography variant="h5" fontWeight={700} gutterBottom textAlign="center">
            {t('resetPassword')}
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
              {error}
            </Alert>
          )}

          <TextField
            fullWidth
            label={t('auth.newPassword')}
            type={showPassword ? 'text' : 'password'}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            margin="normal"
            autoComplete="new-password"
            autoFocus
            required
            slotProps={{
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton onClick={() => setShowPassword(!showPassword)} edge="end" size="small">
                      {showPassword ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
          />

          <TextField
            fullWidth
            label={t('confirmPassword')}
            type={showPassword ? 'text' : 'password'}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            margin="normal"
            autoComplete="new-password"
            required
          />

          <Button
            type="submit"
            fullWidth
            variant="contained"
            size="large"
            disabled={loading || !newPassword || !confirmPassword}
            sx={{ mt: 2 }}
          >
            {loading ? <CircularProgress size={24} /> : t('resetPassword')}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
}
