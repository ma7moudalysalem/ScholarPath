import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Card,
  CardContent,
  Typography,
  Chip,
  Box,
  IconButton,
  Select,
  MenuItem,
  Tooltip,
  type SelectChangeEvent,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive';
import { ApplicationStatus, type ApplicationListItemDto } from '@/types';
import { formatDistanceToNow } from '@/utils/dateUtils';

interface TrackerCardProps {
  application: ApplicationListItemDto;
  onStatusChange: (id: string, status: ApplicationStatus) => void;
  onDelete: (id: string) => void;
  onClick: (application: ApplicationListItemDto) => void;
}

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

export default function TrackerCard({
  application,
  onStatusChange,
  onDelete,
  onClick,
}: TrackerCardProps) {
  const { t, i18n } = useTranslation();
  const [hovered, setHovered] = useState(false);

  const title =
    i18n.language === 'ar' && application.scholarshipTitleAr
      ? application.scholarshipTitleAr
      : application.scholarshipTitle;

  const handleStatusChange = (e: SelectChangeEvent<number>) => {
    e.stopPropagation();
    onStatusChange(application.id, e.target.value as ApplicationStatus);
  };

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    onDelete(application.id);
  };

  return (
    <Card
      sx={{
        cursor: 'pointer',
        transition: 'box-shadow 0.15s, transform 0.15s',
        '&:hover': {
          boxShadow: 4,
          transform: 'translateY(-1px)',
        },
        position: 'relative',
      }}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onClick={() => onClick(application)}
    >
      <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
        {/* Title */}
        <Typography
          variant="subtitle2"
          sx={{
            fontWeight: 600,
            mb: 0.5,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
          }}
        >
          {title}
        </Typography>

        {/* Provider */}
        {application.providerName && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
            {application.providerName}
          </Typography>
        )}

        {/* Deadline chip */}
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mb: 1 }}>
          {application.isOverdue && (
            <Chip label={t('tracker.overdue')} color="error" size="small" />
          )}
          {application.deadlineCountdownDays !== null && !application.isOverdue && (
            <Chip
              label={t('tracker.daysBefore', { count: application.deadlineCountdownDays })}
              color={getDeadlineColor(application.deadlineCountdownDays, false)}
              size="small"
              variant="outlined"
            />
          )}
          {application.hasReminders && (
            <Tooltip title={t('tracker.reminders')}>
              <NotificationsActiveIcon
                sx={{ fontSize: 16, color: 'primary.main', alignSelf: 'center' }}
              />
            </Tooltip>
          )}
        </Box>

        {/* Notes preview */}
        {application.notesPreview && (
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{
              display: 'block',
              mb: 1,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {application.notesPreview}
          </Typography>
        )}

        {/* Updated at */}
        {application.updatedAt && (
          <Typography variant="caption" color="text.disabled" sx={{ display: 'block' }}>
            {t('tracker.lastUpdated', { time: formatDistanceToNow(application.updatedAt) })}
          </Typography>
        )}

        {/* Quick actions on hover */}
        {hovered && (
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              mt: 1,
              pt: 1,
              borderTop: 1,
              borderColor: 'divider',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <Select
              size="small"
              value={application.status}
              onChange={handleStatusChange}
              sx={{ flex: 1, fontSize: '0.75rem' }}
            >
              {STATUS_OPTIONS.map((opt) => (
                <MenuItem key={opt.value} value={opt.value} sx={{ fontSize: '0.75rem' }}>
                  {t(opt.labelKey)}
                </MenuItem>
              ))}
            </Select>
            <Tooltip title={t('tracker.deleteApplication')}>
              <IconButton size="small" color="error" onClick={handleDelete}>
                <DeleteOutlineIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        )}
      </CardContent>
    </Card>
  );
}
