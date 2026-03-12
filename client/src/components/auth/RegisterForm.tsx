import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  TextField,
  Button,
  Typography,
  Alert,
  IconButton,
  InputAdornment,
  Divider,
  FormControlLabel,
  Checkbox,
  CircularProgress,
} from '@mui/material';
import { Visibility, VisibilityOff, Google as GoogleIcon } from '@mui/icons-material';
import { translateError } from '@/utils/errorUtils';
import type { AxiosError } from 'axios';

interface RegisterFormProps {
  onSuccess?: () => void;
  onSwitchToLogin?: () => void;
  onRegister: (data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  onExternalLogin?: (provider: string) => void;
}

export function RegisterForm({
  onSuccess,
  onSwitchToLogin,
  onRegister,
  onExternalLogin,
}: RegisterFormProps) {
  const { t } = useTranslation();
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [agreedToTerms, setAgreedToTerms] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const updateField = (field: string, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setFieldErrors((prev) => ({ ...prev, [field]: '' }));
  };

  const validate = (): boolean => {
    const errors: Record<string, string> = {};
    if (!form.firstName.trim()) errors.firstName = t('errors.validation.firstNameRequired');
    if (!form.lastName.trim()) errors.lastName = t('errors.validation.lastNameRequired');
    if (!form.email.trim()) errors.email = t('errors.validation.emailRequired');
    if (!form.password) errors.password = t('errors.validation.passwordRequired');
    else if (form.password.length < 8) errors.password = t('errors.validation.passwordMinLength');
    if (form.password !== form.confirmPassword)
      errors.confirmPassword = t('errors.validation.passwordsMismatch');
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading || !agreedToTerms) return;
    if (!validate()) return;

    setError(null);
    setLoading(true);

    try {
      await onRegister(form);
      onSuccess?.();
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string; errors?: string[] }>;
      const data = axiosErr?.response?.data;
      if (data?.error) {
        setError(translateError(data.error));
      } else if (data?.errors && data.errors.length > 0) {
        setError(data.errors.map(translateError).join(' '));
      } else {
        setError(t('error'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} noValidate>
      <Typography variant="h5" fontWeight={700} gutterBottom textAlign="center">
        {t('register')}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Box sx={{ display: 'flex', gap: 1 }}>
        <TextField
          fullWidth
          label={t('firstName')}
          value={form.firstName}
          onChange={(e) => updateField('firstName', e.target.value)}
          margin="normal"
          autoComplete="given-name"
          autoFocus
          required
          error={!!fieldErrors.firstName}
          helperText={fieldErrors.firstName}
        />
        <TextField
          fullWidth
          label={t('lastName')}
          value={form.lastName}
          onChange={(e) => updateField('lastName', e.target.value)}
          margin="normal"
          autoComplete="family-name"
          required
          error={!!fieldErrors.lastName}
          helperText={fieldErrors.lastName}
        />
      </Box>

      <TextField
        fullWidth
        label={t('email')}
        type="email"
        value={form.email}
        onChange={(e) => updateField('email', e.target.value)}
        margin="normal"
        autoComplete="email"
        required
        error={!!fieldErrors.email}
        helperText={fieldErrors.email}
      />

      <TextField
        fullWidth
        label={t('password')}
        type={showPassword ? 'text' : 'password'}
        value={form.password}
        onChange={(e) => updateField('password', e.target.value)}
        margin="normal"
        autoComplete="new-password"
        required
        error={!!fieldErrors.password}
        helperText={fieldErrors.password || t('auth.passwordPolicy')}
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
        value={form.confirmPassword}
        onChange={(e) => updateField('confirmPassword', e.target.value)}
        margin="normal"
        autoComplete="new-password"
        required
        error={!!fieldErrors.confirmPassword}
        helperText={fieldErrors.confirmPassword}
      />

      <FormControlLabel
        control={
          <Checkbox
            checked={agreedToTerms}
            onChange={(e) => setAgreedToTerms(e.target.checked)}
            size="small"
          />
        }
        label={<Typography variant="body2">{t('auth.agreeToTerms')}</Typography>}
        sx={{ mt: 1 }}
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={loading || !agreedToTerms}
        sx={{ mt: 2, mb: 2 }}
      >
        {loading ? <CircularProgress size={24} /> : t('register')}
      </Button>

      {onExternalLogin && (
        <>
          <Divider sx={{ mb: 2 }}>
            <Typography variant="body2" color="text.secondary">
              {t('auth.orContinueWith')}
            </Typography>
          </Divider>

          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              fullWidth
              variant="outlined"
              startIcon={<GoogleIcon />}
              onClick={() => onExternalLogin('google')}
              disabled={loading}
            >
              Google
            </Button>
            <Button
              fullWidth
              variant="outlined"
              onClick={() => onExternalLogin('microsoft')}
              disabled={loading}
            >
              Microsoft
            </Button>
          </Box>
        </>
      )}

      {onSwitchToLogin && (
        <Typography variant="body2" textAlign="center" sx={{ mt: 2 }}>
          {t('alreadyHaveAccount')}{' '}
          <Typography
            component="span"
            variant="body2"
            color="primary"
            sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
            onClick={onSwitchToLogin}
          >
            {t('signInNow')}
          </Typography>
        </Typography>
      )}
    </Box>
  );
}
