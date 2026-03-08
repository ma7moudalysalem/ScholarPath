import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Button,
  ToggleButtonGroup,
  ToggleButton,
  FormControl,
  Select,
  MenuItem,
  Skeleton,
  Stack,
  useMediaQuery,
  useTheme,
  Card,
  CardContent,
  Chip,
  IconButton,
  Tooltip,
  type SelectChangeEvent,
} from '@mui/material';
import ViewColumnIcon from '@mui/icons-material/ViewColumn';
import ViewListIcon from '@mui/icons-material/ViewList';
import SchoolOutlinedIcon from '@mui/icons-material/SchoolOutlined';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import { ApplicationStatus, type ApplicationListItemDto } from '@/types';
import { applicationService } from '@/services/applicationService';
import KanbanBoard from '@/components/tracker/KanbanBoard';
import CardDetailDrawer from '@/components/tracker/CardDetailDrawer';
import { formatDistanceToNow } from '@/utils/dateUtils';

const STATUS_OPTIONS = [
  { value: ApplicationStatus.Planned, labelKey: 'tracker.planned' },
  { value: ApplicationStatus.Applied, labelKey: 'tracker.applied' },
  { value: ApplicationStatus.Pending, labelKey: 'tracker.pending' },
  { value: ApplicationStatus.Accepted, labelKey: 'tracker.accepted' },
  { value: ApplicationStatus.Rejected, labelKey: 'tracker.rejected' },
];

function getDeadlineColor(days: number | null, isOverdue: boolean): 'error' | 'warning' | 'default' {
  if (isOverdue) return 'error';
  if (days !== null && days <= 7) return 'warning';
  return 'default';
}

export default function Tracker() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();

  const statusFilter = searchParams.get('status');

  const [sortBy, setSortBy] = useState<string>('deadline');
  const [viewMode, setViewMode] = useState<'kanban' | 'list'>(isMobile ? 'list' : 'kanban');
  const [selectedApp, setSelectedApp] = useState<ApplicationListItemDto | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Fetch all applications (no status filter for kanban, use it for list if provided)
  const { data, isLoading } = useQuery({
    queryKey: ['applications', sortBy],
    queryFn: () =>
      applicationService.getApplications({
        sortBy,
        pageSize: 200, // Get all for kanban view
      }),
  });

  const applications = data?.items ?? [];

  // Filter if status param present and in list view
  const filteredApplications =
    statusFilter && viewMode === 'list'
      ? applications.filter(
          (a) => ApplicationStatus[a.status] === statusFilter,
        )
      : applications;

  // Mutations
  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationService.updateStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => applicationService.deleteApplication(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const handleStatusChange = useCallback(
    (id: string, status: ApplicationStatus) => {
      statusMutation.mutate({ id, status });
    },
    [statusMutation],
  );

  const handleDelete = useCallback(
    (id: string) => {
      deleteMutation.mutate(id);
    },
    [deleteMutation],
  );

  const handleCardClick = useCallback((app: ApplicationListItemDto) => {
    setSelectedApp(app);
    setDrawerOpen(true);
  }, []);

  const handleDrawerClose = () => {
    setDrawerOpen(false);
    setSelectedApp(null);
  };

  const handleDrawerDeleted = () => {
    setDrawerOpen(false);
    setSelectedApp(null);
  };

  const handleViewChange = (_: React.MouseEvent<HTMLElement>, value: 'kanban' | 'list' | null) => {
    if (value) setViewMode(value);
  };

  // Loading skeleton
  if (isLoading) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          {t('tracker.title')}
        </Typography>
        <Stack spacing={2}>
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} variant="rounded" height={100} />
          ))}
        </Stack>
      </Box>
    );
  }

  // Empty state
  if (applications.length === 0) {
    return (
      <Box>
        <Typography variant="h4" gutterBottom>
          {t('tracker.title')}
        </Typography>
        <Box sx={{ textAlign: 'center', mt: 8 }}>
          <SchoolOutlinedIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
          <Typography variant="h6" gutterBottom>
            {t('tracker.noApplications')}
          </Typography>
          <Button variant="contained" onClick={() => navigate('/scholarships')} sx={{ mt: 2 }}>
            {t('tracker.browseScholarships')}
          </Button>
        </Box>
      </Box>
    );
  }

  return (
    <Box>
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 2,
          mb: 3,
        }}
      >
        <Typography variant="h4">{t('tracker.title')}</Typography>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          {/* Sort */}
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <Select
              value={sortBy}
              onChange={(e: SelectChangeEvent) => setSortBy(e.target.value)}
            >
              <MenuItem value="deadline">{t('tracker.sortByDeadline')}</MenuItem>
              <MenuItem value="updatedAt">{t('tracker.sortByUpdated')}</MenuItem>
            </Select>
          </FormControl>

          {/* View toggle (desktop only) */}
          {!isMobile && (
            <ToggleButtonGroup
              value={viewMode}
              exclusive
              onChange={handleViewChange}
              size="small"
            >
              <ToggleButton value="kanban">
                <ViewColumnIcon />
              </ToggleButton>
              <ToggleButton value="list">
                <ViewListIcon />
              </ToggleButton>
            </ToggleButtonGroup>
          )}
        </Box>
      </Box>

      {/* Content */}
      {viewMode === 'kanban' && !isMobile ? (
        <KanbanBoard
          applications={filteredApplications}
          onStatusChange={handleStatusChange}
          onDelete={handleDelete}
          onCardClick={handleCardClick}
        />
      ) : (
        /* List view */
        <Stack spacing={1.5}>
          {filteredApplications.map((app) => {
            const appTitle =
              i18n.language === 'ar' && app.scholarshipTitleAr
                ? app.scholarshipTitleAr
                : app.scholarshipTitle;

            return (
              <Card
                key={app.id}
                sx={{
                  cursor: 'pointer',
                  transition: 'box-shadow 0.15s',
                  '&:hover': { boxShadow: 4 },
                }}
                onClick={() => handleCardClick(app)}
              >
                <CardContent
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 2,
                    p: 2,
                    '&:last-child': { pb: 2 },
                    flexWrap: { xs: 'wrap', sm: 'nowrap' },
                  }}
                >
                  {/* Info */}
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="subtitle2" sx={{ fontWeight: 600 }} noWrap>
                      {appTitle}
                    </Typography>
                    {app.providerName && (
                      <Typography variant="caption" color="text.secondary">
                        {app.providerName}
                      </Typography>
                    )}
                  </Box>

                  {/* Badges */}
                  <Box sx={{ display: 'flex', gap: 0.5, alignItems: 'center', flexShrink: 0 }}>
                    {app.isOverdue && (
                      <Chip label={t('tracker.overdue')} color="error" size="small" />
                    )}
                    {app.deadlineCountdownDays !== null && !app.isOverdue && (
                      <Chip
                        label={t('tracker.daysBefore', { count: app.deadlineCountdownDays })}
                        color={getDeadlineColor(app.deadlineCountdownDays, false)}
                        size="small"
                        variant="outlined"
                      />
                    )}
                    {app.hasReminders && (
                      <NotificationsActiveIcon
                        sx={{ fontSize: 16, color: 'primary.main' }}
                      />
                    )}
                  </Box>

                  {/* Status dropdown */}
                  <Box onClick={(e) => e.stopPropagation()} sx={{ flexShrink: 0 }}>
                    <Select
                      size="small"
                      value={app.status}
                      onChange={(e: SelectChangeEvent<number>) =>
                        handleStatusChange(app.id, e.target.value as ApplicationStatus)
                      }
                      sx={{ minWidth: 120, fontSize: '0.8rem' }}
                    >
                      {STATUS_OPTIONS.map((opt) => (
                        <MenuItem key={opt.value} value={opt.value} sx={{ fontSize: '0.8rem' }}>
                          {t(opt.labelKey)}
                        </MenuItem>
                      ))}
                    </Select>
                  </Box>

                  {/* Updated */}
                  {app.updatedAt && (
                    <Typography
                      variant="caption"
                      color="text.disabled"
                      sx={{ flexShrink: 0, display: { xs: 'none', sm: 'block' } }}
                    >
                      {t('tracker.lastUpdated', { time: formatDistanceToNow(app.updatedAt) })}
                    </Typography>
                  )}

                  {/* Delete */}
                  <Box onClick={(e) => e.stopPropagation()} sx={{ flexShrink: 0 }}>
                    <Tooltip title={t('tracker.deleteApplication')}>
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDelete(app.id)}
                      >
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Box>
                </CardContent>
              </Card>
            );
          })}
        </Stack>
      )}

      {/* Detail Drawer */}
      <CardDetailDrawer
        application={selectedApp}
        open={drawerOpen}
        onClose={handleDrawerClose}
        onDeleted={handleDrawerDeleted}
      />
    </Box>
  );
}
