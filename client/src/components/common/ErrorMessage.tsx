import { Alert, AlertTitle, Box, Button } from '@mui/material';
import { useTranslation } from 'react-i18next';

interface ErrorMessageProps {
  message?: string;
  title?: string;
  onRetry?: () => void;
  severity?: 'error' | 'warning' | 'info';
}

export function ErrorMessage({
  message,
  title,
  onRetry,
  severity = 'error',
}: ErrorMessageProps) {
  const { t } = useTranslation();

  return (
    <Box sx={{ py: 2 }}>
      <Alert
        severity={severity}
        action={
          onRetry ? (
            <Button color="inherit" size="small" onClick={onRetry}>
              {t('retry')}
            </Button>
          ) : undefined
        }
      >
        {title && <AlertTitle>{title}</AlertTitle>}
        {message ?? t('error')}
      </Alert>
    </Box>
  );
}
