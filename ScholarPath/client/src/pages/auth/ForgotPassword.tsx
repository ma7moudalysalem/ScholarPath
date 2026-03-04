import { useState } from "react";
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Alert,
  Box
} from "@mui/material";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSuccess(false);

    if (!email) {
      setError("Email is required");
      return;
    }

    setLoading(true);

    // Fake backend
    setTimeout(() => {
      setLoading(false);
      setSuccess(true);
    }, 1500);
  };

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Typography variant="h5" gutterBottom>
          Forgot Password
        </Typography>

        <Typography variant="body2" sx={{ mb: 3 }}>
          Enter your email to receive reset instructions.
        </Typography>

        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Email Address"
            type="email"
            margin="normal"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />

          {error && <Alert severity="error">{error}</Alert>}
          {success && (
            <Alert severity="success" sx={{ mt: 2 }}>
              If this email exists, a reset link has been sent.
            </Alert>
          )}

          <Button
            fullWidth
            variant="contained"
            type="submit"
            disabled={loading}
            sx={{ mt: 3 }}
          >
            {loading ? "Sending..." : "Send Reset Link"}
          </Button>
        </Box>
      </Paper>
    </Container>
  );
}