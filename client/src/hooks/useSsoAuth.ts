import { useCallback, useState } from 'react';
import { useGoogleLogin } from '@react-oauth/google';
import { PublicClientApplication } from '@azure/msal-browser';
import { GOOGLE_CLIENT_ID, MICROSOFT_CLIENT_ID, msalConfig, msalLoginRequest } from '@/config/oauth';
import { authService } from '@/services/authService';
import { useAuthStore } from '@/stores/authStore';
import { useNavigate } from 'react-router-dom';
import { AccountStatus } from '@/types';

const INTENDED_DESTINATION_KEY = 'intendedDestination';

// Lazily create MSAL instance only if configured
let msalInstance: PublicClientApplication | null = null;
function getMsalInstance(): PublicClientApplication | null {
  if (!MICROSOFT_CLIENT_ID) return null;
  if (!msalInstance) {
    msalInstance = new PublicClientApplication(msalConfig);
  }
  return msalInstance;
}

export function useSsoAuth(onSuccess?: () => void) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);

  const handleExternalLoginResponse = useCallback(
    async (provider: string, idToken: string) => {
      setLoading(true);
      setError(null);
      try {
        const response = await authService.externalLogin({ provider, idToken });
        setAuth(response.user);
        onSuccess?.();

        if (!response.user.isOnboardingComplete || response.user.accountStatus === AccountStatus.Pending) {
          navigate('/onboarding');
        } else {
          const intended = sessionStorage.getItem(INTENDED_DESTINATION_KEY);
          if (intended) {
            sessionStorage.removeItem(INTENDED_DESTINATION_KEY);
            navigate(intended);
          } else {
            navigate('/dashboard');
          }
        }
      } catch (err: unknown) {
        const axiosErr = err as { response?: { data?: { error?: string } } };
        setError(axiosErr?.response?.data?.error || 'errors.auth.ssoFailed');
      } finally {
        setLoading(false);
      }
    },
    [setAuth, navigate, onSuccess]
  );

  const googleLogin = useGoogleLogin({
    onSuccess: async (tokenResponse) => {
      await handleExternalLoginResponse('google', tokenResponse.access_token);
    },
    onError: () => {
      setError('errors.auth.ssoFailed');
    },
    flow: 'implicit',
  });

  const microsoftLogin = useCallback(async () => {
    const instance = getMsalInstance();
    if (!instance) {
      setError('errors.auth.ssoNotConfigured');
      return;
    }
    try {
      await instance.initialize();
      const result = await instance.loginPopup(msalLoginRequest);
      if (result.accessToken) {
        await handleExternalLoginResponse('microsoft', result.accessToken);
      }
    } catch {
      setError('errors.auth.ssoFailed');
    }
  }, [handleExternalLoginResponse]);

  const handleExternalLogin = useCallback(
    (provider: string) => {
      if (provider === 'google') {
        if (!GOOGLE_CLIENT_ID) {
          setError('errors.auth.ssoNotConfigured');
          return;
        }
        googleLogin();
      } else if (provider === 'microsoft') {
        microsoftLogin();
      }
    },
    [googleLogin, microsoftLogin]
  );

  return { handleExternalLogin, loading, error, setError };
}
