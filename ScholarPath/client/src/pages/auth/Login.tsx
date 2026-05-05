import { Container, Paper, Snackbar, Alert } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { LoginForm } from '@/components/auth/LoginForm';
import { useAuth } from '@/hooks/useAuth';
import { useSsoAuth } from '@/hooks/useSsoAuth';
import { translateError } from '@/utils/errorUtils';

export default function Login() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const { handleExternalLogin, error: ssoError, setError: setSsoError } = useSsoAuth();

  const handleLogin = async (data: {
    identifier: string;
    password: string;
    rememberMe: boolean;
  }) => {
    await login(data);
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
        open={!!ssoError}
        autoHideDuration={4000}
        onClose={() => setSsoError(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="error" onClose={() => setSsoError(null)} variant="filled">
          {ssoError ? translateError(ssoError) : ''}
        </Alert>
      </Snackbar>
    </Container>
  );
}
