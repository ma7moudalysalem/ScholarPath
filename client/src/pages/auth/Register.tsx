import { Container, Paper, Snackbar, Alert } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { RegisterForm } from '@/components/auth/RegisterForm';
import { useAuth } from '@/hooks/useAuth';
import { useSsoAuth } from '@/hooks/useSsoAuth';
import { translateError } from '@/utils/errorUtils';

export default function Register() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const { handleExternalLogin, error: ssoError, setError: setSsoError } = useSsoAuth();

  const handleRegister = async (data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }) => {
    await register(data);
  };

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <RegisterForm
          onRegister={handleRegister}
          onSwitchToLogin={() => navigate('/login')}
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
