import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Paper, Typography, Skeleton } from '@mui/material';
import Grid from '@mui/material/Grid2';

interface StatusTileConfig {
  key: string;
  i18nKey: string;
  color: string;
}

const STATUS_TILES: StatusTileConfig[] = [
  { key: 'Saved', i18nKey: 'dashboard.saved', color: '#1976d2' },
  { key: 'Planned', i18nKey: 'dashboard.planned', color: '#757575' },
  { key: 'Applied', i18nKey: 'dashboard.applied', color: '#0288d1' },
  { key: 'Pending', i18nKey: 'dashboard.pending', color: '#ed6c02' },
  { key: 'Accepted', i18nKey: 'dashboard.accepted', color: '#2e7d32' },
  { key: 'Rejected', i18nKey: 'dashboard.rejected', color: '#d32f2f' },
];

interface StatusTilesProps {
  statusCounts: Record<string, number>;
  isLoading: boolean;
}

export default function StatusTiles({ statusCounts, isLoading }: StatusTilesProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  if (isLoading) {
    return (
      <Grid container spacing={2}>
        {STATUS_TILES.map((tile) => (
          <Grid key={tile.key} size={{ xs: 6, sm: 4, md: 2 }}>
            <Skeleton variant="rounded" height={100} />
          </Grid>
        ))}
      </Grid>
    );
  }

  return (
    <Grid container spacing={2}>
      {STATUS_TILES.map((tile) => {
        const count = statusCounts[tile.key] ?? 0;
        return (
          <Grid key={tile.key} size={{ xs: 6, sm: 4, md: 2 }}>
            <Paper
              sx={{
                p: 2.5,
                textAlign: 'center',
                cursor: 'pointer',
                borderTop: 3,
                borderColor: tile.color,
                transition: 'transform 0.15s, box-shadow 0.15s',
                '&:hover': {
                  transform: 'translateY(-2px)',
                  boxShadow: 4,
                },
              }}
              onClick={() => navigate(`/dashboard/tracker?status=${tile.key}`)}
            >
              <Typography
                variant="h3"
                sx={{ color: tile.color, fontWeight: 700 }}
              >
                {count}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                {t(tile.i18nKey)}
              </Typography>
            </Paper>
          </Grid>
        );
      })}
    </Grid>
  );
}
