import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  IconButton,
  InputLabel,
  Link,
  MenuItem,
  Pagination,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import {
  CheckCircle as ApproveIcon,
  Cancel as RejectIcon,
  Info as InfoIcon,
  Close as CloseIcon,
  Visibility as ViewIcon,
} from '@mui/icons-material';
import { adminService, type UpgradeRequestFilters } from '@/services/adminService';
import { translateError } from '@/utils/errorUtils';
import { UpgradeRequestStatus, UserRole } from '@/types';
import type { UpgradeRequestDto, UpgradeRequestDetailDto } from '@/types';
import type { AxiosError } from 'axios';

const REJECTION_REASONS = [
  'missing_crn',
  'proof_not_clear',
  'suspicious_request',
  'incomplete_education',
  'insufficient_experience',
  'invalid_contact_info',
];

const statusColors: Record<number, 'default' | 'info' | 'success' | 'error' | 'warning'> = {
  [UpgradeRequestStatus.Pending]: 'info',
  [UpgradeRequestStatus.Approved]: 'success',
  [UpgradeRequestStatus.Rejected]: 'error',
  [UpgradeRequestStatus.NeedsMoreInfo]: 'warning',
};

const statusLabels: Record<number, string> = {
  [UpgradeRequestStatus.Pending]: 'Pending',
  [UpgradeRequestStatus.Approved]: 'Approved',
  [UpgradeRequestStatus.Rejected]: 'Rejected',
  [UpgradeRequestStatus.NeedsMoreInfo]: 'Needs Info',
};

const roleLabels: Record<number, string> = {
  [UserRole.Consultant]: 'Consultant',
  [UserRole.Company]: 'Company',
};

export default function UpgradeRequests() {
  const { t } = useTranslation();

  const [requests, setRequests] = useState<UpgradeRequestDto[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<UpgradeRequestFilters>({ page: 1, pageSize: 10 });
  const [searchInput, setSearchInput] = useState('');

  // Detail view
  const [selectedDetail, setSelectedDetail] = useState<UpgradeRequestDetailDto | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  // Decision dialog
  const [decisionDialog, setDecisionDialog] = useState<{
    type: 'approve' | 'reject' | 'info';
    requestId: string;
  } | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');
  const [rejectionReasons, setRejectionReasons] = useState<string[]>([]);
  const [decisionLoading, setDecisionLoading] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const fetchRequests = useCallback(async () => {
    setLoading(true);
    try {
      const result = await adminService.getUpgradeRequests(filters);
      setRequests(result.items);
      setTotalPages(result.totalPages);
    } catch {
      setRequests([]);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchRequests();
  }, [fetchRequests]);

  const handleSearch = () => {
    setFilters((prev) => ({ ...prev, search: searchInput, page: 1 }));
  };

  const handleViewDetail = async (id: string) => {
    setDetailLoading(true);
    try {
      const detail = await adminService.getUpgradeRequestDetail(id);
      setSelectedDetail(detail);
    } catch {
      setFeedback({ type: 'error', message: t('error') });
    } finally {
      setDetailLoading(false);
    }
  };

  const handleDecision = async () => {
    if (!decisionDialog) return;
    setDecisionLoading(true);

    try {
      const { type, requestId } = decisionDialog;
      if (type === 'approve') {
        await adminService.approveRequest(requestId, { reviewNotes });
      } else if (type === 'reject') {
        await adminService.rejectRequest(requestId, { reviewNotes, rejectionReasons });
      } else {
        await adminService.requestMoreInfo(requestId, { reviewNotes });
      }
      setFeedback({ type: 'success', message: t('admin.decisionSuccess') });
      setDecisionDialog(null);
      setReviewNotes('');
      setRejectionReasons([]);
      setSelectedDetail(null);
      fetchRequests();
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: string }>;
      setFeedback({
        type: 'error',
        message: translateError(axiosErr?.response?.data?.error ?? t('error')),
      });
    } finally {
      setDecisionLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        {t('admin.upgradeRequests')}
      </Typography>

      {feedback && (
        <Alert severity={feedback.type} sx={{ mb: 2 }} onClose={() => setFeedback(null)}>
          {feedback.message}
        </Alert>
      )}

      {/* Filters */}
      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
          <TextField
            size="small"
            placeholder={t('search')}
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
            sx={{ minWidth: 200 }}
          />
          <Button variant="outlined" onClick={handleSearch} size="small">
            {t('search')}
          </Button>
          <FormControl size="small" sx={{ minWidth: 130 }}>
            <InputLabel>{t('admin.filterStatus')}</InputLabel>
            <Select
              value={filters.status ?? ''}
              label={t('admin.filterStatus')}
              onChange={(e) => setFilters((prev) => ({ ...prev, status: e.target.value || undefined, page: 1 }))}
            >
              <MenuItem value="">{t('admin.all')}</MenuItem>
              <MenuItem value="Pending">Pending</MenuItem>
              <MenuItem value="Approved">Approved</MenuItem>
              <MenuItem value="Rejected">Rejected</MenuItem>
              <MenuItem value="NeedsMoreInfo">Needs Info</MenuItem>
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 130 }}>
            <InputLabel>{t('admin.filterType')}</InputLabel>
            <Select
              value={filters.type ?? ''}
              label={t('admin.filterType')}
              onChange={(e) => setFilters((prev) => ({ ...prev, type: e.target.value || undefined, page: 1 }))}
            >
              <MenuItem value="">{t('admin.all')}</MenuItem>
              <MenuItem value="Consultant">Consultant</MenuItem>
              <MenuItem value="Company">Company</MenuItem>
            </Select>
          </FormControl>
        </CardContent>
      </Card>

      {/* Table */}
      {loading ? (
        <Box textAlign="center" py={4}>
          <CircularProgress />
        </Box>
      ) : requests.length === 0 ? (
        <Card>
          <CardContent>
            <Typography color="text.secondary" textAlign="center">
              {t('admin.noRequests')}
            </Typography>
          </CardContent>
        </Card>
      ) : (
        <>
          <TableContainer component={Card}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>{t('admin.userName')}</TableCell>
                  <TableCell>{t('admin.userEmail')}</TableCell>
                  <TableCell>{t('admin.requestedRole')}</TableCell>
                  <TableCell>{t('admin.status')}</TableCell>
                  <TableCell>{t('admin.date')}</TableCell>
                  <TableCell>{t('admin.actions')}</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {requests.map((req) => (
                  <TableRow key={req.id}>
                    <TableCell>{req.userName}</TableCell>
                    <TableCell>{req.userEmail}</TableCell>
                    <TableCell>{roleLabels[req.requestedRole] ?? ''}</TableCell>
                    <TableCell>
                      <Chip
                        label={statusLabels[req.status]}
                        color={statusColors[req.status]}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{new Date(req.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        onClick={() => handleViewDetail(req.id)}
                        color="primary"
                      >
                        <ViewIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
              <Pagination
                count={totalPages}
                page={filters.page ?? 1}
                onChange={(_, page) => setFilters((prev) => ({ ...prev, page }))}
              />
            </Box>
          )}
        </>
      )}

      {/* Detail Dialog */}
      <Dialog
        open={!!selectedDetail || detailLoading}
        onClose={() => setSelectedDetail(null)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          {t('admin.requestDetail')}
          <IconButton onClick={() => setSelectedDetail(null)} size="small">
            <CloseIcon />
          </IconButton>
        </DialogTitle>
        <DialogContent dividers>
          {detailLoading ? (
            <Box textAlign="center" py={4}>
              <CircularProgress />
            </Box>
          ) : selectedDetail && (
            <Box>
              <Typography variant="subtitle2" color="text.secondary">
                {t('admin.applicant')}
              </Typography>
              <Typography>{selectedDetail.userName} ({selectedDetail.userEmail})</Typography>
              <Typography variant="body2">
                {t('admin.requestedRole')}: {roleLabels[selectedDetail.requestedRole]}
              </Typography>
              <Chip
                label={statusLabels[selectedDetail.status]}
                color={statusColors[selectedDetail.status]}
                size="small"
                sx={{ mt: 0.5 }}
              />

              <Divider sx={{ my: 2 }} />

              {/* Consultant fields */}
              {selectedDetail.requestedRole === UserRole.Consultant && (
                <>
                  {selectedDetail.education.length > 0 && (
                    <>
                      <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.education')}</Typography>
                      {selectedDetail.education.map((edu, i) => (
                        <Typography key={i} variant="body2" sx={{ ml: 1 }}>
                          {edu.degreeName} in {edu.fieldOfStudy} — {edu.institutionName} ({edu.startYear}–{edu.endYear ?? 'present'})
                        </Typography>
                      ))}
                      <Divider sx={{ my: 1 }} />
                    </>
                  )}
                  {selectedDetail.experienceSummary && (
                    <>
                      <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.experience')}</Typography>
                      <Typography variant="body2">{selectedDetail.experienceSummary}</Typography>
                      <Divider sx={{ my: 1 }} />
                    </>
                  )}
                  {selectedDetail.expertiseTags.length > 0 && (
                    <>
                      <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.expertiseTags')}</Typography>
                      <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 0.5 }}>
                        {selectedDetail.expertiseTags.map((tag) => (
                          <Chip key={tag} label={tag} size="small" />
                        ))}
                      </Box>
                      <Divider sx={{ my: 1 }} />
                    </>
                  )}
                  {selectedDetail.links.length > 0 && (
                    <>
                      <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.links')}</Typography>
                      {selectedDetail.links.map((link, i) => (
                        <Typography key={i} variant="body2">
                          {link.label}: <Link href={link.url} target="_blank" rel="noopener">{link.url}</Link>
                        </Typography>
                      ))}
                      <Divider sx={{ my: 1 }} />
                    </>
                  )}
                </>
              )}

              {/* Company fields */}
              {selectedDetail.requestedRole === UserRole.Company && (
                <>
                  <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.companyName')}</Typography>
                  <Typography variant="body2">{selectedDetail.companyName}</Typography>
                  {selectedDetail.country && <Typography variant="body2">{t('upgrade.country')}: {selectedDetail.country}</Typography>}
                  {selectedDetail.website && (
                    <Typography variant="body2">
                      {t('upgrade.website')}: <Link href={selectedDetail.website} target="_blank" rel="noopener">{selectedDetail.website}</Link>
                    </Typography>
                  )}
                  {selectedDetail.contactPersonName && (
                    <Typography variant="body2">{t('upgrade.contactName')}: {selectedDetail.contactPersonName}</Typography>
                  )}
                  {selectedDetail.contactEmail && (
                    <Typography variant="body2">{t('upgrade.contactEmail')}: {selectedDetail.contactEmail}</Typography>
                  )}
                  {selectedDetail.companyRegistrationNumber && (
                    <Typography variant="body2">{t('upgrade.crn')}: {selectedDetail.companyRegistrationNumber}</Typography>
                  )}
                  <Divider sx={{ my: 1 }} />
                </>
              )}

              {/* Files */}
              {selectedDetail.files.length > 0 && (
                <>
                  <Typography variant="subtitle2" fontWeight={600}>{t('upgrade.proofDocuments')}</Typography>
                  {selectedDetail.files.map((file) => (
                    <Typography key={file.id} variant="body2">
                      {file.fileName} ({(file.fileSize / 1024).toFixed(0)} KB)
                    </Typography>
                  ))}
                </>
              )}
            </Box>
          )}
        </DialogContent>
        {selectedDetail && selectedDetail.status === UpgradeRequestStatus.Pending && (
          <DialogActions sx={{ p: 2, gap: 1 }}>
            <Button
              variant="contained"
              color="success"
              startIcon={<ApproveIcon />}
              onClick={() => setDecisionDialog({ type: 'approve', requestId: selectedDetail.id })}
            >
              {t('admin.approve')}
            </Button>
            <Button
              variant="contained"
              color="error"
              startIcon={<RejectIcon />}
              onClick={() => setDecisionDialog({ type: 'reject', requestId: selectedDetail.id })}
            >
              {t('admin.reject')}
            </Button>
            <Button
              variant="outlined"
              startIcon={<InfoIcon />}
              onClick={() => setDecisionDialog({ type: 'info', requestId: selectedDetail.id })}
            >
              {t('admin.requestMoreInfo')}
            </Button>
          </DialogActions>
        )}
      </Dialog>

      {/* Decision Dialog */}
      <Dialog
        open={!!decisionDialog}
        onClose={() => setDecisionDialog(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          {decisionDialog?.type === 'approve' && t('admin.confirmApprove')}
          {decisionDialog?.type === 'reject' && t('admin.confirmReject')}
          {decisionDialog?.type === 'info' && t('admin.requestMoreInfo')}
        </DialogTitle>
        <DialogContent>
          {decisionDialog?.type === 'reject' && (
            <>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                {t('admin.selectReasons')}
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 2 }}>
                {REJECTION_REASONS.map((reason) => (
                  <Chip
                    key={reason}
                    label={t(`admin.reason.${reason}`)}
                    clickable
                    color={rejectionReasons.includes(reason) ? 'error' : 'default'}
                    onClick={() =>
                      setRejectionReasons((prev) =>
                        prev.includes(reason)
                          ? prev.filter((r) => r !== reason)
                          : [...prev, reason]
                      )
                    }
                  />
                ))}
              </Box>
            </>
          )}
          <TextField
            fullWidth
            multiline
            rows={3}
            label={t('admin.reviewNotes')}
            value={reviewNotes}
            onChange={(e) => setReviewNotes(e.target.value)}
            required={decisionDialog?.type !== 'approve'}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDecisionDialog(null)}>{t('cancel')}</Button>
          <Button
            variant="contained"
            onClick={handleDecision}
            disabled={
              decisionLoading ||
              (decisionDialog?.type !== 'approve' && !reviewNotes.trim()) ||
              (decisionDialog?.type === 'reject' && rejectionReasons.length === 0)
            }
            color={decisionDialog?.type === 'reject' ? 'error' : 'primary'}
          >
            {decisionLoading ? <CircularProgress size={24} /> : t('submit')}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
