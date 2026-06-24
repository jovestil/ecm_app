export interface Environment {
  production: boolean;
  configuration: string;
  apiUrl: string;
  msal: {
    auth: {
      clientId: string;
      authority: string;
      redirectUri: string;
      postLogoutRedirectUri: string;
    };
    cache: {
      cacheLocation: 'localStorage' | 'sessionStorage';
      storeAuthStateInCookie: boolean;
    };
    scopes: string[];
  };
  apiScope: string;
  logLevel: 'debug' | 'info' | 'warn' | 'error';
  devname?: string;
}