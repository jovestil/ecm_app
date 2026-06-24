# Mathy ELM Project Setup Guide

This guide provides step-by-step instructions for setting up the Mathy Employee Change Management (ELM) system on a new development machine.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation Steps](#installation-steps)
- [Configuration](#configuration)
- [Database Setup](#database-setup)
- [Running the Application](#running-the-application)
- [Troubleshooting](#troubleshooting)
- [Verification Checklist](#verification-checklist)

---

## Prerequisites

### Required Software

#### 1. .NET 9 SDK
**Required for:** Backend API development and Entity Framework migrations

**Installation:**
- Download from: https://dotnet.microsoft.com/download/dotnet/9.0
- Verify installation:
  ```bash
  dotnet --version
  # Should show: 9.0.x
  ```

#### 2. Node.js LTS (v20.x or v22.x)
**Required for:** Angular development and npm package management

**Installation:**
- Download from: https://nodejs.org/
- Verify installation:
  ```bash
  node --version  # Should show: v20.x or v22.x
  npm --version   # Should show: v10.x
  ```

#### 3. Angular CLI 17.x
**Required for:** Angular project development

**Installation:**
```bash
npm install -g @angular/cli@17
ng version  # Verify installation
```

#### 4. SQL Server
**Required for:** Database

**Options:**
- **SQL Server Express LocalDB** (Included with Visual Studio)
- **SQL Server Developer Edition** (Free): https://www.microsoft.com/sql-server/sql-server-downloads
- **SQL Server in Docker:**
  ```bash
  docker pull mcr.microsoft.com/mssql/server:2022-latest
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
    -p 1433:1433 --name mathy-elm-db \
    -d mcr.microsoft.com/mssql/server:2022-latest
  ```

#### 5. Entity Framework Core Tools
**Required for:** Database migrations

**Installation:**
```bash
dotnet tool install --global dotnet-ef
dotnet ef --version  # Verify installation
```

#### 6. Git
**Required for:** Version control

**Installation:**
- Windows: https://git-scm.com/
- Linux/WSL: `sudo apt-get install git`
- macOS: `brew install git`

### Optional Software

- **Visual Studio 2022** or **VS Code** (Recommended IDE)
- **SQL Server Management Studio (SSMS)** (For database management)
- **Postman** (For API testing)

---

## Installation Steps

### Step 1: Clone the Repository

```bash
# Clone the repository
git clone https://github.com/tritontekph/mathy_elm_app.git

# Navigate to project directory
cd mathy_elm_app

# Checkout the appropriate branch
git checkout initial-structure
```

### Step 2: Backend Setup

#### 2.1. Restore NuGet Packages

```bash
# Navigate to server directory
cd server

# Restore all NuGet packages
dotnet restore
```

#### 2.2. Configure Application Settings

Create a file named `appsettings.Local.json` in `/server/src/Mathy.ELM.Api/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=MathyELM;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "AzureAd": {
    "TenantId": "YOUR_AZURE_AD_TENANT_ID",
    "ClientId": "YOUR_AZURE_AD_CLIENT_ID",
    "Audience": "api://YOUR_AZURE_AD_CLIENT_ID/access_as_user"
  },
  "ViewpointApi": {
    "BaseUrl": "https://api.xchange.trimble.com/connect/v1/direct/subscribers",
    "SubscriberCode": "YOUR_VIEWPOINT_SUBSCRIBER_CODE",
    "ApplicationKey": "YOUR_VIEWPOINT_APPLICATION_KEY"
  },
  "AzureEmail": {
    "ConnectionString": "YOUR_AZURE_COMMUNICATION_SERVICE_CONNECTION_STRING",
    "FromAddress": "DoNotReply@yourdomain.com"
  },
  "ActiveDirectory": {
    "OUPath": "LDAP://OU=User,OU=Domain Accounts,DC={domain},DC=com",
    "PasswordPolicy": {
      "Length": 12,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireNumbers": true,
      "RequireSpecialCharacters": true,
      "AllowedSpecialCharacters": "!@#$%^&*"
    }
  }
}
```

**Note:** This file is git-ignored and will not be committed to version control.

#### 2.3. Optional: Create Server .env File

If using environment variables, copy and update `.env.example`:

```bash
cd server
cp .env.example .env
# Edit .env with your values
```

### Step 3: Frontend Setup

#### 3.1. Install NPM Dependencies

```bash
# Navigate to client directory
cd client

# Install all npm packages
npm install
```

This will install:
- Angular 17.3
- PrimeNG UI components
- Azure MSAL authentication
- SignalR for real-time updates
- Other dependencies

#### 3.2. Configure Environment Variables

Create a `.env` file from the example:

```bash
cp .env.example .env
```

Edit `.env` with your configuration:

```env
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

# Optional: Additional Configuration
NG_APP_LOG_LEVEL=debug
NG_APP_CACHE_LOCATION=localStorage
NG_APP_DEV_NAME=YourName
```

---

## Configuration

### Backend Configuration Details

The backend uses a layered configuration approach:
1. `appsettings.json` - Base configuration (checked into source control)
2. `appsettings.Development.json` - Development overrides
3. `appsettings.Local.json` - **Local machine settings (git-ignored, create this)**

#### Connection String Formats

**SQL Server with Windows Authentication (LocalDB):**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MathyELM;Integrated Security=true;TrustServerCertificate=true"
```

**SQL Server with SQL Authentication:**
```json
"DefaultConnection": "Server=localhost;Database=MathyELM;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

**SQL Server in Docker:**
```json
"DefaultConnection": "Server=localhost,1433;Database=MathyELM;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

#### Azure AD Configuration

1. **Register an application** in Azure Portal:
   - Go to Azure Active Directory > App Registrations
   - Create new registration for the API
   - Create new registration for the Client (or use the same)
   - Note the **Tenant ID** and **Client ID**

2. **Configure API Permissions:**
   - Add API permissions for Microsoft Graph (if needed)
   - Expose an API scope: `api://<client-id>/access_as_user`

3. **Update appsettings.Local.json** with your values

### Frontend Configuration Details

The Angular application uses `.env` files for environment-specific configuration:
- `.env` - Local development (git-ignored)
- `.env.example` - Template for team members

**Important:** Make sure your `NG_APP_API_URL` matches the backend API URL.

---

## Database Setup

### Step 1: Ensure SQL Server is Running

**For LocalDB:**
```bash
sqllocaldb start mssqllocaldb
```

**For Docker:**
```bash
docker start mathy-elm-db
```

### Step 2: Run Database Migrations

```bash
# Navigate to API project
cd server/src/Mathy.ELM.Api

# Apply all migrations to create database
dotnet ef database update

# Verify migration succeeded
dotnet ef migrations list
```

### Step 3: Verify Database Creation

Connect to your SQL Server and verify the `MathyELM` database exists with tables:
- `Companies`
- `Employees`
- `HRRequests`
- `HRRequestDetails`
- `NewHireRequestDetails`
- `PromotionRequestDetails`
- `LayoffRequestDetails`
- `TerminationRequestDetails`
- And more...

---

## Running the Application

### Start Backend API

"ConnectionStrings": {
        "DefaultConnection": "Server=MAT-IIS;Database=MathyELM;User Id=sa;Password=Test1234;TrustServerCertificate=True;"
    },

Open a terminal and run:

```bash
# Navigate to API project
cd server/src/Mathy.ELM.Api

# Run the API
dotnet run
```

The API will start on:
- **HTTPS:** https://localhost:7001
- **HTTP:** http://localhost:5001
- **Swagger UI:** https://localhost:7001/swagger

### Start Frontend Application

Open a **new terminal** and run:

```bash
# Navigate to client directory
cd client

# Start Angular development server
npm start
ng serve --ssl
```

The application will start on:
- **Development:** http://localhost:4200

### Access the Application

1. Open browser: http://localhost:4200
2. You'll be redirected to Azure AD login
3. After authentication, you'll land on the dashboard

---

## Troubleshooting

### Common Issues

#### 1. .NET SDK Not Found
**Error:** `The command 'dotnet' is not recognized`

**Solution:**
- Ensure .NET 9 SDK is installed
- Restart terminal/IDE after installation
- Check PATH environment variable includes .NET

#### 2. Database Connection Failed
**Error:** `A network-related or instance-specific error occurred`

**Solution:**
- Verify SQL Server is running
- Check connection string in `appsettings.Local.json`
- Ensure firewall allows port 1433
- For Docker: `docker ps` to verify container is running

#### 3. Migration Failed
**Error:** `Unable to create an object of type 'ApplicationDbContext'`

**Solution:**
```bash
# Make sure you're in the API project directory
cd server/src/Mathy.ELM.Api

# Try explicit connection string
dotnet ef database update --connection "Server=localhost;Database=MathyELM;..."
```

#### 4. Angular Build Errors
**Error:** `Module not found` or `Cannot find module`

**Solution:**
```bash
# Clear npm cache
npm cache clean --force

# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

#### 5. Azure AD Authentication Failed
**Error:** `AADSTS50011: The reply URL specified in the request does not match`

**Solution:**
- In Azure Portal, add `http://localhost:4200` to Redirect URIs
- Add `http://localhost:4200` to Logout URLs
- Ensure Client ID and Tenant ID are correct in `.env`

#### 6. CORS Errors
**Error:** `Access to XMLHttpRequest has been blocked by CORS policy`

**Solution:**
- Verify backend `appsettings.json` includes `http://localhost:4200` in CORS origins
- Restart backend API after changes

#### 7. Port Already in Use
**Error:** `Address already in use` or `Port 4200/7001 is already in use`

**Solution:**
```bash
# Find process using the port (Windows)
netstat -ano | findstr :4200

# Kill the process
taskkill /PID <process_id> /F

# Or use different port
ng serve --port 4201
```

#### 8. Entity Framework Tools Not Found
**Error:** `Could not execute because the specified command or file was not found`

**Solution:**
```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef
```

### Getting Help

If you encounter issues not covered here:
1. Check the logs in the browser console (F12)
2. Check backend logs in the terminal
3. Review the `design-notes/` folder for additional documentation
4. Contact the development team

---

## Verification Checklist

After completing setup, verify the following:

### Backend Verification

- [ ] `dotnet --version` shows 9.0.x
- [ ] `dotnet restore` completes without errors
- [ ] `dotnet build` succeeds in `/server` directory
- [ ] `dotnet ef migrations list` shows all migrations
- [ ] `dotnet ef database update` creates database
- [ ] Backend API runs on https://localhost:7001
- [ ] Swagger UI accessible at https://localhost:7001/swagger
- [ ] Database `MathyELM` exists with tables

### Frontend Verification

- [ ] `node --version` shows v20.x or v22.x
- [ ] `npm --version` shows v10.x
- [ ] `ng version` shows Angular CLI 17.x
- [ ] `npm install` completes without errors
- [ ] `.env` file exists with proper configuration
- [ ] `npm start` runs without errors
- [ ] Application loads at http://localhost:4200
- [ ] Azure AD login works
- [ ] Dashboard loads after authentication

### Integration Verification

- [ ] Frontend can call backend API
- [ ] Employee search works (calls Viewpoint API)
- [ ] Create HR request works
- [ ] Background jobs process (Hangfire dashboard: https://localhost:7001/hangfire)
- [ ] Real-time updates work (SignalR)

---

## Next Steps

Once setup is complete:

1. **Review Architecture:** Read `design-notes/ARCHITECTURE.md`
2. **Understand API Design:** Review `design-notes/API_DESIGN.md`
3. **Check Azure AD Setup:** See `design-notes/AZURE_AD_APP_REGISTRATIONS.md`
4. **Explore UI Components:** Review `design-notes/PRIMENG_COMPONENTS.md`
5. **Start Development:** Create a feature branch and begin coding!

---

## Additional Resources

- **Project Documentation:** `/design-notes/` folder
- **.NET Documentation:** https://learn.microsoft.com/dotnet/
- **Angular Documentation:** https://angular.io/docs
- **PrimeNG Components:** https://primeng.org/
- **Azure AD Authentication:** https://learn.microsoft.com/azure/active-directory/
- **Entity Framework Core:** https://learn.microsoft.com/ef/core/

---

## Support

For questions or issues:
- Review existing documentation in the `design-notes/` folder
- Check the troubleshooting section above
- Contact the development team

---

**Last Updated:** 2025-10-02
