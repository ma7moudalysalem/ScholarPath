import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Alert, AlertTitle, Collapse, IconButton, Button, Box } from '@mui/material';
import { Close as CloseIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { upgradeRequestService } from '@/services/upgradeRequestService';
import { UpgradeRequestDto, UpgradeRequestStatus } from '@/types';

export function UpgradeRequestBanner() {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const [request, setRequest] = useState<UpgradeRequestDto | null>(null);
    const [open, setOpen] = useState(true);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchStatus = async () => {
            try {
                const data = await upgradeRequestService.getMyUpgradeRequestStatus();
                setRequest(data);
            } catch (error) {
                console.error('Failed to fetch upgrade request status:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchStatus();
    }, []);

    if (loading || !request || !open) {
        return null;
    }

    // Determine Alert type and Title based on status
    let severity: 'info' | 'success' | 'warning' | 'error' = 'info';
    let title = '';
    let message = '';

    switch (request.status) {
        case UpgradeRequestStatus.Pending:
            severity = 'info';
            title = t('upgradeBanner.pendingTitle', 'Upgrade Request Pending');
            message = t(
                'upgradeBanner.pendingMessage',
                'Your account upgrade request is currently under review by our team. We will notify you once a decision is made.'
            );
            break;
        case UpgradeRequestStatus.Approved:
            severity = 'success';
            title = t('upgradeBanner.approvedTitle', 'Upgrade Request Approved!');
            message = t(
                'upgradeBanner.approvedMessage',
                'Congratulations! Your account upgrade request was approved. Enjoy your new features.'
            );
            break;
        case UpgradeRequestStatus.Rejected:
            severity = 'error';
            title = t('upgradeBanner.rejectedTitle', 'Upgrade Request Rejected');
            message = t(
                'upgradeBanner.rejectedMessage',
                'We could not approve your request at this time. Reason: {reason}',
                { reason: request.adminNotes || t('upgradeBanner.noReason', 'No reason provided') }
            );
            break;
        case UpgradeRequestStatus.NeedsMoreInfo:
            severity = 'warning';
            title = t('upgradeBanner.needsInfoTitle', 'Action Required');
            message = t(
                'upgradeBanner.needsInfoMessage',
                'Your request requires more information: {reason}',
                { reason: request.adminNotes || t('upgradeBanner.noReason', 'Please contact support') }
            );
            break;
    }

    return (
        <Box sx={{ p: 2, pb: 0 }}>
            <Collapse in={open}>
                <Alert
                    severity={severity}
                    action={
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            {request.status === UpgradeRequestStatus.NeedsMoreInfo && (
                                <Button color="inherit" size="small" onClick={() => navigate('/community')} sx={{ mr: 1, textTransform: 'none' }}>
                                    {t('upgradeBanner.contactSupport', 'Contact Support')}
                                </Button>
                            )}
                            <IconButton
                                aria-label="close"
                                color="inherit"
                                size="small"
                                onClick={() => setOpen(false)}
                            >
                                <CloseIcon fontSize="inherit" />
                            </IconButton>
                        </Box>
                    }
                >
                    <AlertTitle>{title}</AlertTitle>
                    {message}
                </Alert>
            </Collapse>
        </Box>
    );
}
