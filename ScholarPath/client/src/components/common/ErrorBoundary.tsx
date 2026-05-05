import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Box, Typography, Button, Paper, Container } from '@mui/material';
import i18n from '@/i18n';

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback;
      }

      return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
          <Paper sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="h5" gutterBottom>
              {i18n.t('errorBoundary.title')}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              {i18n.t('errorBoundary.description')}
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
              <Button variant="contained" onClick={this.handleReset}>
                {i18n.t('errorBoundary.tryAgain')}
              </Button>
              <Button variant="outlined" onClick={() => window.location.assign('/')}>
                {i18n.t('errorBoundary.goHome')}
              </Button>
            </Box>
          </Paper>
        </Container>
      );
    }

    return this.props.children;
  }
}
