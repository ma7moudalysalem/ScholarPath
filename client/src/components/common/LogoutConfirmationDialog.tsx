import { useTranslation } from 'react-i18next';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    Button,
} from '@mui/material';

interface LogoutConfirmationDialogProps {
    open: boolean;
    onClose: () => void;
    onConfirm: () => void;
}

export function LogoutConfirmationDialog({
    open,
    onClose,
    onConfirm,
}: LogoutConfirmationDialogProps) {
    const { t } = useTranslation();

    return (
        <Dialog
            open={open}
            onClose={onClose}
            aria-labelledby="logout-dialog-title"
            aria-describedby="logout-dialog-description"
        >
            <DialogTitle id="logout-dialog-title">
                {t('auth.logoutConfirmTitle', 'Confirm Logout')}
            </DialogTitle>
            <DialogContent>
                <DialogContentText id="logout-dialog-description">
                    {t('auth.logoutConfirmMessage', 'Are you sure you want to log out of your account?')}
                </DialogContentText>
            </DialogContent>
            <DialogActions sx={{ px: 3, pb: 2 }}>
                <Button onClick={onClose} color="inherit">
                    {t('common.cancel', 'Cancel')}
                </Button>
                <Button onClick={onConfirm} color="error" variant="contained" autoFocus>
                    {t('logout', 'Logout')}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
