# Azure AD App Registrations Configuration

## Overview

The Mathy ELM application uses two separate Azure AD app registrations for frontend and backend authentication. This document outlines the configuration details and explains why different registrations are used.

## App Registration Details

### Frontend App Registration (Angular Application)
- **Client ID**: `2cf3ed6e-8bb3-43d4-a1c8-7bbecd85753d`
- **Tenant ID**: `aa5e36ca-b6c1-4565-8261-0b02ac026bce`
- **Type**: Single Page Application (SPA)
- **Purpose**: Handles user authentication and token acquisition
- **Scopes Requested**: `user.read openid profile offline_access`
- **Redirect URI**: `https://localhost:4200`
- **Logout Redirect URI**: `http://localhost:4200/logout`

### Backend App Registration (API Application)
- **Client ID**: `4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2`
- **Tenant ID**: `aa5e36ca-b6c1-4565-8261-0b02ac026bce`
- **Type**: Web API
- **Purpose**: Validates JWT tokens from frontend
- **Expected Audience**: `api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user`
- **API Scope**: `api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user`

## Configuration Files

### Frontend Configuration
**File**: `/client/.env.development`
```env
NG_APP_MSAL_CLIENT_ID=2cf3ed6e-8bb3-43d4-a1c8-7bbecd85753d
NG_APP_MSAL_TENANT_ID=aa5e36ca-b6c1-4565-8261-0b02ac026bce
NG_APP_MSAL_REDIRECT_URI=https://localhost:4200
NG_APP_MSAL_POST_LOGOUT_REDIRECT_URI=http://localhost:4200/logout
NG_APP_API_SCOPE=api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user
```

### Backend Configuration
**File**: `/server/src/Mathy.ELM.Api/appsettings.json`
```json
{
  "AzureAd": {
    "TenantId": "aa5e36ca-b6c1-4565-8261-0b02ac026bce",
    "ClientId": "4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2",
    "Audience": "api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user"
  }
}
```

## Authentication Flow

1. **User Login**: Angular app authenticates user using SPA app registration (`2cf3ed6e-8bb3-43d4-a1c8-7bbecd85753d`)
2. **Token Acquisition**: Frontend requests access token for API scope (`api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user`)
3. **API Calls**: Frontend sends requests to backend with Bearer token
4. **Token Validation**: Backend validates token using API app registration (`4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2`)

## Why Two App Registrations?

### Security Best Practices
- **Separation of Concerns**: Frontend handles user authentication, backend handles authorization
- **Least Privilege**: Each component only has the permissions it needs
- **Token Validation**: Backend can validate tokens independently

### Different Application Types
- **SPA Registration**: Optimized for single-page applications with PKCE flow
- **API Registration**: Configured as a protected resource that accepts tokens

### Scope Management
- **Frontend Scopes**: User-focused (`user.read`, `openid`, `profile`)
- **Backend Scopes**: API-specific (`access_as_user`)

## Current Issues

### Token Validation Problems
The backend is currently experiencing signature validation errors:
```
Authentication failed: IDX10500: Signature validation failed. No security keys were provided to validate the signature
```

### Temporary Workarounds
Currently signature validation is disabled in development:
```csharp
ValidateIssuerSigningKey = false, // Temporarily disable to test other validations
RequireSignedTokens = false, // Also disable this for testing
```

## Recommended Solutions

1. **Enable Signature Validation**: Re-enable proper JWT signature validation
2. **Verify App Registration Permissions**: Ensure API permissions are properly configured
3. **Check Token Audience**: Verify tokens have correct audience claim
4. **Azure AD Configuration**: Ensure both app registrations are properly configured in Azure portal

## Configuration Validation

### Frontend Token Request
The frontend should request tokens with:
- **Scopes**: `api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user`
- **Authority**: `https://login.microsoftonline.com/aa5e36ca-b6c1-4565-8261-0b02ac026bce`

### Backend Token Validation
The backend should validate:
- **Issuer**: `https://login.microsoftonline.com/aa5e36ca-b6c1-4565-8261-0b02ac026bce/v2.0`
- **Audience**: `api://4dfc0e9f-2d1e-4e0a-a274-8c21c10606d2/access_as_user`
- **Signature**: Using Azure AD signing keys

## Notes

- Both registrations use the same tenant (`aa5e36ca-b6c1-4565-8261-0b02ac026bce`)
- This configuration follows Microsoft's recommended patterns for SPA + API scenarios
- The API scope format follows the pattern: `api://{api-client-id}/{scope-name}`