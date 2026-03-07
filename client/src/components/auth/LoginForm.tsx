import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  TextField,
  Button,
  FormControlLabel,
  Checkbox,
  Typography,
  Alert,
  IconButton,
  InputAdornment,
  Divider,
  CircularProgress,
} from '@mui/material';
import {
  Visibility,
  VisibilityOff,
  Google as GoogleIcon,
} from '@mui/icons-material';
import { translateError } from '@/utils/errorUtils';
import type { AxiosError } from 'axios';

interface LoginFormProps {
  onSuccess?: () => void;
  onSwitchToRegister?: () => void;
  onSwitchToForgotPassword?: () => void;
  onLogin: (data: { identifier: string; password: string; rememberMe: boolean }) => Promise<void>;
  onExternalLogin?: (provider: string) => void;
}

export function LoginForm({
  onSuccess,
  onSwitchToRegister,
  onSwitchToForgotPassword,
  onLogin,
  onExternalLogin,
}: LoginFormProps) {
  const { t } = useTranslation();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;

    setError(null);
    setLoading(true);

    try {
      await onLogin({ identifier, password, rememberMe });
      onSuccess?.();
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string; errors?: string[] }>;
      const data = axiosErr?.response?.data;
      if (data?.error) {
        setError(translateError(data.error));
      } else if (data?.errors && Array.isArray(data.errors) && data.errors.length > 0) {
        setError(translateError(data.errors[0] as string));
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
        {t('login')}
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <TextField
        fullWidth
        label={t('email')}
        value={identifier}
        onChange={(e) => setIdentifier(e.target.value)}
        margin="normal"
        autoComplete="email"
        autoFocus
        required
      />

      <TextField
        fullWidth
        label={t('password')}
        type={showPassword ? 'text' : 'password'}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        margin="normal"
        autoComplete="current-password"
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

      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 1 }}>
        <FormControlLabel
          control={
            <Checkbox
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              size="small"
            />
          }
          label={<Typography variant="body2">{t('rememberMe')}</Typography>}
        />
        {onSwitchToForgotPassword && (
          <Typography
            variant="body2"
            color="primary"
            sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
            onClick={onSwitchToForgotPassword}
          >
            {t('forgotPassword')}
          </Typography>
        )}
      </Box>

      <Button
        type="submit"
        fullWidth
        variant="contained"
        size="large"
        disabled={loading || !identifier || !password}
        sx={{ mt: 2, mb: 2 }}
      >
        {loading ? <CircularProgress size={24} /> : t('login')}
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

      {onSwitchToRegister && (
        <Typography variant="body2" textAlign="center" sx={{ mt: 2 }}>
          {t('dontHaveAccount')}{' '}
          <Typography
            component="span"
            variant="body2"
            color="primary"
            sx={{ cursor: 'pointer', '&:hover': { textDecoration: 'underline' } }}
            onClick={onSwitchToRegister}
          >
            {t('signUpNow')}
          </Typography>
        </Typography>
      )}
    </Box>
  );
}
