import { useState } from "react";
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Divider,
  IconButton,
  InputAdornment,
  Checkbox,
  FormControlLabel,
  Stack,
  CircularProgress
} from "@mui/material";
import { Visibility, VisibilityOff } from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import googleIcon from "@/assets/google-icon-logo-svgrepo-com.svg";
import appleIcon from "@/assets/apple-logo-svgrepo-com.svg";
import microsoftIcon from "@/assets/microsoft-logo-svgrepo-com.svg";
import { Logo } from "@/components/common/Logo";

export default function Login() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    email: "",
    password: "",
    remember: false,
  });

  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleChange = (field: string, value: string | boolean) => {
    setForm({ ...form, [field]: value });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    setTimeout(() => {
      setLoading(false);
      navigate("/dashboard"); // بعد النجاح
    }, 1500);
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "linear-gradient(135deg,#f6f7f8,#e3f2fd)",
        p: 2,
      }}
    >
      <Paper
        elevation={6}
        sx={{
          width: "100%",
          maxWidth: 480,
          p: 4,
          borderRadius: 3,
        }}
      >
        {/* Header */}
        <Box mb={3}>
          <Logo />
        </Box>

        <Typography variant="h5" fontWeight={700}>
          Welcome back
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={3}>
          Log in to your account to continue your journey
        </Typography>

        {/* Social Buttons */}
        <Stack spacing={2} mb={3}>
          {/* Google */}
          <Button
            fullWidth
            variant="outlined"
            onClick={() => console.log('Google login clicked')}
            sx={{
              height: 48,
              borderRadius: 2,
              textTransform: "none",
              fontWeight: 500,
              borderColor: "#e2e8f0",
              backgroundColor: "white",
              justifyContent: "center",
              gap: 1.5,
              "&:hover": {
                backgroundColor: "#f8fafc",
                borderColor: "#cbd5e1",
              },
            }}
          >
            <img
              src={googleIcon}
              alt="google"
              width={24}
              height={24}
            />
            Continue with Google
          </Button>

          {/* Apple */}
          <Button
            fullWidth
            variant="outlined"
            onClick={() => console.log('Apple login clicked')}
            sx={{
              height: 48,
              borderRadius: 2,
              textTransform: "none",
              fontWeight: 500,
              borderColor: "#e2e8f0",
              backgroundColor: "white",
              justifyContent: "center",
              gap: 1.5,
              "&:hover": {
                backgroundColor: "#f8fafc",
              },
            }}
          >
            <img
              src={appleIcon}
              alt="apple"
              width={24}
              height={24}
            />
            Continue with Apple
          </Button>

          {/* Microsoft */}
          <Button
            fullWidth
            variant="outlined"
            onClick={() => console.log('Microsoft login clicked')}
            sx={{
              height: 48,
              borderRadius: 2,
              textTransform: "none",
              fontWeight: 500,
              borderColor: "#e2e8f0",
              backgroundColor: "white",
              justifyContent: "center",
              gap: 1.5,
              "&:hover": {
                backgroundColor: "#f8fafc",
              },
            }}
          >
            <img
              src={microsoftIcon}
              alt="microsoft"
              width={24}
              height={24}
            />
            Continue with Microsoft
          </Button>

        </Stack>
        <Divider sx={{ mb: 3 }}>OR</Divider>

        {/* Form */}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Email or Username"
            margin="normal"
            value={form.email}
            onChange={(e) => handleChange("email", e.target.value)}
          />

          <TextField
            fullWidth
            label="Password"
            margin="normal"
            type={showPassword ? "text" : "password"}
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

          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            mt={1}
          >
            <FormControlLabel
              control={
                <Checkbox
                  checked={form.remember}
                  onChange={(e) =>
                    handleChange("remember", e.target.checked)
                  }
                />
              }
              label="Keep me logged in"
            />

            <Button
              size="small"
              onClick={() => navigate("/forgot-password")}
            >
              Forgot Password?
            </Button>
          </Stack>

          <Button
            fullWidth
            variant="contained"
            type="submit"
            sx={{ mt: 3, height: 48 }}
            disabled={loading}
          >
            {loading ? (
              <>
                <CircularProgress size={20} sx={{ mr: 1 }} />
                Logging in...
              </>
            ) : (
              "Login"
            )}
          </Button>

          <Typography
            variant="body2"
            textAlign="center"
            mt={3}
          >
            Don't have an account?{" "}
            <Button size="small" onClick={() => navigate("/register")}>
              Sign up
            </Button>
          </Typography>
        </Box>
      </Paper>
    </Box>
  );
}