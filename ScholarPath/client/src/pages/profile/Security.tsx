import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  Stack,
  Alert,
  InputAdornment,
  IconButton,
} from '@mui/material';
import { Visibility, VisibilityOff, LockOutlined, VpnKeyOutlined } from '@mui/icons-material';
import { useAuthStore } from '@/stores/authStore';

export default function Security() {
  const { t } = useTranslation();
  const user = useAuthStore((state) => state.user);

  // Assuming user object has a property to determine if they are an SSO user
  // For example: user?.provider !== 'local' or user?.isSSO
  // Since we don't have the exact property, we'll mock it or use a default
  // Example: const isSSO = user?.provider && user?.provider !== 'local';
  const isSSO = user?.authProvider && user?.authProvider !== 'local'; // Adjust 'authProvider' based on your actual User type

  const [form, setForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const [showPassword, setShowPassword] = useState({
    current: false,
    new: false,
    confirm: false,
  });

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleChange = (field: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [field]: e.target.value });
    setError(null);
    setSuccess(false);
  };

  const handleTogglePassword = (field: keyof typeof showPassword) => {
    setShowPassword((prev) => ({ ...prev, [field]: !prev[field] }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    // Validations
    if (!isSSO && !form.currentPassword) {
      setError('Current password is required.');
      return;
    }

    if (!form.newPassword) {
      setError('New password is required.');
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    if (!isSSO && form.currentPassword === form.newPassword) {
      setError('New password cannot be the same as the current password.');
      return;
    }

    if (form.newPassword.length < 8) {
      setError('Password must be at least 8 characters long.');
      return;
    }

    // Submit logic here
    console.log('Submitting password change...', form);
    setSuccess(true);
    setForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom fontWeight="bold" sx={{ mb: 4 }}>
        {t('security')}
      </Typography>

      <Paper sx={{ p: 4, maxWidth: 600, borderRadius: 2 }}>
        <Stack direction="row" spacing={2} alignItems="center" mb={3}>
          {isSSO ? <VpnKeyOutlined color="primary" /> : <LockOutlined color="primary" />}
          <Typography variant="h6" fontWeight="600">
            {isSSO ? 'Set Password' : 'Change Password'}
          </Typography>
        </Stack>

        <Typography variant="body2" color="text.secondary" mb={4}>
          {isSSO
            ? 'Since you logged in using a social account, you can set a password here to also login with email and password in the future.'
            : 'Update your password regularly to keep your account secure.'}
        </Typography>

        {error && (
          <Alert severity="error" sx={{ mb: 3 }}>
            {error}
          </Alert>
        )}

        {success && (
          <Alert severity="success" sx={{ mb: 3 }}>
            {isSSO ? 'Password set successfully.' : 'Password changed successfully.'}
          </Alert>
        )}

        <Box component="form" onSubmit={handleSubmit}>
          {!isSSO && (
            <TextField
              fullWidth
              label="Current Password"
              type={showPassword.current ? 'text' : 'password'}
              value={form.currentPassword}
              onChange={handleChange('currentPassword')}
              margin="normal"
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton onClick={() => handleTogglePassword('current')} edge="end">
                      {showPassword.current ? <VisibilityOff /> : <Visibility />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
          )}

          <TextField
            fullWidth
            label="New Password"
            type={showPassword.new ? 'text' : 'password'}
            value={form.newPassword}
            onChange={handleChange('newPassword')}
            margin="normal"
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={() => handleTogglePassword('new')} edge="end">
                    {showPassword.new ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <TextField
            fullWidth
            label="Confirm New Password"
            type={showPassword.confirm ? 'text' : 'password'}
            value={form.confirmPassword}
            onChange={handleChange('confirmPassword')}
            margin="normal"
            helperText="Minimum 8 characters."
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={() => handleTogglePassword('confirm')} edge="end">
                    {showPassword.confirm ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <Button type="submit" variant="contained" color="primary" sx={{ mt: 3 }}>
            {isSSO ? 'Save Password' : 'Update Password'}
          </Button>
        </Box>
      </Paper>
    </Box>
  );
}
