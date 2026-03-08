import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Box, Typography, Paper, Stack, Chip } from '@mui/material';
import { ApplicationStatus, type ApplicationListItemDto } from '@/types';
import TrackerCard from './TrackerCard';

interface KanbanBoardProps {
  applications: ApplicationListItemDto[];
  onStatusChange: (id: string, status: ApplicationStatus) => void;
  onDelete: (id: string) => void;
  onCardClick: (application: ApplicationListItemDto) => void;
}

interface ColumnConfig {
  status: ApplicationStatus;
  labelKey: string;
  color: string;
}

const COLUMNS: ColumnConfig[] = [
  { status: ApplicationStatus.Planned, labelKey: 'tracker.planned', color: '#757575' },
  { status: ApplicationStatus.Applied, labelKey: 'tracker.applied', color: '#0288d1' },
  { status: ApplicationStatus.Pending, labelKey: 'tracker.pending', color: '#ed6c02' },
  { status: ApplicationStatus.Accepted, labelKey: 'tracker.accepted', color: '#2e7d32' },
  { status: ApplicationStatus.Rejected, labelKey: 'tracker.rejected', color: '#d32f2f' },
];

export default function KanbanBoard({
  applications,
  onStatusChange,
  onDelete,
  onCardClick,
}: KanbanBoardProps) {
  const { t } = useTranslation();

  const grouped = useMemo(() => {
    const map = new Map<ApplicationStatus, ApplicationListItemDto[]>();
    for (const col of COLUMNS) {
      map.set(col.status, []);
    }
    for (const app of applications) {
      const list = map.get(app.status);
      if (list) {
        list.push(app);
      }
    }
    return map;
  }, [applications]);

  return (
    <Box
      sx={{
        display: 'flex',
        gap: 2,
        overflowX: 'auto',
        pb: 2,
        minHeight: 400,
      }}
    >
      {COLUMNS.map((col) => {
        const items = grouped.get(col.status) ?? [];
        return (
          <Paper
            key={col.status}
            variant="outlined"
            sx={{
              minWidth: 280,
              maxWidth: 280,
              flex: '0 0 280px',
              display: 'flex',
              flexDirection: 'column',
              bgcolor: 'action.hover',
            }}
          >
            {/* Column header */}
            <Box
              sx={{
                p: 1.5,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                borderBottom: 2,
                borderColor: col.color,
              }}
            >
              <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                {t(col.labelKey)}
              </Typography>
              <Chip label={items.length} size="small" sx={{ bgcolor: col.color, color: '#fff' }} />
            </Box>

            {/* Column body */}
            <Stack spacing={1.5} sx={{ p: 1.5, flex: 1, overflowY: 'auto' }}>
              {items.map((app) => (
                <TrackerCard
                  key={app.id}
                  application={app}
                  onStatusChange={onStatusChange}
                  onDelete={onDelete}
                  onClick={onCardClick}
                />
              ))}
              {items.length === 0 && (
                <Typography
                  variant="body2"
                  color="text.disabled"
                  sx={{ textAlign: 'center', mt: 4 }}
                >
                  --
                </Typography>
              )}
            </Stack>
          </Paper>
        );
      })}
    </Box>
  );
}
