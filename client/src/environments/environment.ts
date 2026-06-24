// Single environment configuration using process.env
// Values are injected at build time from .env files
// Supports all environments (development, production, etc.) through environment variables

import { Environment } from './environment.interface';

export const environment: Environment = {
  production: process.env['NG_APP_ENVIRONMENT'] === 'production',
  configuration: process.env['NG_APP_ENVIRONMENT'] || 'development',
  apiUrl: process.env['NG_APP_API_URL'] || 'https://localhost:7001/api/v1',
  msal: {
    auth: {
      clientId: process.env['NG_APP_MSAL_CLIENT_ID'] || 'your-client-id-here',
      authority: `https://login.microsoftonline.com/${process.env['NG_APP_MSAL_TENANT_ID'] || 'your-tenant-id-here'}`,
      redirectUri: process.env['NG_APP_MSAL_REDIRECT_URI'] || 'http://localhost:4200',
      postLogoutRedirectUri: process.env['NG_APP_MSAL_POST_LOGOUT_REDIRECT_URI'] || 'http://localhost:4200'
    },
    cache: {
      cacheLocation: (process.env['NG_APP_CACHE_LOCATION'] as 'localStorage' | 'sessionStorage') || 'localStorage',
      storeAuthStateInCookie: false
    },
    scopes: (process.env['NG_APP_API_SCOPE'] + ' ' + 'user.read openid profile email').split(' ')
  },
  apiScope: process.env['NG_APP_API_SCOPE'] || 'api://your-api-scope-here',
  logLevel: (process.env['NG_APP_LOG_LEVEL'] as 'debug' | 'info' | 'warn' | 'error') || 'debug',
  devname: process.env['NG_APP_DEV_NAME']
};
