# Swagger UI Documentation

This document explains how to access and use the Swagger UI for the Mathy ELM API.

## Accessing Swagger UI

When running the application in development mode, Swagger UI is available at:

**URL**: `{your-base-url}/swagger`

Based on your current launch settings:
- **HTTPS**: `https://localhost:7001/swagger`
- **HTTP**: `http://localhost:5001/swagger`

The exact URL depends on your `launchSettings.json` configuration.

## Features

### 🔍 Interactive API Documentation
- Browse all available endpoints
- View request/response schemas
- See detailed parameter descriptions
- Check response codes and examples

### 🔐 JWT Authentication Support
- Click the "Authorize" button (🔒) in the top right
- Enter your JWT token in the format: `Bearer your-jwt-token-here`
- All authenticated endpoints will use this token automatically

### 🧪 API Testing
- Click "Try it out" on any endpoint
- Fill in parameters and request body
- Execute requests directly from the browser
- View live responses with status codes and data

## Available Endpoints

### Authentication (`/api/v1/auth`)
- `GET /health` - Health check (no auth required)
- `GET /me` - Get current user info
- `GET /validate` - Validate JWT token
- `GET /companies` - Get user's accessible companies

### Employees (`/api/v1/employees`)
- `GET /search` - Search employees by name or number
- `GET /{employeeNumber}` - Get employee details
- `GET /company/{companyCode}` - Get employees by company (paginated)

### Layoff Requests (`/api/v1/layoffrequests`)
- `GET /` - Get layoff requests (paginated)
- `GET /{id}` - Get specific layoff request
- `POST /` - Create new layoff request
- `PUT /{id}` - Update layoff request
- `POST /{id}/submit` - Submit layoff request

## Getting a JWT Token

To test authenticated endpoints, you need a JWT token from Azure AD:

1. **From Angular Client**: Login to the Angular app and copy the token from browser dev tools
2. **From Postman/Insomnia**: Set up OAuth 2.0 with Azure AD
3. **For Development**: Use Azure CLI or Azure PowerShell to get a token

## Authorization Levels

- **Public**: `/auth/health` - No authentication required
- **Authenticated**: Most endpoints - Valid JWT token required
- **Company-Based**: Employee and request endpoints - Token + company access required
- **Role-Based**: Some operations require specific roles (HRAdmin, Manager, SystemAdmin)

## Response Format

All API responses follow a consistent format:

```json
{
  "success": true,
  "message": "Optional message",
  "data": {
    // Response data here
  }
}
```

For paginated responses:
```json
{
  "success": true,
  "data": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

## Error Responses

Common HTTP status codes:
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (missing/invalid token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `500` - Internal Server Error

## Development Tips

1. **XML Documentation**: API descriptions are generated from XML comments in the code
2. **Schema Validation**: Request/response schemas are automatically generated from DTOs
3. **Try It Out**: Use the interactive features to test API behavior
4. **Export**: You can export the OpenAPI specification for use in other tools

## Troubleshooting

### Swagger UI Not Loading
- Ensure you're running in Development environment
- Check that the URL is correct: `https://localhost:7001/swagger`
- Verify the application started without errors

### Authorization Issues
- Make sure your JWT token is valid and not expired
- Include "Bearer " prefix when entering the token
- Check that your token has the required claims and roles

### CORS Issues
- Swagger UI should work since it's served from the same origin
- If testing from external tools, ensure CORS is properly configured