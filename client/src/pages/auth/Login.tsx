import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Container, Paper, Snackbar, Alert } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { LoginForm } from '@/components/auth/LoginForm';
import { useAuth } from '@/hooks/useAuth';

export default function Login() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { login } = useAuth();
  const [ssoToast, setSsoToast] = useState(false);

  const handleLogin = async (data: { identifier: string; password: string; rememberMe: boolean }) => {
    await login(data);
  };

  const handleExternalLogin = (_provider: string) => {
    setSsoToast(true);
  };

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <LoginForm
          onLogin={handleLogin}
          onSwitchToRegister={() => navigate('/register')}
          onSwitchToForgotPassword={() => navigate('/forgot-password')}
          onExternalLogin={handleExternalLogin}
        />
      </Paper>

      <Snackbar
        open={ssoToast}
        autoHideDuration={4000}
        onClose={() => setSsoToast(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="info" onClose={() => setSsoToast(false)} variant="filled">
          {t('auth.ssoComingSoon')}
        </Alert>
      </Snackbar>
    </Container>
  );
}
