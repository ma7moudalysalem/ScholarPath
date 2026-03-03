import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { setIntendedDestination } from '@/utils/navigation';

/**
 * A hook that provides a function to wrap actions requiring authentication.
 * 
 * If the user is authenticated, the provided action function is executed immediately.
 * If the user is not authenticated, the current path (or a provided targetUrl) is saved 
 * to sessionStorage as the 'intendedDestination', and the user is redirected to the login page.
 * 
 * @returns A function `executeProtectedAction` that takes the action to run, and an optional explicit targetUrl.
 */
export function useProtectedAction() {
    const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
    const navigate = useNavigate();
    const location = useLocation();

    const executeProtectedAction = (action: () => void, targetUrl?: string) => {
        if (isAuthenticated) {
            action();
        } else {
            // Save the destination (either provided one, or the current URL)
            const destination = targetUrl || location.pathname + location.search;
            setIntendedDestination(destination);

            // Redirect to login
            navigate('/login');
        }
    };

    return executeProtectedAction;
}
