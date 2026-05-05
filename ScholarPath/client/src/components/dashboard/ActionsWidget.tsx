import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { alpha } from '@mui/material/styles';
import { Box, Typography, Skeleton, useTheme } from '@mui/material';
import { ArrowForwardIos } from '@mui/icons-material';

// Maps backend i18n action keys (returned by the API) to their navigation targets
const ACTION_ROUTES: Record<string, string> = {
  'action.completeProfile': '/profile',
  'action.startTracking': '/scholarships',
  'action.browseAndSave': '/scholarships',
  'action.setReminders': '/dashboard/tracker',
};

function getActionLink(actionKey: string): string {
  return ACTION_ROUTES[actionKey] ?? '/dashboard';
}

interface ActionsWidgetProps {
  actions: string[];
  isLoading: boolean;
}

export default function ActionsWidget({ actions, isLoading }: ActionsWidgetProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const primary = theme.palette.primary.main;
  const displayFont = theme.typography.h2.fontFamily as string;

  if (isLoading) {
    return (
      <Box
        sx={{
          p: 3,
          borderRadius: 3,
          border: `1px solid ${alpha(primary, 0.09)}`,
          bgcolor: 'background.paper',
        }}
      >
        <Skeleton width={160} height={28} sx={{ mb: 2.5 }} />
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} height={52} sx={{ mb: 1, borderRadius: 2 }} />
        ))}
      </Box>
    );
  }

  if (actions.length === 0) return null;

  return (
    <Box
      sx={{
        p: 3,
        borderRadius: 3,
        border: `1px solid ${alpha(primary, 0.09)}`,
        bgcolor: 'background.paper',
      }}
    >
      <Typography variant="h6" sx={{ fontWeight: 600, mb: 3 }}>
        {t('dashboard.recommendedActions')}
      </Typography>

      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        {actions.map((action, index) => (
          <Box
            key={index}
            onClick={() => navigate(getActionLink(action))}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2,
              p: '12px 16px',
              borderRadius: 2.5,
              cursor: 'pointer',
              border: `1px solid ${alpha(primary, 0.09)}`,
              transition: 'all 0.2s ease',
              '&:hover': {
                bgcolor: alpha(primary, 0.07),
                borderColor: alpha(primary, 0.24),
                transform: 'translateX(2px)',
              },
            }}
          >
            {/* Index bullet */}
            <Box
              sx={{
                width: 28,
                height: 28,
                borderRadius: '50%',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                bgcolor: alpha(primary, 0.1),
                flexShrink: 0,
              }}
            >
              <Typography
                sx={{
                  fontFamily: displayFont,
                  fontSize: '0.9rem',
                  fontWeight: 700,
                  color: primary,
                  lineHeight: 1,
                }}
              >
                {index + 1}
              </Typography>
            </Box>
            <Typography variant="body2" sx={{ flex: 1, fontWeight: 400, lineHeight: 1.5 }}>
              {t(action)}
            </Typography>
            <ArrowForwardIos
              sx={{ fontSize: 12, color: 'text.secondary', opacity: 0.4, flexShrink: 0 }}
            />
          </Box>
        ))}
      </Box>
    </Box>
  );
}
