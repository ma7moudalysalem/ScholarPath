import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  IconButton,
  InputAdornment,
  TextField,
  Typography,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';
import { authService } from '@/services/authService';
import { translateError } from '@/utils/errorUtils';
import type { AxiosError } from 'axios';

export default function SecurityTab() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const isSsoOnly = user?.hasPassword === false;

  const [form, setForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmNewPassword: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const updateField = (field: string, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setError(null);
    setSuccess(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;

    if (form.newPassword !== form.confirmNewPassword) {
      setError(t('errors.validation.passwordsMismatch'));
      return;
    }

    if (form.newPassword.length < 8) {
      setError(t('errors.validation.passwordMinLength'));
      return;
    }

    setError(null);
    setLoading(true);

    try {
      await authService.changePassword({
        currentPassword: isSsoOnly ? undefined : form.currentPassword,
        newPassword: form.newPassword,
        confirmNewPassword: form.confirmNewPassword,
      });
      setSuccess(true);
      setForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string }>;
      setError(translateError(axiosErr?.response?.data?.error ?? t('error')));
    } finally {
      setLoading(false);
    }
  };

  const isSubmitDisabled =
    loading ||
    !form.newPassword ||
    !form.confirmNewPassword ||
    (!isSsoOnly && !form.currentPassword);

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          {isSsoOnly ? t('securityPage.setPassword') : t('changePassword')}
        </Typography>

        {isSsoOnly && (
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('securityPage.setPasswordDescription')}
          </Alert>
        )}

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}
        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            {isSsoOnly ? t('securityPage.passwordSet') : t('profile.passwordChanged')}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit} noValidate sx={{ maxWidth: 400 }}>
          {!isSsoOnly && (
            <TextField
              fullWidth
              label={t('profile.currentPassword')}
              type={showPassword ? 'text' : 'password'}
              value={form.currentPassword}
              onChange={(e) => updateField('currentPassword', e.target.value)}
              margin="normal"
              autoComplete="current-password"
              required
              slotProps={{
                input: {
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton
                        onClick={() => setShowPassword(!showPassword)}
                        edge="end"
                        size="small"
                      >
                        {showPassword ? <VisibilityOff /> : <Visibility />}
                      </IconButton>
                    </InputAdornment>
                  ),
                },
              }}
            />
          )}

          <TextField
            fullWidth
            label={t('auth.newPassword')}
            type={showPassword ? 'text' : 'password'}
            value={form.newPassword}
            onChange={(e) => updateField('newPassword', e.target.value)}
            margin="normal"
            autoComplete="new-password"
            required
          />

          <TextField
            fullWidth
            label={t('confirmPassword')}
            type={showPassword ? 'text' : 'password'}
            value={form.confirmNewPassword}
            onChange={(e) => updateField('confirmNewPassword', e.target.value)}
            margin="normal"
            autoComplete="new-password"
            required
          />

          <Button type="submit" variant="contained" disabled={isSubmitDisabled} sx={{ mt: 2 }}>
            {loading ? (
              <CircularProgress size={24} />
            ) : isSsoOnly ? (
              t('securityPage.setPassword')
            ) : (
              t('changePassword')
            )}
          </Button>
        </Box>
      </CardContent>
    </Card>
  );
}
