import { useState } from "react";
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Alert,
  Box,
  IconButton,
  InputAdornment,
  LinearProgress
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";

export default function ResetPassword() {
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [show, setShow] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  const passwordStrength = Math.min(password.length * 10, 100);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (password.length < 8) {
      setError("Password must be at least 8 characters");
      return;
    }

    if (password !== confirm) {
      setError("Passwords do not match");
      return;
    }

    setSuccess(true);
  };

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Typography variant="h5" gutterBottom>
          Create New Password
        </Typography>

        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="New Password"
            type={show ? "text" : "password"}
            margin="normal"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={() => setShow(!show)}>
                    {show ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <LinearProgress
            variant="determinate"
            value={passwordStrength}
            sx={{ mt: 1, mb: 2 }}
          />

          <TextField
            fullWidth
            label="Confirm Password"
            type="password"
            margin="normal"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
          />

          {error && <Alert severity="error">{error}</Alert>}
          {success && (
            <Alert severity="success" sx={{ mt: 2 }}>
              Password reset successfully 🎉
            </Alert>
          )}

          <Button
            fullWidth
            variant="contained"
            type="submit"
            sx={{ mt: 3 }}
          >
            Reset Password
          </Button>
        </Box>
      </Paper>
    </Container>
  );
}
