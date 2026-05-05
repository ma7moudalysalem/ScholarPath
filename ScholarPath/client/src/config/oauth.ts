export const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || '';
export const MICROSOFT_CLIENT_ID = import.meta.env.VITE_MICROSOFT_CLIENT_ID || '';

export const msalConfig = {
  auth: {
    clientId: MICROSOFT_CLIENT_ID,
    authority: 'https://login.microsoftonline.com/common',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage' as const,
    storeAuthStateInCookie: false,
  },
};

export const msalLoginRequest = {
  scopes: ['User.Read', 'openid', 'profile', 'email'],
};
