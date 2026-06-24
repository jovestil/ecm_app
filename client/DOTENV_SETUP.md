# Angular Environment Variables with dotenv

This document explains how to use environment variables in the Angular application using dotenv for secure configuration management.

## Overview

The application uses dotenv to manage environment variables across different environments (development, production, staging). This allows for secure storage of sensitive configuration like Azure AD credentials without committing them to version control.

The Angular application uses a single `environment.ts` file that reads all configuration from environment variables, eliminating the need for separate environment files per configuration.

## Environment Files

### File Priority (highest to lowest)
1. `.env` (base configuration)

### Available Environment Files

- **`.env.example`** - Template file showing all available variables
- **`.env`** - Base configuration for development

## Environment Variables

All Angular environment variables must be prefixed with `NG_APP_`:

```bash
# Application Settings
NG_APP_ENVIRONMENT=development
NG_APP_API_URL=https://localhost:7001/api/v1

# Azure AD / MSAL Configuration
NG_APP_MSAL_CLIENT_ID=your-azure-ad-client-id-here
NG_APP_MSAL_TENANT_ID=your-azure-ad-tenant-id-here
NG_APP_MSAL_REDIRECT_URI=http://localhost:4200
NG_APP_MSAL_POST_LOGOUT_REDIRECT_URI=http://localhost:4200

# API Scopes
NG_APP_API_SCOPE=api://your-api-scope-here

# Optional Configuration
NG_APP_LOG_LEVEL=debug
NG_APP_CACHE_LOCATION=localStorage
```

## Setup Instructions

### 1. Create Local Environment File

Copy the example file to create your configuration:

```bash
cp .env.example .env
```

### 2. Configure Your Variables

Edit `.env` with your actual values:

```bash
# Your actual Azure AD configuration
NG_APP_MSAL_CLIENT_ID=12345678-1234-1234-1234-123456789abc
NG_APP_MSAL_TENANT_ID=87654321-4321-4321-4321-cba987654321
NG_APP_API_SCOPE=api://mathy-elm-api/access_as_user
```

### 3. Start Development

The single environment.ts file automatically reads from your .env variables:

```bash
# Start development server
npm start

# Build for production (reads from environment variables or CI/CD)
npm run build
```

## NPM Scripts

The package.json includes scripts that automatically load environment variables:

- **`npm start`** - Starts dev server with environment variables from .env
- **`npm run build`** - Builds for production using environment variables (CI/CD or local .env)

## TypeScript Integration

### Environment Interface

The `Environment` interface provides type safety:

```typescript
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
  };
  apiScope: string;
  logLevel: 'debug' | 'info' | 'warn' | 'error';
}
```

### Using Environment Variables

Import and use environment variables in your code:

```typescript
import { environment } from '../environments/environment';

// Use in services
constructor() {
  console.log('API URL:', environment.apiUrl);
  console.log('Client ID:', environment.msal.auth.clientId);
}
```

## Security Best Practices

### 1. Git Ignore Sensitive Files

The `.gitignore` should be configured to exclude:
- `.env` (contains sensitive values)
- `.env.*.local`
- `.env.secrets`
- `*.env.local`

### 2. Never Commit Secrets

- Use `.env.example` for documentation
- Store actual values in `.env` for development
- Use secure environment variable storage in production (Azure Key Vault, etc.)

### 3. Validate Required Variables

The build script validates that required variables are present:

```javascript
// In load-env.js
if (!process.env.NG_APP_MSAL_CLIENT_ID) {
  throw new Error('NG_APP_MSAL_CLIENT_ID is required');
}
```

## Production Deployment

### Azure App Service

Set environment variables in the Azure portal:

1. Go to your App Service
2. Navigate to Configuration > Application settings
3. Add each `NG_APP_*` variable
4. Restart the application

### CI/CD Pipeline

Set environment variables in your build pipeline:

```yaml
# Azure DevOps example
variables:
  NG_APP_MSAL_CLIENT_ID: $(AZURE_AD_CLIENT_ID)
  NG_APP_MSAL_TENANT_ID: $(AZURE_AD_TENANT_ID)
  NG_APP_API_SCOPE: $(API_SCOPE)

steps:
- script: npm run build
  displayName: 'Build Application'
```

## Troubleshooting

### Common Issues

1. **Variables not loading**: Ensure variable names start with `NG_APP_`
2. **Build failures**: Check that all required variables are set
3. **Type errors**: Verify environment interface matches your configuration

### Debug Mode

Set `NG_APP_LOG_LEVEL=debug` to see detailed logging in the application:

```bash
# In .env
NG_APP_LOG_LEVEL=debug
```

### Verify Configuration

Check generated environment files:

```bash
cat src/environments/environment.ts
```

## Development Workflow

1. **Initial Setup**:
   ```bash
   cp .env.example .env
   # Edit .env with your values
   npm start
   ```

2. **Adding New Variables**:
   - Add to `.env.example`
   - Update `Environment` interface in `environment.interface.ts`
   - Update `environment.ts` to read the new variable
   - Add to your `.env`

3. **Switching Environments**:
   ```bash
   # Modify .env to change NG_APP_ENVIRONMENT
   # Or set environment variables in your deployment system
   ```

This setup provides a secure, flexible way to manage environment-specific configuration while maintaining type safety and preventing accidental exposure of sensitive information.