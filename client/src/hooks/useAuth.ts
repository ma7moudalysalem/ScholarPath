import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useShallow } from 'zustand/react/shallow';
import { useAuthStore, selectIsAdmin, selectRole, selectIsOnboarded } from '@/stores/authStore';
import { authService } from '@/services/authService';
import type { LoginRequest, RegisterRequest, OnboardingRequest } from '@/types';
import { AccountStatus, UserRole } from '@/types';

export function useAuth() {
  const navigate = useNavigate();

  const { user, isAuthenticated, isLoading, setAuth, setUser, setLoading, storeLogout } =
    useAuthStore(
      useShallow((s) => ({
        user: s.user,
        isAuthenticated: s.isAuthenticated,
        isLoading: s.isLoading,
        setAuth: s.setAuth,
        setUser: s.setUser,
        setLoading: s.setLoading,
        storeLogout: s.logout,
      }))
    );

  const isAdmin = useAuthStore(selectIsAdmin);
  const role = useAuthStore(selectRole);
  const isOnboarded = useAuthStore(selectIsOnboarded);

  const login = useCallback(
    async (data: LoginRequest) => {
      setLoading(true);
      try {
        const response = await authService.login(data);
        setAuth(response.user, response.accessToken, response.refreshToken);

        if (!response.user.isOnboardingComplete || response.user.accountStatus === AccountStatus.Pending) {
          navigate('/onboarding');
        } else {
          navigate('/dashboard');
        }
      } finally {
        setLoading(false);
      }
    },
    [setAuth, setLoading, navigate]
  );

  const register = useCallback(
    async (data: RegisterRequest) => {
      setLoading(true);
      try {
        const response = await authService.register(data);
        setAuth(response.user, response.accessToken, response.refreshToken);
        navigate('/onboarding');
      } finally {
        setLoading(false);
      }
    },
    [setAuth, setLoading, navigate]
  );

  const logout = useCallback(async () => {
    try {
      await authService.logout();
    } catch {
      // Ignore errors during logout
    } finally {
      storeLogout();
      navigate('/login');
    }
  }, [storeLogout, navigate]);

  const completeOnboarding = useCallback(
    async (data: OnboardingRequest) => {
      setLoading(true);
      try {
        const updatedUser = await authService.completeOnboarding(data);
        setUser(updatedUser);
        if (updatedUser.accountStatus === AccountStatus.Pending) {
          navigate('/onboarding');
        } else {
          navigate('/dashboard');
        }
      } finally {
        setLoading(false);
      }
    },
    [setUser, setLoading, navigate]
  );

  const refreshCurrentUser = useCallback(async () => {
    try {
      const currentUser = await authService.getMe();
      setUser(currentUser);
    } catch {
      storeLogout();
    }
  }, [setUser, storeLogout]);

  return {
    user,
    isAuthenticated,
    isLoading,
    isAdmin,
    role,
    isOnboarded,
    isStudent: role === UserRole.Student,
    isConsultant: role === UserRole.Consultant,
    isCompany: role === UserRole.Company,
    login,
    register,
    logout,
    completeOnboarding,
    refreshCurrentUser,
  };
}
