import { Stack, Typography } from "@mui/material";
import { School } from "@mui/icons-material";
import { useNavigate } from "react-router-dom";

interface LogoProps {
  color?: "primary" | "secondary" | "action" | "disabled" | "error" | "info" | "success" | "warning";
  variant?: "h4" | "h5" | "h6";
  clickable?: boolean;
}

export const Logo = ({ color = "primary", variant = "h6", clickable = true }: LogoProps) => {
  const navigate = useNavigate();

  return (
    <Stack
      direction="row"
      spacing={1}
      alignItems="center"
      onClick={() => clickable && navigate("/")}
      sx={{ cursor: clickable ? "pointer" : "default" }}
    >
      <School color={color} fontSize="large" />
      <Typography variant={variant} fontWeight={800} color={`${color}.main`}>
        ScholarPath
      </Typography>
    </Stack>
  );
};
