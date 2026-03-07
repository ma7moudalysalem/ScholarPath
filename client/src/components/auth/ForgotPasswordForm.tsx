import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  TextField,
  Button,
  Typography,
  Alert,
  CircularProgress,
} from '@mui/material';
import { authService } from '@/services/authService';

interface ForgotPasswordFormProps {
  onSwitchToLogin?: () => void;
}

export function ForgotPasswordForm({ onSwitchToLogin }: ForgotPasswordFormProps) {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading || !email.trim()) return;

    setLoading(true);
    try {
      await authService.forgotPassword({ email });
    } catch {
      // Always show success to prevent account enumeration
    } finally {
      setLoading(false);
      setSubmitted(true);
    }
  };

  if (submitted) {
    return (
      <Box textAlign="center">
        <Typography variant="h5" fontWeight={700} gutterBottom>
          {t('auth.checkYourEmail')}
        </Typography>
        <Alert severity="success" sx={{ mb: 2 }}>
          {t('auth.resetEmailSent')}
        </Alert>
        {onSwitchToLogin && (
          <Button variant="text" onClick={onSwitchToLogin}>
            {t('auth.backToLogin')}
          </Button>
        )}
      </Box>
    );
  }

  return (
    <Box component="form" onSubmit={handleSubmit} noValidate>
      <Typography variant="h5" fontWeight={700} gutterBottom textAlign="center">
        {t('forgotPassword')}
      </Typography>
      <Typography variant="body2" color="text.secondary" textAlign="center" sx={{ mb: 2 }}>
        {t('auth.forgotPasswordDesc')}
      </Typography>

      <TextField
        fullWidth
        label={t('email')}
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        margin="normal"
        autoComplete="email"
        autoFocus
        required
      />

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={loading || !email.trim()}
        sx={{ mt: 2, mb: 2 }}
      >
        {loading ? <CircularProgress size={24} /> : t('auth.sendResetLink')}
      </Button>

      {onSwitchToLogin && (
        <Typography variant="body2" textAlign="center">
          <Typography
            component="span"
            variant="body2"
            color="primary"
            sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
            onClick={onSwitchToLogin}
          >
            {t('auth.backToLogin')}
          </Typography>
        </Typography>
      )}
    </Box>
  );
}
