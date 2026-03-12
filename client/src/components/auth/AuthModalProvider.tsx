import { Dialog, DialogContent, IconButton, Snackbar, Alert } from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import { useAuthModal } from '@/hooks/useAuthModal';
import { useAuth } from '@/hooks/useAuth';
import { useSsoAuth } from '@/hooks/useSsoAuth';
import { LoginForm } from './LoginForm';
import { RegisterForm } from './RegisterForm';
import { ForgotPasswordForm } from './ForgotPasswordForm';
import { translateError } from '@/utils/errorUtils';

export function AuthModalProvider() {
  const { view, close, switchTo } = useAuthModal();
  const { login, register } = useAuth();
  const { handleExternalLogin, error: ssoError, setError: setSsoError } = useSsoAuth(close);

  const handleLogin = async (data: { identifier: string; password: string; rememberMe: boolean }) => {
    await login(data);
    close();
  };

  const handleRegister = async (data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    confirmPassword: string;
  }) => {
    await register(data);
    close();
  };

  return (
    <>
      <Dialog
        open={view !== null}
        onClose={close}
        maxWidth="xs"
        fullWidth
        PaperProps={{ sx: { position: 'relative' } }}
      >
        <IconButton
          onClick={close}
          sx={{ position: 'absolute', top: 8, right: 8 }}
          size="small"
        >
          <CloseIcon />
        </IconButton>

        <DialogContent sx={{ pt: 4, pb: 3, px: 3 }}>
          {view === 'login' && (
            <LoginForm
              onLogin={handleLogin}
              onSuccess={close}
              onSwitchToRegister={() => switchTo('register')}
              onSwitchToForgotPassword={() => switchTo('forgotPassword')}
              onExternalLogin={handleExternalLogin}
            />
          )}
          {view === 'register' && (
            <RegisterForm
              onRegister={handleRegister}
              onSuccess={close}
              onSwitchToLogin={() => switchTo('login')}
              onExternalLogin={handleExternalLogin}
            />
          )}
          {view === 'forgotPassword' && (
            <ForgotPasswordForm
              onSwitchToLogin={() => switchTo('login')}
            />
          )}
        </DialogContent>
      </Dialog>

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
    </>
  );
}
