# Prerequisites - Mathy ELM System

This document outlines the prerequisites and installation steps needed to develop the Mathy Employee Change Management (ELM) System.

## Required Software

### 1. .NET 9 SDK
**Required for**: Backend API development, Entity Framework, and solution management

#### Windows Installation:
1. Download .NET 9 SDK from: https://dotnet.microsoft.com/en-us/download/dotnet/9.0
2. Run the installer and follow the setup wizard
3. Verify installation:
   ```bash
   dotnet --version
   ```
   Should show: `9.0.x`

#### WSL/Linux Installation:
```bash
# Download Microsoft package signing key
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package index
sudo apt-get update

# Install .NET SDK 9
sudo apt-get install -y dotnet-sdk-9.0

# Verify installation
dotnet --version
```

### 2. Node.js (LTS Version)
**Required for**: Angular development, npm package management

#### Installation:
1. Download Node.js LTS from: https://nodejs.org/
2. Install using the default settings
3. Verify installation:
   ```bash
   node --version
   npm --version
   ```
   Should show Node.js v20.x or v22.x and npm v10.x

#### Alternative - Using Node Version Manager (nvm):
```bash
# Install nvm (Linux/WSL/macOS)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Restart terminal or run:
source ~/.bashrc

# Install and use Node.js LTS
nvm install --lts
nvm use --lts
```

### 3. Angular CLI
**Required for**: Angular project scaffolding and development

#### Installation:
```bash
npm install -g @angular/cli@19

# Verify installation
ng version
```
Should show Angular CLI version 19.x

### 4. PrimeNG and Dependencies
**Required for**: Enterprise Angular UI components

#### Core PrimeNG Installation:
```bash
# Install PrimeNG and dependencies
npm install primeng
npm install primeicons
npm install @angular/animations
npm install @angular/cdk

# Verify PrimeNG installation
npm list primeng
```

#### Additional PrimeNG Features (Optional):
```bash
# For advanced chart components
npm install chart.js

# For calendar localization
npm install @angular/common

# For development/testing
npm install --save-dev @types/chart.js
```

### 5. SQL Server (Development)
**Required for**: Database development and testing

#### Options:

**Option A: SQL Server Express LocalDB (Recommended for Development)**
- Included with Visual Studio
- Or download separately: https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

**Option B: SQL Server Developer Edition**
- Free for development: https://www.microsoft.com/en-us/sql-server/sql-server-downloads

**Option C: SQL Server in Docker (Cross-platform)**
```bash
# Pull SQL Server 2022 image
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Run SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name mathy-elm-db \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

### 6. Git (Version Control)
**Required for**: Source control management

#### Installation:
- **Windows**: Download from https://git-scm.com/
- **Linux/WSL**: `sudo apt-get install git`
- **macOS**: `brew install git` or download from https://git-scm.com/

### 7. Code Editor/IDE
**Recommended for**: Development environment

#### Recommended Options:
- **Visual Studio 2022** (Windows) - Full IDE with excellent .NET support
- **Visual Studio Code** (Cross-platform) - Lightweight with extensions
- **JetBrains Rider** (Cross-platform) - Premium IDE

#### Visual Studio Code Extensions:
```bash
# Install recommended extensions
code --install-extension ms-dotnettools.csharp
code --install-extension angular.ng-template
code --install-extension ms-vscode.vscode-typescript-next
code --install-extension ms-mssql.mssql
code --install-extension formulahendry.auto-rename-tag
code --install-extension bradlc.vscode-tailwindcss
```

## Verification Steps

After installing all prerequisites, verify everything is working:

### 1. Verify .NET Installation
```bash
dotnet --info
```
Should show:
- .NET SDK version 9.0.x
- Runtime versions installed

### 2. Verify Node.js and Angular CLI
```bash
node --version
npm --version
ng version
```

### 3. Verify PrimeNG Installation (After Project Creation)
```bash
# After installing PrimeNG in your project
npm list primeng primeicons @angular/animations @angular/cdk

# Test PrimeNG component import (in Angular component)
# import { ButtonModule } from 'primeng/button';
```

### 4. Create Test Projects
```bash
# Test .NET project creation
mkdir test-dotnet && cd test-dotnet
dotnet new webapi
dotnet build
cd .. && rm -rf test-dotnet

# Test Angular project creation
ng new test-angular --routing --style=scss --skip-install
cd test-angular && npm install
ng build
cd .. && rm -rf test-angular
```

### 5. Test Database Connection
If using SQL Server in Docker:
```bash
# Test connection using sqlcmd (install via npm if not available)
npm install -g sql-cli
mssql -s localhost -u sa -p YourStrong@Passw0rd
```

## Development Environment Setup

### 1. Configure Git (if not done)
```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@company.com"
```

### 2. Set Environment Variables

#### Server (.NET API) Environment Variables
Create a `.env` file in the `/server` directory (will be added to .gitignore):
```
# Database Connection
DATABASE_CONNECTION_STRING="Server=localhost;Database=MathyELM;Integrated Security=true;TrustServerCertificate=true"

# API Settings
API_BASE_URL="https://localhost:7001"
CORS_ORIGINS="https://localhost:4200"

# External Integrations (to be configured later)
VIEWPOINT_API_URL=""
VIEWPOINT_API_KEY=""
SMTP_SERVER=""
SMTP_PORT=""

# Authentication
JWT_SECRET_KEY=""
JWT_ISSUER="MathyELM"
JWT_AUDIENCE="MathyELM"
```

#### Client (Angular) Environment Variables
Client environment variables are configured in `/client/.env.development` and managed via webpack:
```
# Already configured in client/.env.development
NG_APP_ENVIRONMENT=development
NG_APP_API_URL=https://localhost:7001/api/v1
NG_APP_MSAL_CLIENT_ID=your-azure-client-id
NG_APP_MSAL_TENANT_ID=your-azure-tenant-id
NG_APP_DEV_NAME=YourName
```

### 3. Recommended VSCode Workspace Settings
Create `.vscode/settings.json`:
```json
{
  "dotnet.defaultSolution": "Mathy.ELM.sln",
  "typescript.preferences.includePackageJsonAutoImports": "on",
  "angular.experimental-ivy": true,
  "files.exclude": {
    "**/node_modules": true,
    "**/bin": true,
    "**/obj": true
  }
}
```

## Troubleshooting

### Common Issues:

#### .NET SDK Not Found
- Ensure PATH includes .NET installation directory
- Restart terminal/IDE after installation
- On WSL, make sure you installed the Linux version, not Windows version

#### Angular CLI Installation Fails
```bash
# Clear npm cache
npm cache clean --force

# Install with verbose logging
npm install -g @angular/cli@19 --verbose
```

#### SQL Server Connection Issues
- Verify SQL Server service is running
- Check firewall settings for port 1433
- Ensure SQL Server Authentication is enabled (if using SQL auth)

#### Port Conflicts
- .NET API default: https://localhost:7001, http://localhost:5001
- Angular default: http://localhost:4200
- SQL Server default: localhost:1433

## Next Steps

Once all prerequisites are installed and verified:

1. ✅ Run the verification steps above
2. ✅ Proceed with project creation using `dotnet new` commands
3. ✅ Set up the Angular client application
4. ✅ Configure database connections and run initial migrations
5. ✅ Set up development workflows and build scripts

## Support

If you encounter issues during installation:
- Check the official documentation for each tool
- Verify system requirements and compatibility
- Consider using package managers (Chocolatey for Windows, Homebrew for macOS)
- Docker can provide isolated environments for development dependencies