# SSL Certificate Setup for Local Development

This project requires HTTPS for Azure authentication. Follow these steps to set up trusted SSL certificates on your local machine.

## One-Time Setup (Per Developer)

### Step 1: Install mkcert

**On WSL/Linux:**
```bash
cd ~/
curl -JLO "https://dl.filippo.io/mkcert/latest?for=linux/amd64"
chmod +x mkcert-v*-linux-amd64
mkdir -p ~/bin
mv mkcert-v*-linux-amd64 ~/bin/mkcert
```

**On Windows (PowerShell as Admin):**
```powershell
choco install mkcert
```

**On macOS:**
```bash
brew install mkcert
```

### Step 2: Generate Certificates

Navigate to the client folder and run:

```bash
cd /mnt/c/Users/[YourUsername]/source/repos/mathy_elm_app/client
~/bin/mkcert localhost 127.0.0.1 ::1
```

This will create:
- `localhost+2.pem` (certificate)
- `localhost+2-key.pem` (private key)

**Note:** These files are already in `.gitignore` and should NOT be committed.

### Step 3: Install Root CA Certificate in Windows

**Find your CA certificate location:**
```bash
~/bin/mkcert -CAROOT
```

**Copy to Windows:**
```bash
cp ~/.local/share/mkcert/rootCA.pem /mnt/c/Users/[YourUsername]/
```

**Install in Windows:**
1. Navigate to `C:\Users\[YourUsername]\`
2. Double-click `rootCA.pem`
3. Click "Install Certificate..."
4. Select "Current User" → Next
5. Choose "Place all certificates in the following store"
6. Click "Browse..." → Select "Trusted Root Certification Authorities"
7. Click OK → Next → Finish
8. Confirm the security warning with "Yes"

### Step 4: Restart Browser

Close all browser windows and restart your browser.

### Step 5: Run the Application

```bash
npm start
```

Visit https://localhost:4200 - you should see a green padlock with no certificate warnings.

## Troubleshooting

**Still getting certificate errors?**
- Make sure you installed the CA in "Trusted Root Certification Authorities" (not "Personal")
- Restart your browser completely (close all windows)
- Clear browser cache
- Check that `localhost+2.pem` and `localhost+2-key.pem` exist in the client folder

**Certificate files missing?**
- Run `mkcert localhost 127.0.0.1 ::1` again in the client folder
- The `angular.json` is already configured to use these files

## Note for Test/Production

This setup is **only for local development**. Test and production servers will use proper SSL certificates and won't need mkcert.
