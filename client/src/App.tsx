import { useMemo, lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';
import { ThemeProvider, CssBaseline, CircularProgress, Box } from '@mui/material';
import { createScholarPathTheme } from '@/theme';
import { useUiStore } from '@/stores/uiStore';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { ErrorBoundary } from '@/components/common/ErrorBoundary';
import { AuthenticatedLayout } from '@/components/Layout/AuthenticatedLayout';
import { PublicLayout } from '@/components/Layout/PublicLayout';
import { UserRole } from '@/types';

// Lazy-loaded pages for code-splitting
const Home = lazy(() => import('@/pages/Home'));
const Login = lazy(() => import('@/pages/auth/Login'));
const Register = lazy(() => import('@/pages/auth/Register'));
const ForgotPassword = lazy(() => import('@/pages/auth/ForgotPassword'));
const ResetPassword = lazy(() => import('@/pages/auth/ResetPassword'));
const Onboarding = lazy(() => import('@/pages/auth/Onboarding'));
const Dashboard = lazy(() => import('@/pages/Dashboard'));
const Profile = lazy(() => import('@/pages/profile/Profile'));
const Security = lazy(() => import('@/pages/profile/Security'));
const ScholarshipList = lazy(() => import('@/pages/scholarships/ScholarshipList'));
const ScholarshipDetail = lazy(() => import('@/pages/scholarships/ScholarshipDetail'));
const Community = lazy(() => import('@/pages/community/Community'));
const GroupDetail = lazy(() => import('@/pages/community/GroupDetail'));
const Notifications = lazy(() => import('@/pages/Notifications'));
const UpgradeRequests = lazy(() => import('@/pages/admin/UpgradeRequests'));
const NotFound = lazy(() => import('@/pages/NotFound'));

function PageLoader() {
  return (
    <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
      <CircularProgress />
    </Box>
  );
}

export default function App() {
  const direction = useUiStore((s) => s.direction);
  const themeMode = useUiStore((s) => s.themeMode);

  const theme = useMemo(
    () => createScholarPathTheme(direction, themeMode),
    [direction, themeMode]
  );

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <ErrorBoundary>
        <Suspense fallback={<PageLoader />}>
          <Routes>
            {/* Public routes */}
            <Route element={<PublicLayout />}>
              <Route path="/" element={<Home />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route path="/forgot-password" element={<ForgotPassword />} />
              <Route path="/reset-password" element={<ResetPassword />} />
            </Route>

            {/* Onboarding (protected, no layout chrome) */}
            <Route
              path="/onboarding"
              element={
                <ProtectedRoute>
                  <Onboarding />
                </ProtectedRoute>
              }
            />

            {/* Authenticated routes */}
            <Route
              element={
                <ProtectedRoute>
                  <AuthenticatedLayout />
                </ProtectedRoute>
              }
            >
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/profile" element={<Profile />} />
              <Route path="/profile/security" element={<Security />} />
              <Route path="/scholarships" element={<ScholarshipList />} />
              <Route path="/scholarships/:id" element={<ScholarshipDetail />} />
              <Route path="/community" element={<Community />} />
              <Route path="/community/groups/:id" element={<GroupDetail />} />
              <Route path="/notifications" element={<Notifications />} />

              {/* Admin routes */}
              <Route
                path="/admin/upgrade-requests"
                element={
                  <ProtectedRoute requiredRole={UserRole.Admin}>
                    <UpgradeRequests />
                  </ProtectedRoute>
                }
              />
            </Route>

            {/* 404 page */}
            <Route path="*" element={<NotFound />} />
          </Routes>
        </Suspense>
      </ErrorBoundary>
    </ThemeProvider>
  );
}
