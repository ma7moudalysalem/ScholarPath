import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore, selectIsAdmin } from '@/stores/authStore';
import { AccountStatus, UserRole } from '@/types';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: UserRole;
}

export function ProtectedRoute({ children, requiredRole }: ProtectedRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);
  const isAdmin = useAuthStore(selectIsAdmin);
  const location = useLocation();

  if (!isAuthenticated) {
    sessionStorage.setItem('intendedDestination', location.pathname);
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // If user hasn't completed onboarding, redirect to onboarding
  // (unless they're already on the onboarding page)
  if (user && !user.isOnboardingComplete && location.pathname !== '/onboarding') {
    return <Navigate to="/onboarding" replace />;
  }

  if (user && user.accountStatus === AccountStatus.Pending && location.pathname !== '/onboarding') {
    return <Navigate to="/onboarding" replace />;
  }

  if (
    user &&
    user.isOnboardingComplete &&
    user.accountStatus === AccountStatus.Active &&
    location.pathname === '/onboarding'
  ) {
    return <Navigate to="/dashboard" replace />;
  }

  // Check role requirement
  if (requiredRole === UserRole.Admin && !isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  if (requiredRole !== undefined && user?.role !== requiredRole && !isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}
