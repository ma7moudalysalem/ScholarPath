import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  Paper,
  Typography,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Skeleton,
} from '@mui/material';
import LightbulbOutlinedIcon from '@mui/icons-material/LightbulbOutlined';

function getActionLink(action: string): string {
  const lower = action.toLowerCase();
  if (lower.includes('complete') && lower.includes('profile')) return '/profile';
  if (lower.includes('start tracking')) return '/scholarships';
  if (lower.includes('browse') && lower.includes('save')) return '/scholarships';
  if (lower.includes('deadline') && lower.includes('reminder')) return '/dashboard/tracker';
  if (lower.includes('browse')) return '/scholarships';
  return '/dashboard';
}

interface ActionsWidgetProps {
  actions: string[];
  isLoading: boolean;
}

export default function ActionsWidget({ actions, isLoading }: ActionsWidgetProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <Paper sx={{ p: 3 }}>
        <Skeleton width={200} height={32} sx={{ mb: 2 }} />
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} height={48} sx={{ mb: 1 }} />
        ))}
      </Paper>
    );
  }

  if (actions.length === 0) return null;

  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        {t('dashboard.recommendedActions')}
      </Typography>

      <List disablePadding>
        {actions.map((action, index) => (
          <ListItemButton
            key={index}
            onClick={() => navigate(getActionLink(action))}
            sx={{ borderRadius: 1, mb: 0.5 }}
          >
            <ListItemIcon sx={{ minWidth: 36 }}>
              <LightbulbOutlinedIcon color="warning" />
            </ListItemIcon>
            <ListItemText primary={action} />
          </ListItemButton>
        ))}
      </List>
    </Paper>
  );
}
