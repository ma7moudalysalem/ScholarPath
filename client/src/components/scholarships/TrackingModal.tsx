import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link as RouterLink } from 'react-router-dom';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  Alert,
  Link,
  Snackbar,
  CircularProgress,
} from '@mui/material';
import { ApplicationStatus } from '@/types';
import { applicationService } from '@/services/applicationService';

interface TrackingModalProps {
  open: boolean;
  onClose: () => void;
  scholarshipId: string;
  scholarshipTitle: string;
}

export function TrackingModal({
  open,
  onClose,
  scholarshipId,
  scholarshipTitle: _scholarshipTitle,
}: TrackingModalProps) {
  const { t } = useTranslation();
  const [status, setStatus] = useState<ApplicationStatus>(ApplicationStatus.Planned);
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [alreadyExisted, setAlreadyExisted] = useState(false);
  const [snackbar, setSnackbar] = useState(false);

  const handleSubmit = async () => {
    setLoading(true);
    try {
      const response = await applicationService.trackApplication({
        scholarshipId,
        status,
        notes: notes.trim() || undefined,
      });

      if (response.alreadyExisted) {
        setAlreadyExisted(true);
      } else {
        setSnackbar(true);
        setTimeout(() => {
          onClose();
          resetForm();
        }, 1500);
      }
    } catch {
      // Error handled silently - API interceptor handles auth errors
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setStatus(ApplicationStatus.Planned);
    setNotes('');
    setAlreadyExisted(false);
  };

  const handleClose = () => {
    onClose();
    resetForm();
  };

  return (
    <>
      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <DialogTitle>{t('scholarshipDetail.addToTracking')}</DialogTitle>
        <DialogContent sx={{ pt: 2 }}>
          {alreadyExisted && (
            <Alert severity="info" sx={{ mb: 2 }}>
              {t('scholarshipDetail.alreadyTracked')}{' '}
              <Link component={RouterLink} to="/dashboard/tracker">
                {t('scholarshipDetail.viewTracker')}
              </Link>
            </Alert>
          )}

          <FormControl fullWidth sx={{ mt: 1, mb: 2 }}>
            <InputLabel>{t('scholarshipDetail.trackStatus')}</InputLabel>
            <Select
              value={status}
              label={t('scholarshipDetail.trackStatus')}
              onChange={(e) => setStatus(e.target.value as ApplicationStatus)}
              disabled={alreadyExisted}
            >
              <MenuItem value={ApplicationStatus.Planned}>
                {t('scholarshipDetail.planned')}
              </MenuItem>
              <MenuItem value={ApplicationStatus.Applied}>
                {t('scholarshipDetail.applied')}
              </MenuItem>
            </Select>
          </FormControl>

          <TextField
            fullWidth
            multiline
            rows={3}
            label={t('scholarshipDetail.trackNotes')}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={alreadyExisted}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>{t('cancel')}</Button>
          <Button
            onClick={handleSubmit}
            variant="contained"
            disabled={loading || alreadyExisted}
            startIcon={loading ? <CircularProgress size={18} /> : undefined}
          >
            {t('submit')}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={snackbar}
        autoHideDuration={3000}
        onClose={() => setSnackbar(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" variant="filled" onClose={() => setSnackbar(false)}>
          {t('scholarshipDetail.addedToTracker')}{' '}
          <Link
            component={RouterLink}
            to="/dashboard/tracker"
            sx={{ color: 'inherit', fontWeight: 600 }}
          >
            {t('scholarshipDetail.viewTracker')}
          </Link>
        </Alert>
      </Snackbar>
    </>
  );
}
