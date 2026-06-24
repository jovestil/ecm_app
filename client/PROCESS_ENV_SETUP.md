# Angular process.env Implementation

This document explains how Angular environment files now use `process.env` to directly read from `.env` files at build time.

## Overview

The Angular environment configuration has been updated to use `process.env` variables that are injected at build time using webpack's DefinePlugin. This provides a more standard Node.js-like experience for environment variable management.

## Implementation Details

### Webpack Configuration

A custom webpack configuration (`webpack.config.js`) loads environment variables from `.env` files and injects them into the browser bundle using webpack's DefinePlugin.

**Environment File Loading Priority:**
1. `.env.{environment}.local` (e.g., `.env.development.local`)
2. `.env.local`
3. `.env.{environment}` (e.g., `.env.development`)
4. `.env`

### Environment Files Structure

All environment files now use `process.env` syntax:

```typescript
// src/environments/environment.ts
export const environment: Environment = {
  production: process.env['NG_APP_ENVIRONMENT'] === 'production',
  configuration: process.env['NG_APP_ENVIRONMENT'] || 'development',
  apiUrl: process.env['NG_APP_API_URL'] || 'https://localhost:7001/api/v1',
  msal: {
    auth: {
      clientId: process.env['NG_APP_MSAL_CLIENT_ID'] || 'your-client-id-here',
      authority: `https://login.microsoftonline.com/${process.env['NG_APP_MSAL_TENANT_ID'] || 'your-tenant-id-here'}`,
      // ... more config
    }
  },
  devname: process.env['NG_APP_DEV_NAME']
};
```

## Environment Variables

All variables must be prefixed with `NG_APP_` to be available in the browser:

```bash
# .env.development
NG_APP_ENVIRONMENT=development
NG_APP_API_URL=https://localhost:7001/api/v1
NG_APP_MSAL_CLIENT_ID=2cf3ed6e-8bb3-43d4-a1c8-7bbecd85753d
NG_APP_MSAL_TENANT_ID=aa5e36ca-b6c1-4565-8261-0b02ac026bce
NG_APP_MSAL_REDIRECT_URI=http://localhost:4200
NG_APP_MSAL_POST_LOGOUT_REDIRECT_URI=http://localhost:4200/logout
NG_APP_API_SCOPE=api://e48a9e2c-7cee-4193-801f-5cedd28dd0f3/api_access
NG_APP_LOG_LEVEL=debug
NG_APP_CACHE_LOCATION=localStorage
NG_APP_DEV_NAME=Reindel
```

## Build Configuration

### Angular.json Changes

Updated to use `@angular-builders/custom-webpack:browser` builder:

```json
{
  "build": {
    "builder": "@angular-builders/custom-webpack:browser",
    "options": {
      "customWebpackConfig": {
        "path": "./webpack.config.js"
      }
    }
  }
}
```

### Package.json Scripts

Simplified scripts since webpack handles environment loading:

```json
{
  "start": "ng serve",
  "build": "ng build",
  "build:dev": "ng build --configuration development"
}
```

## TypeScript Support

Added TypeScript declarations for `process.env`:

```typescript
// src/types/process-env.d.ts
declare var process: {
  env: {
    [key: string]: string | undefined;
  };
};
```

Updated `tsconfig.app.json`:

```json
{
  "compilerOptions": {
    "types": ["node"]
  }
}
```

## Usage Examples

### Reading Environment Variables

```typescript
import { environment } from '../environments/environment';

// Use processed environment object
console.log('API URL:', environment.apiUrl);
console.log('Dev Name:', environment.devname);

// Or access process.env directly (though not recommended)
console.log('Raw env:', process.env['NG_APP_DEV_NAME']);
```

### Environment-Specific Builds

```bash
# Development build (loads .env.development)
npm run build:dev

# Production build (loads .env.production)  
npm run build

# Development server
npm start
```

## Build Output

When building, you'll see webpack debug output:

```
🔧 Webpack: Loaded environment variables: [
  'NG_APP_ENVIRONMENT',
  'NG_APP_API_URL',
  'NG_APP_MSAL_CLIENT_ID',
  'NG_APP_MSAL_TENANT_ID',
  // ... all loaded variables
]
```

## Benefits

1. **Standard Process**: Uses familiar `process.env` syntax from Node.js
2. **Build-Time Injection**: Variables are replaced at build time, not runtime
3. **Type Safety**: Full TypeScript support with environment interface
4. **Automatic Loading**: Webpack automatically loads appropriate `.env` files
5. **Environment Isolation**: Different configurations for dev, staging, production

## Security Notes

- Environment variables are injected at **build time**
- All `NG_APP_*` variables become visible in the browser bundle
- Sensitive values should be in `.env.local` files (gitignored)
- Production builds should use secure environment variable injection

## Troubleshooting

### Build Errors

If you see webpack errors:
1. Ensure all `NG_APP_*` variables are properly set
2. Check that `.env` files exist and are readable
3. Verify webpack.config.js is in the project root

### Missing Variables

If environment variables aren't loading:
1. Ensure variable names start with `NG_APP_`
2. Check file naming (`.env.development` not `.env.dev`)
3. Verify file encoding (should be UTF-8)

### TypeScript Errors

If you see `process is not defined` errors:
1. Ensure `@types/node` is installed
2. Check `tsconfig.app.json` includes `"types": ["node"]`
3. Verify `process-env.d.ts` exists and is included

## Migration from Old System

The old build scripts (`npm run env:dev`, etc.) are no longer needed since webpack handles environment loading automatically. However, they are still available for generating static environment files if needed for other purposes.

This new system provides better integration with modern JavaScript tooling while maintaining the security and type safety of the previous implementation.