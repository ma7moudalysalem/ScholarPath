import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Container, Paper, Snackbar, Alert } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { RegisterForm } from '@/components/auth/RegisterForm';
import { useAuth } from '@/hooks/useAuth';

export default function Register() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { register } = useAuth();
  const [ssoToast, setSsoToast] = useState(false);

  const handleRegister = async (data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }) => {
    await register(data);
  };

  const handleExternalLogin = (_provider: string) => {
    setSsoToast(true);
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
