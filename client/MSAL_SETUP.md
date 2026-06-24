# MSAL.js 2.0 Setup Guide

This document outlines the setup and configuration of Microsoft Authentication Library (MSAL) 2.0 in the Angular application.

## Prerequisites

1. Azure AD tenant
2. App registration in Azure AD
3. Angular 19 application

## Azure AD App Registration

1. Go to Azure Portal > Azure Active Directory > App registrations
2. Click "New registration"
3. Fill in the details:
   - Name: Your app name
   - Supported account types: Choose appropriate option
   - Redirect URI: `http://localhost:4200` (for development)
4. Note down the Application (client) ID and Directory (tenant) ID

### Configure App Registration

1. **Authentication**:
   - Add redirect URIs for your environments
   - Enable ID tokens and access tokens
   - Add logout URLs

2. **API Permissions**:
   - Add Microsoft Graph permissions (User.Read at minimum)
   - Add any custom API permissions needed

3. **Expose an API** (if using custom API):
   - Add scopes for your backend API

## Environment Configuration

Update the environment files with your Azure AD configuration:

```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api/v1',
  msal: {
    auth: {
      clientId: 'your-client-id-here',
      authority: 'https://login.microsoftonline.com/your-tenant-id-here',
      redirectUri: 'http://localhost:4200',
      postLogoutRedirectUri: 'http://localhost:4200'
    },
    cache: {
      cacheLocation: 'localStorage',
      storeAuthStateInCookie: false
    }
  }
};
```

## Application Configuration

The application is configured to use MSAL in `app.config.ts`:

- MSAL instance is created with environment configuration
- MSAL interceptor is configured for API calls
- Protected resource map is set for backend API

## Usage

### Authentication Service

The `AuthService` provides methods for:
- Login (redirect or popup)
- Logout
- Getting access tokens
- Checking authentication status
- Getting user information

### Route Protection

All routes are protected with `MsalGuard` which will redirect unauthenticated users to Azure AD login.

### API Calls

The MSAL interceptor automatically adds authentication tokens to API requests to your backend.

## Development

1. Update environment configurations with your Azure AD app details
2. Replace placeholder values:
   - `your-client-id-here`
   - `your-tenant-id-here`
   - `api://your-api-scope`
3. Test authentication flow
4. Configure additional scopes as needed

## Testing

- Navigate to protected routes to test authentication
- Check browser developer tools for token acquisition
- Verify API calls include Authorization header

## Troubleshooting

Common issues:
1. **Redirect URI mismatch**: Ensure redirect URIs in Azure AD match your application URLs
2. **Scope issues**: Verify API permissions and scopes are correctly configured
3. **Token acquisition failures**: Check network requests and Azure AD configuration

## Security Considerations

- Use HTTPS in production
- Configure appropriate token lifetimes
- Implement proper error handling
- Consider using refresh tokens for long-running applications
- Store sensitive configuration in secure environment variables