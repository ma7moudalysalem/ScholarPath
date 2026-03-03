export const INTENDED_DESTINATION_KEY = 'intendedDestination';

/**
 * Stores the intended destination URL in sessionStorage.
 * This is used to redirect a user back to their intended action after logging in.
 * 
 * @param url The target URL to store (e.g., '/dashboard', '/scholarships/123')
 */
export const setIntendedDestination = (url: string): void => {
    if (typeof window !== 'undefined') {
        sessionStorage.setItem(INTENDED_DESTINATION_KEY, url);
    }
};

/**
 * Retrieves the stored intended destination URL from sessionStorage.
 * 
 * @returns The stored URL, or null if nothing is stored.
 */
export const getIntendedDestination = (): string | null => {
    if (typeof window !== 'undefined') {
        return sessionStorage.getItem(INTENDED_DESTINATION_KEY);
    }
    return null;
};

/**
 * Clears the stored intended destination URL from sessionStorage.
 * Call this after successful navigation to the intended destination.
 */
export const clearIntendedDestination = (): void => {
    if (typeof window !== 'undefined') {
        sessionStorage.removeItem(INTENDED_DESTINATION_KEY);
    }
};
