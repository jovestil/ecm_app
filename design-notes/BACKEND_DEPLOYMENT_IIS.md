# ECM Backend Deployment Guide - IIS

This guide provides complete step-by-step instructions for deploying the ECM (Employee Change Management) .NET Core 9 Web API to IIS.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Server Preparation](#server-preparation)
3. [Publish the API](#publish-the-api)
4. [SSL Certificate Setup](#ssl-certificate-setup)
5. [IIS Configuration](#iis-configuration)
6. [Application Configuration](#application-configuration)
7. [Verification and Testing](#verification-and-testing)
8. [Troubleshooting](#troubleshooting)
9. [Updating the Test Server](#updating-the-test-server)

---

## Prerequisites

### Required Software

Before deploying, ensure the following are installed on the server:

1. **IIS (Internet Information Services)**
2. **.NET Core Hosting Bundle** (for .NET 9)
3. **URL Rewrite Module** (for Angular frontend routing)

### Verify Prerequisites

Run these commands in PowerShell as Administrator:

```powershell
# Check if IIS is installed
Get-WindowsFeature -Name Web-Server

# Check .NET Core Hosting Bundle
Get-ChildItem "HKLM:\SOFTWARE\Microsoft\ASP.NET Core\Shared Framework" -ErrorAction SilentlyContinue

# Alternative: Check installed .NET runtimes
dotnet --list-runtimes

# Check URL Rewrite Module
Get-WebGlobalModule | Where-Object { $_.Name -like "*Rewrite*" }
```

### Install Missing Prerequisites

**Install IIS:**
```powershell
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
```

**Install .NET Core Hosting Bundle:**
1. Download from: https://dotnet.microsoft.com/download/dotnet/9.0
2. Select "Hosting Bundle" under ASP.NET Core Runtime
3. Run the installer
4. Restart IIS: `iisreset`

**Install URL Rewrite Module:**
1. Download from: https://www.iis.net/downloads/microsoft/url-rewrite
2. Run the installer
3. Restart IIS: `iisreset`

---

## Server Preparation

### Create Deployment Folder

```powershell
# Create the deployment folder
New-Item -Path "C:\inetpub\wwwroot\ecm-api" -ItemType Directory -Force

# Create logs folder for stdout logging
New-Item -Path "C:\inetpub\wwwroot\ecm-api\logs" -ItemType Directory -Force
```

### Set Folder Permissions

```powershell
# Grant IIS_IUSRS full control
$folderPath = "C:\inetpub\wwwroot\ecm-api"
$acl = Get-Acl $folderPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $folderPath $acl

# Verify permissions
Get-Acl $folderPath | Format-List
```

### Configure Hosts File (For Local DNS Resolution)

If the domain is not in public DNS, add entries to the hosts file:

```powershell
# Open hosts file in Notepad as Administrator
notepad C:\Windows\System32\drivers\etc\hosts
```

Add these lines (replace IP with your server IP):
```
127.0.0.1    ecmtest-api.corpmts.com
127.0.0.1    ecmtest.corpmts.com
```

For network access from other machines, also add:
```
10.2.111.58    ecmtest-api.corpmts.com
10.2.111.58    ecmtest.corpmts.com
```

Flush DNS cache after editing:
```powershell
ipconfig /flushdns
```

---

## Publish the API

### Option 1: Publish from Command Line

Navigate to the API project folder and run:

```powershell
cd "C:\path\to\your\project\server\src\Mathy.ELM.Api"

# Publish in Release mode
dotnet publish -c Release -o "C:\inetpub\wwwroot\ecm-api"
```

### Option 2: Publish from Visual Studio (Recommended)

Set up a Publish Profile for one-click deployment after pulling changes.

#### Create Publish Profile (First Time Only)

1. **Right-click** the `Mathy.ELM.Api` project in Solution Explorer
2. Click **Publish...**
3. Click **Add a publish profile** (or **New**)
4. Select **Folder** → Click **Next**
5. Set the folder location:
   ```
   C:\inetpub\wwwroot\ecm-api
   ```
6. Click **Finish**
7. Click **Show all settings** (or the pencil icon) to configure:
   - **Configuration:** Release
   - **Target Framework:** net9.0
   - **Deployment Mode:** Framework-dependent
   - **Target Runtime:** win-x64 (or Portable)
8. Click **Save**
9. Click **Publish** to deploy

#### One-Click Deploy (After Setup)

After pulling new changes:

1. Right-click `Mathy.ELM.Api` → **Publish...**
2. Click **Publish** button

That's it - the API is deployed to IIS.

#### If Files Are Locked During Publish

If you get file-locked errors, stop IIS before publishing:

```powershell
# Stop IIS
iisreset /stop

# Publish in Visual Studio...

# Start IIS
iisreset /start
```

### Verify Published Files

```powershell
Get-ChildItem "C:\inetpub\wwwroot\ecm-api" | Select-Object Name
```

Expected files include:
- `Mathy.ELM.Api.dll`
- `Mathy.ELM.Api.exe`
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `web.config`
- Various dependency DLLs

> **⚠️ Important:** When `ASPNETCORE_ENVIRONMENT=Development` (set in web.config), the app reads `appsettings.Development.json` which **OVERRIDES** values in `appsettings.json`. After publishing, delete `appsettings.Development.json` to ensure your server configuration is used, or update it with the correct server settings.

---

## SSL Certificate Setup

### Option 1: Create Self-Signed Certificate (Development/Testing)

Run in PowerShell as Administrator:

```powershell
# Create self-signed certificate for multiple domains
$cert = New-SelfSignedCertificate `
    -DnsName "ecmtest-api.corpmts.com", "ecmtest.corpmts.com", "localhost" `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(5) `
    -FriendlyName "ECM Development Certificate"

# Display certificate thumbprint
$cert.Thumbprint
```

### Option 2: Use Existing Certificate

If you have a certificate from a Certificate Authority:
1. Import the `.pfx` file to **Local Machine > Personal** store
2. Note the certificate thumbprint for IIS binding

### Verify Certificate Installation

```powershell
Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*ecmtest*" }
```

---

## IIS Configuration

### Create Application Pool

1. Open **IIS Manager** (run `inetmgr`)
2. In the left panel, expand your server name
3. Click on **Application Pools**
4. Right-click and select **Add Application Pool**
5. Configure:
   - **Name:** `ECM-API-Pool`
   - **.NET CLR Version:** `No Managed Code`
   - **Managed Pipeline Mode:** `Integrated`
6. Click **OK**

### Configure Application Pool Advanced Settings

1. Right-click `ECM-API-Pool` > **Advanced Settings**
2. Set these values:
   - **Start Mode:** `AlwaysRunning` (optional, for faster cold starts)
   - **Identity:** `ApplicationPoolIdentity` or a specific service account

### Create IIS Site

1. In IIS Manager, right-click **Sites** > **Add Website**
2. Configure:
   - **Site name:** `ECM-API`
   - **Application pool:** `ECM-API-Pool`
   - **Physical path:** `C:\inetpub\wwwroot\ecm-api`
   - **Binding Type:** `https`
   - **IP Address:** `All Unassigned`
   - **Port:** `443`
   - **Host name:** `ecmtest-api.corpmts.com`
   - **SSL Certificate:** Select your certificate
3. Click **OK**

### Alternative: Create Site via PowerShell

```powershell
Import-Module WebAdministration

# Create Application Pool
New-WebAppPool -Name "ECM-API-Pool"
Set-ItemProperty "IIS:\AppPools\ECM-API-Pool" -Name "managedRuntimeVersion" -Value ""

# Create Website
New-Website -Name "ECM-API" `
    -PhysicalPath "C:\inetpub\wwwroot\ecm-api" `
    -ApplicationPool "ECM-API-Pool" `
    -Port 443 `
    -HostHeader "ecmtest-api.corpmts.com" `
    -Ssl

# Get certificate thumbprint
$thumbprint = (Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -like "*ecmtest*" }).Thumbprint

# Bind SSL certificate
New-WebBinding -Name "ECM-API" -Protocol "https" -Port 443 -HostHeader "ecmtest-api.corpmts.com"
$binding = Get-WebBinding -Name "ECM-API" -Protocol "https"
$binding.AddSslCertificate($thumbprint, "My")
```

---

## Application Configuration

### Configure web.config

The `web.config` file controls how IIS hosts the application. Update it with proper settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Mathy.ELM.Api.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**Environment Options:**
- `Development` - Enables Swagger UI, detailed errors
- `Staging` - Swagger disabled, limited error details
- `Production` - Swagger disabled, minimal error details

### Configure appsettings.json

Update the `appsettings.json` file in the deployment folder with your environment-specific settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=YOUR_SQL_SERVER;uid=YOUR_USER;pwd=YOUR_PASSWORD;database=MathyECM;TrustServerCertificate=true;Encrypt=false;"
  },
  "AzureAd": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "Audience": "api://YOUR_CLIENT_ID/access_as_user"
  },
  "AzureServiceBus": {
    "ConnectionString": "YOUR_SERVICE_BUS_CONNECTION_STRING",
    "QueueName": "email-notifications",
    "MaxConcurrentCalls": 5,
    "MaxRetryAttempts": 3
  },
  "AzureEmail": {
    "ConnectionString": "YOUR_AZURE_COMMUNICATION_SERVICE_CONNECTION_STRING",
    "SenderAddress": "DoNotReply@your-domain.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Apply web.config via PowerShell

```powershell
$webConfigContent = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\Mathy.ELM.Api.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
'@

Set-Content -Path "C:\inetpub\wwwroot\ecm-api\web.config" -Value $webConfigContent
```

---

## Verification and Testing

### Restart IIS

```powershell
iisreset
```

### Test the API

1. **Test Swagger UI:**
   Open browser and navigate to:
   ```
   https://ecmtest-api.corpmts.com/swagger
   ```

   **Note:** If you see a certificate warning, click **Advanced** → **Proceed** to accept the self-signed certificate.

2. **Test Health Endpoint (if available):**
   ```
   https://ecmtest-api.corpmts.com/health
   ```

3. **Test API Endpoint:**
   ```
   https://ecmtest-api.corpmts.com/api/v1/referencedata/companies
   ```

### Verify Database Connection

On first run, Entity Framework Core will:
1. Create the database if it doesn't exist
2. Apply all pending migrations
3. Create Hangfire tables for background jobs

Check the logs if there are issues:
```powershell
Get-Content "C:\inetpub\wwwroot\ecm-api\logs\stdout*.log" -Tail 100
```

---

## Troubleshooting

### HTTP Error 500.30 - ASP.NET Core app failed to start

**Check stdout logs:**
```powershell
Get-ChildItem "C:\inetpub\wwwroot\ecm-api\logs" | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content
```

**Run the app directly to see detailed errors:**
```powershell
cd "C:\inetpub\wwwroot\ecm-api"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet Mathy.ELM.Api.dll
```

**Common causes:**
- Missing .NET Core Hosting Bundle
- Database connection string invalid
- Missing configuration values in appsettings.json
- Folder permission issues

### HTTP Error 404 - Page Not Found

**Check IIS binding:**
- Ensure the hostname in the URL matches the IIS binding
- Verify the port number is correct

**Check if site is running:**
```powershell
Get-Website | Where-Object { $_.Name -eq "ECM-API" }
```

### HTTP Error 400 - Hostname Invalid

This occurs when accessing via `localhost` but IIS binding only accepts specific hostname.

**Solutions:**
- Use the configured hostname: `https://ecmtest-api.corpmts.com:7001`
- Add a binding for localhost in IIS

### Database Connection Failed

**Verify SQL Server connectivity:**
```powershell
Test-NetConnection -ComputerName YOUR_SQL_SERVER -Port 1433
```

**Check SQL Server login:**
- Ensure the SQL user exists and has proper permissions
- Verify the connection string is correct

**Check for config override (common issue):**
If `ASPNETCORE_ENVIRONMENT=Development`, the app uses `appsettings.Development.json` which overrides `appsettings.json`. Delete the Development file:
```powershell
Remove-Item "C:\inetpub\wwwroot\ecm-api\appsettings.Development.json" -ErrorAction SilentlyContinue
iisreset
```

### Swagger Not Loading

**Check environment setting:**
Swagger is only enabled in `Development` environment. Verify `web.config`:
```powershell
Get-Content "C:\inetpub\wwwroot\ecm-api\web.config" | Select-String "ASPNETCORE_ENVIRONMENT"
```

### Hangfire Tables Missing

If you see "Invalid object name 'HangFire.Server'":
1. Ensure database connection succeeds on startup
2. Restart the application after database is created
3. Hangfire auto-creates tables on first successful connection

### View Application Event Logs

```powershell
Get-EventLog -LogName Application -Source "IIS*" -Newest 20 | Format-List
```

---

## Updating the Test Server

After pushing changes from your local development machine, follow these steps on the test server to deploy the updates.

### Preserving Server Configuration (Important!)

Publishing will overwrite `appsettings.json` and `web.config`. To prevent losing server-specific settings:

#### First-Time Setup: Backup Config Files

```powershell
# Create config backup folder
New-Item -Path "C:\inetpub\config\ecm-api" -ItemType Directory -Force

# Backup your working configs
Copy-Item "C:\inetpub\wwwroot\ecm-api\appsettings.json" "C:\inetpub\config\ecm-api\"
Copy-Item "C:\inetpub\wwwroot\ecm-api\web.config" "C:\inetpub\config\ecm-api\"
```

#### After Each Publish: Restore Config Files

```powershell
Copy-Item "C:\inetpub\config\ecm-api\appsettings.json" "C:\inetpub\wwwroot\ecm-api\" -Force
Copy-Item "C:\inetpub\config\ecm-api\web.config" "C:\inetpub\wwwroot\ecm-api\" -Force
# Delete Development override to use server config
Remove-Item "C:\inetpub\wwwroot\ecm-api\appsettings.Development.json" -ErrorAction SilentlyContinue
iisreset
```

### Using Visual Studio (Recommended)

1. **Open** the solution in Visual Studio
2. **Pull** latest changes:
   - Go to **Git** menu → **Pull**
   - Or in **Git Changes** window, click the down arrow (↓)
3. **Publish** the API:
   - Right-click `Mathy.ELM.Api` → **Publish...**
   - Click **Publish** button
4. **Restore config files and delete Development override:**
   ```powershell
   Copy-Item "C:\inetpub\config\ecm-api\appsettings.json" "C:\inetpub\wwwroot\ecm-api\" -Force
   Copy-Item "C:\inetpub\config\ecm-api\web.config" "C:\inetpub\wwwroot\ecm-api\" -Force
   Remove-Item "C:\inetpub\wwwroot\ecm-api\appsettings.Development.json" -ErrorAction SilentlyContinue
   ```
5. **Restart IIS:**
   ```powershell
   iisreset
   ```

### Using Command Line

```powershell
# Navigate to project folder
cd "C:\repo\Mathy\mathy_elm_app\server"

# Pull latest changes
git pull

# Publish to IIS folder
dotnet publish src\Mathy.ELM.Api -c Release -o "C:\inetpub\wwwroot\ecm-api"

# Restore server config files and delete Development override
Copy-Item "C:\inetpub\config\ecm-api\appsettings.json" "C:\inetpub\wwwroot\ecm-api\" -Force
Copy-Item "C:\inetpub\config\ecm-api\web.config" "C:\inetpub\wwwroot\ecm-api\" -Force
Remove-Item "C:\inetpub\wwwroot\ecm-api\appsettings.Development.json" -ErrorAction SilentlyContinue

# Restart IIS
iisreset
```

### Quick One-Liner

```powershell
cd "C:\repo\Mathy\mathy_elm_app\server" && git pull && dotnet publish src\Mathy.ELM.Api -c Release -o "C:\inetpub\wwwroot\ecm-api" && Copy-Item "C:\inetpub\config\ecm-api\*" "C:\inetpub\wwwroot\ecm-api\" -Force && Remove-Item "C:\inetpub\wwwroot\ecm-api\appsettings.Development.json" -ErrorAction SilentlyContinue && iisreset
```

---

## Quick Reference Commands

```powershell
# Restart IIS
iisreset

# Restart specific App Pool
Restart-WebAppPool -Name "ECM-API-Pool"

# Stop/Start Website
Stop-Website -Name "ECM-API"
Start-Website -Name "ECM-API"

# View recent logs
Get-Content "C:\inetpub\wwwroot\ecm-api\logs\stdout*.log" -Tail 50

# Check site status
Get-Website | Format-Table Name, State, PhysicalPath

# Check app pool status
Get-WebAppPoolState -Name "ECM-API-Pool"

# Republish (from project folder)
dotnet publish -c Release -o "C:\inetpub\wwwroot\ecm-api"
```

---

## Deployment Checklist

- [ ] .NET Core Hosting Bundle installed
- [ ] IIS installed with management tools
- [ ] URL Rewrite Module installed (for frontend)
- [ ] Deployment folder created with proper permissions
- [ ] SSL certificate installed
- [ ] Application Pool created (No Managed Code)
- [ ] IIS Site created with HTTPS binding
- [ ] API published to deployment folder
- [ ] appsettings.json configured with correct connection strings
- [ ] appsettings.Development.json deleted (to prevent config override)
- [ ] web.config configured with correct environment
- [ ] Hosts file updated (if using local DNS)
- [ ] Database connection verified
- [ ] Swagger UI accessible
- [ ] API endpoints responding correctly
