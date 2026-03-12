import { useState, useEffect, useCallback, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Drawer,
  Box,
  Typography,
  IconButton,
  Select,
  MenuItem,
  TextField,
  Checkbox,
  FormControlLabel,
  Switch,
  Button,
  Divider,
  Stack,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogContentText,
  DialogActions,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  type SelectChangeEvent,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import OpenInNewIcon from '@mui/icons-material/OpenInNew';
import AddIcon from '@mui/icons-material/Add';
import { ApplicationStatus, type ApplicationListItemDto, type ChecklistItem } from '@/types';
import { applicationService } from '@/services/applicationService';

interface CardDetailDrawerProps {
  application: ApplicationListItemDto | null;
  open: boolean;
  onClose: () => void;
  onDeleted: () => void;
}

const STATUS_OPTIONS = [
  { value: ApplicationStatus.Planned, labelKey: 'tracker.planned' },
  { value: ApplicationStatus.Applied, labelKey: 'tracker.applied' },
  { value: ApplicationStatus.Pending, labelKey: 'tracker.pending' },
  { value: ApplicationStatus.Accepted, labelKey: 'tracker.accepted' },
  { value: ApplicationStatus.Rejected, labelKey: 'tracker.rejected' },
];

const REMINDER_PRESETS = [30, 14, 7, 3, 1];
const MAX_NOTES_LENGTH = 2000;

export default function CardDetailDrawer({
  application,
  open,
  onClose,
  onDeleted,
}: CardDetailDrawerProps) {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Local state
  const [notes, setNotes] = useState('');
  const [checklist, setChecklist] = useState<ChecklistItem[]>([]);
  const [newItemText, setNewItemText] = useState('');
  const [reminderPresets, setReminderPresets] = useState<number[]>([]);
  const [channels, setChannels] = useState({ inApp: true, email: false });
  const [paused, setPaused] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  // Debounce refs
  const notesTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const checklistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const reminderTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const title =
    i18n.language === 'ar' && application?.scholarshipTitleAr
      ? application.scholarshipTitleAr
      : (application?.scholarshipTitle ?? '');

  // Reset state when application changes
  useEffect(() => {
    if (application) {
      setNotes(application.notesPreview ?? '');
      setChecklist([]);
      setReminderPresets([]);
      setChannels({ inApp: true, email: false });
      setPaused(false);
      setNewItemText('');
    }
  }, [application]);

  // Cleanup timers on unmount
  useEffect(() => {
    return () => {
      if (notesTimerRef.current) clearTimeout(notesTimerRef.current);
      if (checklistTimerRef.current) clearTimeout(checklistTimerRef.current);
      if (reminderTimerRef.current) clearTimeout(reminderTimerRef.current);
    };
  }, []);

  // Mutations
  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ApplicationStatus }) =>
      applicationService.updateStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const notesMutation = useMutation({
    mutationFn: ({ id, notesVal }: { id: string; notesVal: string }) =>
      applicationService.updateNotes(id, notesVal),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const checklistMutation = useMutation({
    mutationFn: ({ id, items }: { id: string; items: ChecklistItem[] }) =>
      applicationService.updateChecklist(id, items),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const reminderMutation = useMutation({
    mutationFn: ({
      id,
      presets,
      ch,
      p,
    }: {
      id: string;
      presets: number[];
      ch: { inApp: boolean; email: boolean };
      p: boolean;
    }) => applicationService.updateReminders(id, { presets, channels: ch, paused: p }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['applications'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => applicationService.deleteApplication(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      onDeleted();
    },
  });

  // Handlers
  const handleStatusChange = (e: SelectChangeEvent<number>) => {
    if (!application) return;
    statusMutation.mutate({ id: application.id, status: e.target.value as ApplicationStatus });
  };

  const debouncedSaveNotes = useCallback(
    (notesVal: string) => {
      if (!application) return;
      if (notesTimerRef.current) clearTimeout(notesTimerRef.current);
      notesTimerRef.current = setTimeout(() => {
        notesMutation.mutate({ id: application.id, notesVal });
      }, 1000);
    },
    [application, notesMutation]
  );

  const handleNotesChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value.slice(0, MAX_NOTES_LENGTH);
    setNotes(val);
    debouncedSaveNotes(val);
  };

  const saveChecklist = useCallback(
    (items: ChecklistItem[]) => {
      if (!application) return;
      if (checklistTimerRef.current) clearTimeout(checklistTimerRef.current);
      checklistTimerRef.current = setTimeout(() => {
        checklistMutation.mutate({ id: application.id, items });
      }, 500);
    },
    [application, checklistMutation]
  );

  const handleCheckItem = (index: number) => {
    const updated = checklist.map((item, i) =>
      i === index ? { ...item, isChecked: !item.isChecked } : item
    );
    setChecklist(updated);
    saveChecklist(updated);
  };

  const handleDeleteChecklistItem = (index: number) => {
    const updated = checklist.filter((_, i) => i !== index);
    setChecklist(updated);
    saveChecklist(updated);
  };

  const handleAddChecklistItem = () => {
    if (!newItemText.trim()) return;
    const updated = [...checklist, { text: newItemText.trim(), isChecked: false }];
    setChecklist(updated);
    setNewItemText('');
    saveChecklist(updated);
  };

  const saveReminders = useCallback(
    (presets: number[], ch: { inApp: boolean; email: boolean }, p: boolean) => {
      if (!application) return;
      if (reminderTimerRef.current) clearTimeout(reminderTimerRef.current);
      reminderTimerRef.current = setTimeout(() => {
        reminderMutation.mutate({ id: application.id, presets, ch, p });
      }, 1000);
    },
    [application, reminderMutation]
  );

  const handlePresetToggle = (days: number) => {
    const updated = reminderPresets.includes(days)
      ? reminderPresets.filter((d) => d !== days)
      : [...reminderPresets, days];
    setReminderPresets(updated);
    saveReminders(updated, channels, paused);
  };

  const handleChannelToggle = (channel: 'inApp' | 'email') => {
    const updated = { ...channels, [channel]: !channels[channel] };
    setChannels(updated);
    saveReminders(reminderPresets, updated, paused);
  };

  const handlePauseToggle = () => {
    const updated = !paused;
    setPaused(updated);
    saveReminders(reminderPresets, channels, updated);
  };

  const handleDelete = () => {
    if (!application) return;
    deleteMutation.mutate(application.id);
    setDeleteDialogOpen(false);
  };

  const hasDeadline = application?.deadline !== null;

  return (
    <>
      <Drawer
        anchor="right"
        open={open}
        onClose={onClose}
        PaperProps={{ sx: { width: { xs: '100%', sm: 450 } } }}
      >
        {application && (
          <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            {/* Header */}
            <Box sx={{ p: 2, display: 'flex', alignItems: 'flex-start', gap: 1 }}>
              <Box sx={{ flex: 1 }}>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {title}
                </Typography>
                {application.providerName && (
                  <Typography variant="body2" color="text.secondary">
                    {application.providerName}
                  </Typography>
                )}
              </Box>
              <IconButton onClick={onClose} size="small">
                <CloseIcon />
              </IconButton>
            </Box>

            <Divider />

            {/* Scrollable content */}
            <Box sx={{ flex: 1, overflowY: 'auto', p: 2 }}>
              <Stack spacing={3}>
                {/* Status */}
                <Box>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    {t('scholarshipDetail.trackStatus')}
                  </Typography>
                  <Select
                    size="small"
                    fullWidth
                    value={application.status}
                    onChange={handleStatusChange}
                  >
                    {STATUS_OPTIONS.map((opt) => (
                      <MenuItem key={opt.value} value={opt.value}>
                        {t(opt.labelKey)}
                      </MenuItem>
                    ))}
                  </Select>
                </Box>

                {/* Notes */}
                <Box>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    {t('tracker.notes')}
                  </Typography>
                  <TextField
                    multiline
                    minRows={3}
                    maxRows={8}
                    fullWidth
                    size="small"
                    placeholder={t('tracker.notesPlaceholder')}
                    value={notes}
                    onChange={handleNotesChange}
                    inputProps={{ maxLength: MAX_NOTES_LENGTH }}
                  />
                  <Typography
                    variant="caption"
                    color="text.secondary"
                    sx={{ display: 'block', textAlign: 'right', mt: 0.5 }}
                  >
                    {t('tracker.charsRemaining', { count: MAX_NOTES_LENGTH - notes.length })}
                  </Typography>
                </Box>

                <Divider />

                {/* Checklist */}
                <Box>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    {t('tracker.checklist')}
                  </Typography>
                  <List dense disablePadding>
                    {checklist.map((item, index) => (
                      <ListItem
                        key={index}
                        disablePadding
                        secondaryAction={
                          <IconButton
                            edge="end"
                            size="small"
                            onClick={() => handleDeleteChecklistItem(index)}
                          >
                            <DeleteOutlineIcon fontSize="small" />
                          </IconButton>
                        }
                      >
                        <ListItemIcon sx={{ minWidth: 36 }}>
                          <Checkbox
                            edge="start"
                            checked={item.isChecked}
                            onChange={() => handleCheckItem(index)}
                            size="small"
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={item.text}
                          sx={{
                            textDecoration: item.isChecked ? 'line-through' : 'none',
                            color: item.isChecked ? 'text.disabled' : 'text.primary',
                          }}
                        />
                      </ListItem>
                    ))}
                  </List>
                  <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                    <TextField
                      size="small"
                      fullWidth
                      placeholder={t('tracker.addItem')}
                      value={newItemText}
                      onChange={(e) => setNewItemText(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          handleAddChecklistItem();
                        }
                      }}
                    />
                    <IconButton
                      color="primary"
                      onClick={handleAddChecklistItem}
                      disabled={!newItemText.trim()}
                    >
                      <AddIcon />
                    </IconButton>
                  </Box>
                </Box>

                <Divider />

                {/* Reminders */}
                <Box>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>
                    {t('tracker.reminders')}
                  </Typography>

                  {!hasDeadline ? (
                    <Typography variant="body2" color="text.secondary">
                      {t('tracker.noDeadline')}
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      {/* Preset checkboxes */}
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                        {REMINDER_PRESETS.map((days) => (
                          <FormControlLabel
                            key={days}
                            control={
                              <Checkbox
                                size="small"
                                checked={reminderPresets.includes(days)}
                                onChange={() => handlePresetToggle(days)}
                                disabled={paused}
                              />
                            }
                            label={
                              <Typography variant="body2">
                                {t('tracker.daysBefore', { count: days })}
                              </Typography>
                            }
                          />
                        ))}
                      </Box>

                      {/* Channel toggles */}
                      <Box sx={{ display: 'flex', gap: 2 }}>
                        <FormControlLabel
                          control={
                            <Checkbox
                              size="small"
                              checked={channels.inApp}
                              onChange={() => handleChannelToggle('inApp')}
                              disabled={paused}
                            />
                          }
                          label={<Typography variant="body2">{t('tracker.inApp')}</Typography>}
                        />
                        <FormControlLabel
                          control={
                            <Checkbox
                              size="small"
                              checked={channels.email}
                              onChange={() => handleChannelToggle('email')}
                              disabled={paused}
                            />
                          }
                          label={<Typography variant="body2">{t('tracker.email')}</Typography>}
                        />
                      </Box>

                      {/* Pause */}
                      <FormControlLabel
                        control={
                          <Switch size="small" checked={paused} onChange={handlePauseToggle} />
                        }
                        label={
                          <Typography variant="body2">{t('tracker.pauseReminders')}</Typography>
                        }
                      />

                      <Typography variant="caption" color="text.secondary">
                        {t('tracker.remindersTimezone')}
                      </Typography>
                    </Stack>
                  )}
                </Box>
              </Stack>
            </Box>

            {/* Footer */}
            <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider' }}>
              <Stack spacing={1}>
                <Button
                  variant="outlined"
                  startIcon={<OpenInNewIcon />}
                  fullWidth
                  onClick={() => navigate(`/scholarships/${application.scholarshipId}`)}
                >
                  {t('tracker.viewScholarship')}
                </Button>
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<DeleteOutlineIcon />}
                  fullWidth
                  onClick={() => setDeleteDialogOpen(true)}
                >
                  {t('tracker.deleteApplication')}
                </Button>
              </Stack>
            </Box>
          </Box>
        )}
      </Drawer>

      {/* Delete confirmation dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>{t('tracker.deleteApplication')}</DialogTitle>
        <DialogContent>
          <DialogContentText>{t('tracker.deleteConfirm')}</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>{t('cancel')}</Button>
          <Button color="error" variant="contained" onClick={handleDelete}>
            {t('delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
