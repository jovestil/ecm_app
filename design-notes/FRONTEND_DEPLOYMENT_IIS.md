# ECM Frontend Deployment Guide - IIS

This guide provides complete step-by-step instructions for deploying the ECM (Employee Change Management) Angular frontend application to IIS.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Server Preparation](#server-preparation)
3. [Configure Environment Variables](#configure-environment-variables)
4. [Build the Angular Application](#build-the-angular-application)
5. [SSL Certificate Setup](#ssl-certificate-setup)
6. [IIS Configuration](#iis-configuration)
7. [URL Rewrite Configuration](#url-rewrite-configuration)
8. [Firewall Configuration](#firewall-configuration)
9. [Verification and Testing](#verification-and-testing)
10. [Troubleshooting](#troubleshooting)
11. [Updating the Test Server](#updating-the-test-server)

---

## Prerequisites

### Required Software

1. **IIS (Internet Information Services)**
2. **URL Rewrite Module** (critical for Angular routing)
3. **Node.js** (for building - required on build machine)

### Verify Prerequisites

```powershell
# Check if IIS is installed
Get-WindowsFeature -Name Web-Server

# Check URL Rewrite Module
Get-WebGlobalModule | Where-Object { $_.Name -like "*Rewrite*" }

# Check Node.js version (on build machine)
node --version
npm --version
```

### Install Missing Prerequisites

**Install IIS:**
```powershell
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
```

**Install URL Rewrite Module:**
1. Download from: https://www.iis.net/downloads/microsoft/url-rewrite
2. Run the installer
3. Restart IIS: `iisreset`

**Install Node.js:**
1. Download from: https://nodejs.org/ (LTS version)
2. Run the installer

---

## Server Preparation

### Create Deployment Folder

```powershell
New-Item -Path "C:\inetpub\wwwroot\ecm-frontend" -ItemType Directory -Force
```

### Set Folder Permissions

```powershell
$folderPath = "C:\inetpub\wwwroot\ecm-frontend"
$acl = Get-Acl $folderPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $folderPath $acl
```

### Configure Hosts File (If Using Local DNS)

```powershell
notepad C:\Windows\System32\drivers\etc\hosts
```

Add:
```
127.0.0.1    ecmtest.corpmts.com
127.0.0.1    ecmtest-api.corpmts.com
```

For network access from other machines, also add your server IP:
```
10.2.111.58    ecmtest.corpmts.com
10.2.111.58    ecmtest-api.corpmts.com
```

Flush DNS:
```powershell
ipconfig /flushdns
```

---

## Configure Environment Variables

### Navigate to Client Folder

```powershell
cd "C:\repo\Mathy\mathy_elm_app\client"
```

### Create .env File

```powershell
Copy-Item .env.example .env
```

### Edit .env File for Test Server

Update the `.env` file with test server values:

```env
# Test Server Environment Configuration
NG_APP_ENVIRONMENT=production
NG_APP_API_URL=https://ecmtest-api.corpmts.com/api/v1

# Azure AD / MSAL Configuration
NG_APP_MSAL_CLIENT_ID=6ad3d255-c90f-44e4-8fc4-ce4787ab873a
NG_APP_MSAL_TENANT_ID=c0057058-c437-46cb-9d12-6202ad373107
NG_APP_MSAL_REDIRECT_URI=https://ecmtest.corpmts.com
NG_APP_MSAL_POST_LOGOUT_REDIRECT_URI=https://ecmtest.corpmts.com

# API Scopes
NG_APP_API_SCOPE=api://145a165e-a6ab-42ba-be05-4b756db6ce38/access_as_user

# Settings
NG_APP_LOG_LEVEL=warn
NG_APP_CACHE_LOCATION=localStorage
```

**Important:** The `.env` file is environment-specific and should NOT be committed to git. Each environment (local, test server) maintains its own `.env` file.

---

## Build the Angular Application

### Install Dependencies

```powershell
cd "C:\repo\Mathy\mathy_elm_app\client"
npm install
```

### Fix CSS Budget Limits (If Build Fails)

If the build fails with CSS budget errors, update `angular.json`:

```powershell
$angularJson = Get-Content "angular.json" -Raw | ConvertFrom-Json
$angularJson.projects.'mathy-elm-client'.architect.build.configurations.production.budgets = @(
    @{
        type = "initial"
        maximumWarning = "2mb"
        maximumError = "5mb"
    },
    @{
        type = "anyComponentStyle"
        maximumWarning = "20kb"
        maximumError = "50kb"
    }
)
$angularJson | ConvertTo-Json -Depth 20 | Set-Content "angular.json"
```

### Build for Production

```powershell
npm run build
```

### Copy Build Output to Server

**Important:** The build output is in `dist\mathy-elm-client\` (not `browser` subfolder).

```powershell
Copy-Item -Path "dist\mathy-elm-client\*" -Destination "C:\inetpub\wwwroot\ecm-frontend" -Recurse -Force
```

---

## SSL Certificate Setup

### Use Existing Certificate

If you created a certificate for the backend that includes both domains, use that.

### Verify Certificate

```powershell
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*ecmtest*" }
```

### Create New Certificate (If Needed)

```powershell
$cert = New-SelfSignedCertificate `
    -DnsName "ecmtest.corpmts.com", "ecmtest-api.corpmts.com", "localhost" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(5) `
    -FriendlyName "ECM Development Certificate"

$cert.Thumbprint
```

---

## IIS Configuration

### Create Application Pool

```powershell
Import-Module WebAdministration
New-WebAppPool -Name "Mathy-ECM-Frontend-Pool"
Set-ItemProperty "IIS:\AppPools\Mathy-ECM-Frontend-Pool" -Name "managedRuntimeVersion" -Value ""
```

### Create IIS Site (HTTPS Only)

Use IIS Manager to create the site on the default HTTPS port (443) so the URL doesn't require a port number.

1. Open **IIS Manager** (`inetmgr`)
2. Click **Application Pools** > Right-click > **Add Application Pool**
   - **Name:** `Mathy-ECM-Frontend-Pool`
   - **.NET CLR Version:** `No Managed Code`
   - **Managed Pipeline Mode:** `Integrated`
3. Right-click **Sites** > **Add Website**
   - **Site name:** `Mathy-ECM-Frontend`
   - **Application pool:** `Mathy-ECM-Frontend-Pool`
   - **Physical path:** `C:\inetpub\wwwroot\ecm-frontend`
   - **Binding Type:** `https`
   - **Port:** `443`
   - **Host name:** `ecmtest.corpmts.com`
   - **SSL Certificate:** Select your certificate
4. Click **OK**

### Alternative: PowerShell Configuration

```powershell
# Create Website with HTTPS binding on port 443
New-Website -Name "Mathy-ECM-Frontend" `
    -PhysicalPath "C:\inetpub\wwwroot\ecm-frontend" `
    -ApplicationPool "Mathy-ECM-Frontend-Pool" `
    -Ssl -Port 443 `
    -HostHeader "ecmtest.corpmts.com"

# Bind SSL certificate
$thumbprint = (Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*ecmtest*" }).Thumbprint
$binding = Get-WebBinding -Name "Mathy-ECM-Frontend" -Protocol "https"
$binding.AddSslCertificate($thumbprint, "My")
```

---

## URL Rewrite Configuration

**Critical:** Without URL Rewrite, refreshing any Angular route returns 404.

### Create web.config

Open Notepad and create `C:\inetpub\wwwroot\ecm-frontend\web.config`:

```powershell
notepad "C:\inetpub\wwwroot\ecm-frontend\web.config"
```

Paste this content and save:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="Angular Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".woff" />
      <mimeMap fileExtension=".woff" mimeType="font/woff" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".woff2" mimeType="font/woff2" />
    </staticContent>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
        <add name="X-Frame-Options" value="SAMEORIGIN" />
        <add name="X-XSS-Protection" value="1; mode=block" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
</configuration>
```

---

## Firewall Configuration

Port 443 (HTTPS) is typically already open. If not, open it in Windows Firewall:

```powershell
New-NetFirewallRule -DisplayName "ECM Frontend HTTPS 443" -Direction Inbound -LocalPort 443 -Protocol TCP -Action Allow
```

---

## Verification and Testing

### Restart IIS

```powershell
iisreset
```

### Verify Site is Running

```powershell
Get-Website | Where-Object { $_.Name -like "*Mathy*" }
```

The site should show **State: Started**.

### Start Site if Stopped

```powershell
Start-Website -Name "Mathy-ECM-Frontend"
```

### Test the Application

Open browser and navigate to:
```
https://ecmtest.corpmts.com
```

### Verify Tests

1. **Homepage loads** - Angular app renders
2. **Angular Routing** - Navigate to a route, then refresh - should work
3. **Azure AD Login** - Click login, verify redirect works
4. **API Connection** - Verify data loads from backend

### Verify Deployment Files

```powershell
Get-ChildItem "C:\inetpub\wwwroot\ecm-frontend"
```

Expected files:
- `index.html`
- `main-*.js`
- `polyfills-*.js`
- `styles-*.css`
- `web.config`
- `assets/` folder
- Various font files (`.woff`, `.woff2`, etc.)

---

## Troubleshooting

### Site Won't Start - "Cannot create a file when that file already exists"

This is usually a port binding conflict. Another site may already be using port 443. Solution:

```powershell
# Check what's using port 443
netstat -ano | findstr ":443"

# Remove and recreate with HTTPS only
Remove-Website -Name "Mathy-ECM-Frontend"

New-Website -Name "Mathy-ECM-Frontend" `
    -PhysicalPath "C:\inetpub\wwwroot\ecm-frontend" `
    -ApplicationPool "Mathy-ECM-Frontend-Pool" `
    -Ssl -Port 443 `
    -HostHeader "ecmtest.corpmts.com"

$thumbprint = (Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*ecmtest*" }).Thumbprint
$binding = Get-WebBinding -Name "Mathy-ECM-Frontend" -Protocol "https"
$binding.AddSslCertificate($thumbprint, "My")
```

### ERR_CONNECTION_REFUSED

1. Check if site is running:
   ```powershell
   Get-Website | Where-Object { $_.Name -like "*Mathy*Frontend*" }
   ```
2. Start the site if stopped:
   ```powershell
   Start-Website -Name "Mathy-ECM-Frontend"
   ```
3. Check firewall rule exists:
   ```powershell
   Get-NetFirewallRule -DisplayName "*ECM*"
   ```

### 404 Error on Page Refresh

1. Verify URL Rewrite Module is installed:
   ```powershell
   Get-WebGlobalModule | Where-Object { $_.Name -like "*Rewrite*" }
   ```
2. Verify web.config exists and has content:
   ```powershell
   Get-Content "C:\inetpub\wwwroot\ecm-frontend\web.config"
   ```

### CSS Budget Build Errors

If build fails with "exceeded maximum budget" errors, update angular.json budgets (see Build section).

### CORS Errors

Backend must allow origin: `https://ecmtest.corpmts.com`

### Azure AD Login Redirect Issues

1. Azure Portal > App Registrations > Your App > Authentication
2. Add Redirect URI: `https://ecmtest.corpmts.com`
3. Ensure ID tokens are enabled

### Blank Page

Check browser console (F12) for 404 or JS errors. Common causes:
- Files not copied correctly
- web.config missing or malformed

### API Connection Failing

1. Check Network tab in browser dev tools
2. Verify API URL in .env is correct
3. Rebuild and redeploy

---

## Updating the Test Server

After pushing changes from your local development machine, follow these steps on the test server to deploy the updates.

### Important: The .env file is NOT overwritten

Unlike the backend, the frontend `.env` file exists in the source folder, not the deployment folder. It won't be overwritten during deployment, but you should verify it has the correct test server values before building.

### Using Visual Studio

1. **Open** the solution in Visual Studio
2. **Pull** latest changes:
   - Go to **Git** menu → **Pull**
3. **Verify `.env`** has test server values (not localhost)
4. **Build and deploy** using PowerShell:
   ```powershell
   cd "C:\repo\Mathy\mathy_elm_app\client"
   npm run build
   Copy-Item -Path "dist\mathy-elm-client\*" -Destination "C:\inetpub\wwwroot\ecm-frontend" -Recurse -Force
   iisreset
   ```

### Using Command Line

```powershell
# Navigate to client folder
cd "C:\repo\Mathy\mathy_elm_app\client"

# Pull latest changes
git pull

# Install new dependencies (if any)
npm install

# Build for production
npm run build

# Deploy to IIS
Copy-Item -Path "dist\mathy-elm-client\*" -Destination "C:\inetpub\wwwroot\ecm-frontend" -Recurse -Force

# Restart IIS
iisreset
```

### Quick One-Liner

```powershell
cd "C:\repo\Mathy\mathy_elm_app\client" && git pull && npm install && npm run build && Copy-Item -Path "dist\mathy-elm-client\*" -Destination "C:\inetpub\wwwroot\ecm-frontend" -Recurse -Force && iisreset
```

---

## Quick Reference Commands

```powershell
# Restart IIS
iisreset

# Stop/Start Website
Stop-Website -Name "Mathy-ECM-Frontend"
Start-Website -Name "Mathy-ECM-Frontend"

# Check site status
Get-Website | Where-Object { $_.Name -like "*Mathy*Frontend*" }

# Check what's using port 443
netstat -ano | findstr ":443"

# Rebuild and redeploy
cd "C:\repo\Mathy\mathy_elm_app\client"
npm run build
Copy-Item -Path "dist\mathy-elm-client\*" -Destination "C:\inetpub\wwwroot\ecm-frontend" -Recurse -Force
iisreset
```

---

## Azure AD Configuration Checklist

- [ ] Redirect URI: `https://ecmtest.corpmts.com`
- [ ] Logout URL: `https://ecmtest.corpmts.com`
- [ ] ID tokens enabled
- [ ] API Permissions: `User.Read`, your API scope

---

## Deployment Checklist

- [ ] IIS installed
- [ ] URL Rewrite Module installed
- [ ] Node.js installed (build machine)
- [ ] Deployment folder created (`C:\inetpub\wwwroot\ecm-frontend`)
- [ ] Folder permissions set for IIS_IUSRS
- [ ] SSL certificate installed
- [ ] Hosts file configured (if using local DNS)
- [ ] `.env` configured with test server URLs
- [ ] CSS budgets updated in angular.json (if needed)
- [ ] Angular app built (`npm run build`)
- [ ] Files copied to deployment folder
- [ ] web.config created with URL Rewrite rules
- [ ] Application Pool created (`Mathy-ECM-Frontend-Pool`, No Managed Code)
- [ ] IIS Site created (`Mathy-ECM-Frontend`, HTTPS only, port 443)
- [ ] SSL certificate bound to site
- [ ] Firewall port 443 opened (usually already open for HTTPS)
- [ ] Site is running (Started state)
- [ ] Azure AD redirect URIs configured
- [ ] Application loads and works
