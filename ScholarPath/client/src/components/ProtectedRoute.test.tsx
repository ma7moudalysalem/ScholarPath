import { act, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { useAuthStore } from '@/stores/authStore';
import { AccountStatus, UserRole } from '@/types';

describe('ProtectedRoute', () => {
  afterEach(() => {
    act(() => {
      useAuthStore.getState().logout();
    });
  });

  it('redirects unauthenticated users to login', async () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Dashboard</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('Login')).toBeInTheDocument();
  });

  it('redirects non-onboarded users to onboarding', async () => {
    act(() => {
      useAuthStore.setState({
        isAuthenticated: true,
        user: {
          id: 'user-1',
          email: 'user@test.local',
          firstName: 'User',
          lastName: 'Test',
          profileImageUrl: null,
          role: UserRole.Unassigned,
          accountStatus: AccountStatus.Active,
          isOnboardingComplete: false,
        },
      });
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Dashboard</div>
              </ProtectedRoute>
            }
          />
          <Route path="/onboarding" element={<div>Onboarding</div>} />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('Onboarding')).toBeInTheDocument();
  });

  it('redirects pending users to onboarding', async () => {
    act(() => {
      useAuthStore.setState({
        isAuthenticated: true,
        user: {
          id: 'user-2',
          email: 'pending@test.local',
          firstName: 'Pending',
          lastName: 'User',
          profileImageUrl: null,
          role: UserRole.Unassigned,
          accountStatus: AccountStatus.Pending,
          isOnboardingComplete: true,
        },
      });
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Dashboard</div>
              </ProtectedRoute>
            }
          />
          <Route path="/onboarding" element={<div>Onboarding</div>} />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('Onboarding')).toBeInTheDocument();
  });

  it('allows onboarded active users to access protected route', () => {
    act(() => {
      useAuthStore.setState({
        isAuthenticated: true,
        user: {
          id: 'user-3',
          email: 'student@test.local',
          firstName: 'Student',
          lastName: 'User',
          profileImageUrl: null,
          role: UserRole.Student,
          accountStatus: AccountStatus.Active,
          isOnboardingComplete: true,
        },
      });
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <div>Dashboard</div>
              </ProtectedRoute>
            }
          />
          <Route path="/onboarding" element={<div>Onboarding</div>} />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('redirects onboarded active users away from onboarding', async () => {
    act(() => {
      useAuthStore.setState({
        isAuthenticated: true,
        user: {
          id: 'user-4',
          email: 'active@test.local',
          firstName: 'Active',
          lastName: 'User',
          profileImageUrl: null,
          role: UserRole.Student,
          accountStatus: AccountStatus.Active,
          isOnboardingComplete: true,
        },
      });
    });

    render(
      <MemoryRouter initialEntries={['/onboarding']}>
        <Routes>
          <Route
            path="/onboarding"
            element={
              <ProtectedRoute>
                <div>Onboarding</div>
              </ProtectedRoute>
            }
          />
          <Route path="/dashboard" element={<div>Dashboard</div>} />
          <Route path="/login" element={<div>Login</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(await screen.findByText('Dashboard')).toBeInTheDocument();
  });
});
