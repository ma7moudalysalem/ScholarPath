import { Container, Paper } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { ForgotPasswordForm } from '@/components/auth/ForgotPasswordForm';

export default function ForgotPassword() {
  const navigate = useNavigate();

  return (
    <Container maxWidth="xs" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <ForgotPasswordForm onSwitchToLogin={() => navigate('/login')} />
      </Paper>
    </Container>
  );
}
