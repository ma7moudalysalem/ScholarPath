import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { alpha } from '@mui/material/styles';
import { Box, Typography, Skeleton, useTheme } from '@mui/material';
import Grid from '@mui/material/Grid2';

const SERIF = '"Cormorant Garamond", "Georgia", serif';

interface StatusTileConfig {
  key: string;
  i18nKey: string;
  color: string;
  darkColor: string;
}

const STATUS_TILES: StatusTileConfig[] = [
  { key: 'Saved', i18nKey: 'dashboard.saved', color: '#2D5BE3', darkColor: '#4F7EF5' },
  { key: 'Planned', i18nKey: 'dashboard.planned', color: '#5A6878', darkColor: '#8A9BB5' },
  { key: 'Applied', i18nKey: 'dashboard.applied', color: '#0369A1', darkColor: '#38BDF8' },
  { key: 'Pending', i18nKey: 'dashboard.pending', color: '#B45309', darkColor: '#F59E0B' },
  { key: 'Accepted', i18nKey: 'dashboard.accepted', color: '#047857', darkColor: '#34D399' },
  { key: 'Rejected', i18nKey: 'dashboard.rejected', color: '#B91C1C', darkColor: '#F87171' },
];

interface StatusTilesProps {
  statusCounts: Record<string, number>;
  isLoading: boolean;
}

export default function StatusTiles({ statusCounts, isLoading }: StatusTilesProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const primary = theme.palette.primary.main;

  if (isLoading) {
    return (
      <Grid container spacing={2}>
        {STATUS_TILES.map((tile) => (
          <Grid key={tile.key} size={{ xs: 6, sm: 4, md: 2 }}>
            <Skeleton variant="rounded" height={108} sx={{ borderRadius: 3 }} />
          </Grid>
        ))}
      </Grid>
    );
  }

  return (
    <Grid container spacing={2}>
      {STATUS_TILES.map((tile) => {
        const count = statusCounts[tile.key] ?? 0;
        const accentColor = isDark ? tile.darkColor : tile.color;

        return (
          <Grid key={tile.key} size={{ xs: 6, sm: 4, md: 2 }}>
            <Box
              onClick={() => {
                // 'Saved' is not a tracker status — route to the saved scholarships list instead
                if (tile.key === 'Saved') {
                  navigate('/scholarships?savedOnly=true');
                } else {
                  navigate(`/dashboard/tracker?status=${tile.key}`);
                }
              }}
              sx={{
                p: 2.5,
                borderRadius: 3,
                cursor: 'pointer',
                position: 'relative',
                overflow: 'hidden',
                border: `1px solid ${alpha(primary, 0.09)}`,
                bgcolor: 'background.paper',
                transition: 'all 0.25s ease',
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  top: 0,
                  left: 0,
                  right: 0,
                  height: '3px',
                  background: accentColor,
                  borderRadius: '3px 3px 0 0',
                },
                '&:hover': {
                  transform: 'translateY(-3px)',
                  borderColor: alpha(accentColor, 0.35),
                  boxShadow: isDark
                    ? '0 10px 28px rgba(0,0,0,0.45)'
                    : '0 8px 24px rgba(0,0,0,0.09)',
                },
              }}
            >
              <Typography
                sx={{
                  fontFamily: SERIF,
                  fontSize: '2.6rem',
                  fontWeight: 700,
                  color: accentColor,
                  lineHeight: 1,
                  mb: 0.75,
                }}
              >
                {count}
              </Typography>
              <Typography
                variant="caption"
                sx={{
                  color: 'text.secondary',
                  fontWeight: 500,
                  letterSpacing: '0.03em',
                  display: 'block',
                }}
              >
                {t(tile.i18nKey)}
              </Typography>
            </Box>
          </Grid>
        );
      })}
    </Grid>
  );
}
