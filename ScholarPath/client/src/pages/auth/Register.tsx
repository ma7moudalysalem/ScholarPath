import { useState } from "react";
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Checkbox,
  FormControlLabel,
  Alert,
  IconButton,
  InputAdornment,
  LinearProgress
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";

export default function Register() {
  const [form, setForm] = useState({
    fullName: "",
    email: "",
    password: "",
    confirm: "",
    terms: false,
  });

  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const completion =
    [form.fullName, form.email, form.password, form.confirm].filter(Boolean)
      .length * 25;

  const handleChange = (field: string, value: any) => {
    setForm({ ...form, [field]: value });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!form.fullName || !form.email) {
      setError("All fields are required");
      return;
    }

    if (form.password.length < 6) {
      setError("Password must be at least 6 characters");
      return;
    }

    if (form.password !== form.confirm) {
      setError("Passwords do not match");
      return;
    }

    if (!form.terms) {
      setError("You must agree to Terms & Conditions");
      return;
    }

    setLoading(true);

    setTimeout(() => {
      alert("Account Created 🎉");
      setLoading(false);
    }, 1500);
  };

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4 }}>

        <Typography variant="h5" gutterBottom>
          Create Student Account
        </Typography>

        <Typography variant="body2" sx={{ mb: 1 }}>
          Profile Completion: {completion}%
        </Typography>

        <LinearProgress
          variant="determinate"
          value={completion}
          sx={{ mb: 3 }}
        />

        <Box component="form" onSubmit={handleSubmit}>

          <TextField
            fullWidth
            label="Full Name"
            margin="normal"
            value={form.fullName}
            onChange={(e) => handleChange("fullName", e.target.value)}
          />

          <TextField
            fullWidth
            label="Email"
            type="email"
            margin="normal"
            value={form.email}
            onChange={(e) => handleChange("email", e.target.value)}
          />

          <TextField
            fullWidth
            label="Password"
            type={showPassword ? "text" : "password"}
            margin="normal"
            value={form.password}
            onChange={(e) => handleChange("password", e.target.value)}
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    onClick={() => setShowPassword(!showPassword)}
                  >
                    {showPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          <TextField
            fullWidth
            label="Confirm Password"
            type="password"
            margin="normal"
            value={form.confirm}
            onChange={(e) => handleChange("confirm", e.target.value)}
          />

          <FormControlLabel
            control={
              <Checkbox
                checked={form.terms}
                onChange={(e) =>
                  handleChange("terms", e.target.checked)
                }
              />
            }
            label="I agree to Terms & Privacy Policy"
          />

          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}

          <Button
            fullWidth
            variant="contained"
            type="submit"
            disabled={loading}
            sx={{ mt: 3 }}
          >
            {loading ? "Creating..." : "Complete Registration"}
          </Button>

        </Box>
      </Paper>
    </Container>
  );
}