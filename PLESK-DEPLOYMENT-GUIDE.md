# Plesk Deployment Guide for 360Review

Complete guide for deploying the 360Review .NET 10 application on Windows Server with Plesk.

## Overview

This guide covers deployment to a Windows Server running Plesk panel. Plesk manages IIS sites differently than standalone IIS, so we've created Plesk-specific scripts.

## Key Differences from Standard IIS Deployment

| Aspect | Standard IIS | Plesk IIS |
|--------|-------------|-----------|
| Site Path | `C:\inetpub\wwwroot\sitename` | `C:\Inetpub\vhosts\domain.com\httpdocs` |
| App Pool Management | Direct via IIS Manager | Through Plesk interface or `websrvmng.exe` |
| Configuration | Manual IIS configuration | Plesk vhost reconfiguration |
| Permissions | IIS_IUSRS | IIS_IUSRS + PLESK_IUSR |
| SSL Certificates | IIS Manager or PowerShell | Plesk SSL/TLS interface |

## Prerequisites

### Server Requirements
- Windows Server 2019 or later
- Plesk Obsidian 18.0.30 or later
- .NET 10.0 Hosting Bundle installed
- PostgreSQL client tools (if using PostgreSQL)
- PowerShell 5.1 or later

### Plesk Domain Setup
- Domain must be created in Plesk first
- Domain should have appropriate hosting plan
- DNS configured and pointing to server

### GitHub Configuration
- Repository secrets configured (see below)
- GitHub Actions workflow enabled

## Installation Steps

### Step 1: Create Domain in Plesk

1. **Login to Plesk**
   - Navigate to `https://your-server:8443`
   - Login with admin credentials

2. **Add Domain**
   - Go to **Websites & Domains**
   - Click **Add Domain**
   - Enter domain name: `reviews.yourdomain.com`
   - Select hosting type: **Website hosting**
   - Complete the wizard

3. **Verify Domain Creation**
   ```powershell
   # On server, verify domain exists
   cd C:\Inetpub\vhosts
   dir
   # Should see your domain folder
   ```

### Step 2: Install .NET 10.0 Hosting Bundle

```powershell
# Download Hosting Bundle
$downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/.../dotnet-hosting-10.0-win.exe"
$installerPath = "C:\temp\dotnet-hosting-10.0.exe"

# Download
Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath

# Install
Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait

# Restart IIS
iisreset

# Verify installation
Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET" | 
    Where-Object { $_.GetValue("DisplayName") -like "*ASP.NET Core*10*" }
```

### Step 3: Run Plesk Setup Script

```powershell
# Download or copy the setup script to server
cd C:\Plesk\Scripts

# Run setup
.\Setup-Plesk.ps1 -Domain "reviews.yourdomain.com"

# With SSL enabled
.\Setup-Plesk.ps1 -Domain "reviews.yourdomain.com" -EnableSSL
```

The script will:
- ✅ Verify domain exists in Plesk
- ✅ Create necessary directories
- ✅ Set proper permissions
- ✅ Configure application pool for .NET Core
- ✅ Create web.config
- ✅ Create sample appsettings.json

### Step 4: Configure Environment Variables in Plesk

**Option A: Via Plesk Interface**

1. Go to **Websites & Domains** → Your domain
2. Click **Hosting Settings** (or **Application Settings** depending on Plesk version)
3. Scroll to **Environment Variables** section
4. Add the following variables:

   | Variable | Value |
   |----------|-------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `DB_HOST` | `db.yourdomain.com` |
   | `DB_NAME` | `360review` |
   | `DB_USER` | `appuser` |
   | `DB_PASSWORD` | `your_secure_password` |
   | `GH_PAT` | `your_github_token` |

**Option B: Via PowerShell**

```powershell
# Set system environment variables
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("DB_HOST", "db.yourdomain.com", "Machine")
[Environment]::SetEnvironmentVariable("DB_NAME", "360review", "Machine")
[Environment]::SetEnvironmentVariable("DB_USER", "appuser", "Machine")
[Environment]::SetEnvironmentVariable("DB_PASSWORD", "secure_password", "Machine")
[Environment]::SetEnvironmentVariable("GH_PAT", "your_github_pat", "Machine")

# Restart IIS to apply environment variable changes
iisreset
```

### Step 5: Configure SSL Certificate

**Via Plesk Interface:**

1. Go to **Websites & Domains** → Your domain
2. Click **SSL/TLS Certificates**
3. Choose one of:
   - **Let's Encrypt** (free, automatic renewal)
   - **Purchase** (buy from certificate authority)
   - **Upload** (upload existing certificate)
4. Install the certificate
5. Enable **Permanent SEO-safe 301 redirect from HTTP to HTTPS**

**For Let's Encrypt:**
1. Click **Install** under Let's Encrypt
2. Enter email address
3. Select domain/subdomains
4. Click **Get it free**

### Step 6: Configure GitHub Secrets

In your GitHub repository (https://github.com/mrmtb/360review):

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Add the following secrets:

   | Secret Name | Description | Example Value |
   |-------------|-------------|---------------|
   | `DEPLOY_SERVER` | Server hostname | `wintermute.local` |
   | `DEPLOY_USER` | Windows admin user | `Administrator` |
   | `DEPLOY_PASSWORD` | User password | `YourPassword123` |
   | `DB_HOST` | Database server | `db.yourdomain.com` |
   | `DB_NAME` | Database name | `360review` |
   | `DB_USER` | Database user | `appuser` |
   | `DB_PASSWORD` | Database password | `SecurePassword!` |

### Step 7: Update Deployment Scripts on Server

```powershell
# Create scripts directory
New-Item -ItemType Directory -Path "C:\Plesk\Scripts" -Force

# Download Plesk-specific scripts
$scriptBase = "https://raw.githubusercontent.com/mrmtb/360review/main/Powershell"

$scripts = @(
    "Deploy-Production-Plesk.ps1",
    "Setup-Plesk.ps1",
    "Run-Migration.ps1"
)

cd C:\Plesk\Scripts

foreach ($script in $scripts) {
    $url = "$scriptBase/$script"
    Invoke-WebRequest -Uri $url -OutFile ".\$script"
    Write-Host "Downloaded: $script" -ForegroundColor Green
}
```

### Step 8: Configure Database

```powershell
# Connect to PostgreSQL
$env:PGPASSWORD = "admin_password"
psql -h localhost -U postgres

# In psql console:
CREATE DATABASE "360review";
CREATE USER appuser WITH ENCRYPTED PASSWORD 'secure_password';
GRANT ALL PRIVILEGES ON DATABASE "360review" TO appuser;
\q

# Test connection
$env:PGPASSWORD = "secure_password"
psql -h localhost -U appuser -d "360review" -c "SELECT version();"
```

### Step 9: Initial Deployment

```powershell
cd C:\Plesk\Scripts

# Get latest build SHA from GitHub Actions
# Visit: https://github.com/mrmtb/360review/actions

$buildSHA = "abc123def456..."  # Replace with actual SHA
$domain = "reviews.yourdomain.com"

# Test deployment (dry run)
.\Deploy-Production-Plesk.ps1 -BuildNumber $buildSHA -Domain $domain -DryRun

# Actual deployment
.\Deploy-Production-Plesk.ps1 -BuildNumber $buildSHA -Domain $domain
```

## Daily Operations

### Manual Deployment

```powershell
# Deploy specific build
.\Deploy-Production-Plesk.ps1 `
    -BuildNumber "abc123" `
    -Domain "reviews.yourdomain.com"

# Deploy without migrations
.\Deploy-Production-Plesk.ps1 `
    -BuildNumber "abc123" `
    -Domain "reviews.yourdomain.com" `
    -SkipMigrations

# Deploy without backup (not recommended for production)
.\Deploy-Production-Plesk.ps1 `
    -BuildNumber "abc123" `
    -Domain "reviews.yourdomain.com" `
    -SkipBackup
```

### Restart Application

```powershell
# Option 1: Restart app pool (recommended)
Import-Module WebAdministration
Restart-WebAppPool -Name "reviews.yourdomain.com"

# Option 2: IIS reset (restarts all sites)
iisreset
```

### View Logs

```powershell
# Application logs
$domain = "reviews.yourdomain.com"
$logsPath = "C:\Inetpub\vhosts\$domain\httpdocs\logs"
Get-Content "$logsPath\stdout-*.log" -Tail 50

# Deployment logs
Get-ChildItem "C:\Plesk\Backups\360review" | 
    Where-Object { $_.Name -like "deployment-*.log" } |
    Sort-Object CreationTime -Descending |
    Select-Object -First 1 |
    Get-Content -Tail 50

# IIS logs (through Plesk)
$iisLogs = "C:\Inetpub\vhosts\$domain\logs"
Get-ChildItem $iisLogs | Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 | Get-Content -Tail 20
```

### Rollback

```powershell
$domain = "reviews.yourdomain.com"
$httpdocs = "C:\Inetpub\vhosts\$domain\httpdocs"
$backupPath = "C:\Plesk\Backups\360review"

# Stop app pool
Import-Module WebAdministration
Stop-WebAppPool -Name $domain

# Get latest backup
$latestBackup = Get-ChildItem $backupPath |
    Where-Object { $_.PSIsContainer } |
    Sort-Object CreationTime -Descending |
    Select-Object -First 1

# Restore
Remove-Item "$httpdocs\*" -Recurse -Force
Copy-Item "$($latestBackup.FullName)\*" -Destination $httpdocs -Recurse

# Restart
Import-Module WebAdministration
Start-WebAppPool -Name $domain
```

### Health Checks

```powershell
$domain = "reviews.yourdomain.com"

# Check if site responds
Invoke-WebRequest -Uri "https://$domain" -UseBasicParsing

# Check health endpoint
Invoke-WebRequest -Uri "https://$domain/health" -UseBasicParsing

# Check app pool status
Import-Module WebAdministration
Get-WebAppPoolState -Name $domain

# Check Plesk site status
& "${env:plesk_bin}\domain.exe" --info $domain
```

## Plesk-Specific Considerations

### File Permissions

Plesk manages permissions differently:
- `IIS_IUSRS` - Standard IIS user
- `PLESK_IUSR` - Plesk system user (if exists)
- Domain-specific users may be created by Plesk

The setup script handles this automatically.

### Protected Directories

Plesk creates several directories that should not be deleted:
- `plesk-stat/` - Statistics
- `statistics/` - Domain statistics
- `error_docs/` - Error pages
- `_private/` - Private files

The deployment script excludes these automatically.

### Application Pool Management

Plesk manages app pools with specific naming:
- Usually named same as domain: `reviews.yourdomain.com`
- Configured through Plesk interface or `websrvmng.exe`
- May not be directly accessible via IIS Manager

### SSL Certificate Management

Always use Plesk's SSL management:
- Let's Encrypt integration
- Automatic renewal
- Proper binding to IIS

Don't manually configure SSL in IIS - use Plesk interface.

### Database Access

If using Plesk's database management:
1. Go to **Databases** in Plesk
2. Create database and user
3. Note the connection details
4. Use these in your appsettings.json

## Troubleshooting

### Application Won't Start

**Check:**
1. **.NET 10 Hosting Bundle installed?**
   ```powershell
   Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET"
   ```

2. **web.config correct?**
   ```powershell
   $domain = "reviews.yourdomain.com"
   Get-Content "C:\Inetpub\vhosts\$domain\httpdocs\web.config"
   ```

3. **Application DLL exists?**
   ```powershell
   Test-Path "C:\Inetpub\vhosts\$domain\httpdocs\360review.dll"
   ```

4. **Check application logs:**
   ```powershell
   Get-Content "C:\Inetpub\vhosts\$domain\httpdocs\logs\stdout-*.log" -Tail 100
   ```

### Permission Errors

```powershell
$domain = "reviews.yourdomain.com"
$httpdocs = "C:\Inetpub\vhosts\$domain\httpdocs"

# Reset permissions
$acl = Get-Acl $httpdocs
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $httpdocs $acl

# Restart app pool
Import-Module WebAdministration
Restart-WebAppPool -Name $domain
```

### Database Connection Fails

**Check connection string in appsettings.json:**
```powershell
$domain = "reviews.yourdomain.com"
Get-Content "C:\Inetpub\vhosts\$domain\httpdocs\appsettings.json" | 
    Select-String "ConnectionString"
```

**Test database connectivity:**
```powershell
$env:PGPASSWORD = "your_password"
psql -h db.server.com -U appuser -d 360review -c "SELECT 1;"
```

### Deployment Script Fails

**Check GitHub token:**
```powershell
# Verify token is set
$env:GH_PAT
# Should output your token (keep secure!)

# Test GitHub API access
$headers = @{"Authorization" = "Bearer $env:GH_PAT"}
Invoke-RestMethod -Uri "https://api.github.com/user" -Headers $headers
```

### SSL Certificate Issues

**Via Plesk:**
1. Go to **Websites & Domains** → Domain → **SSL/TLS Certificates**
2. Check certificate status
3. Reinstall if needed

**Check certificate binding:**
```powershell
Import-Module WebAdministration
Get-WebBinding -Name "reviews.yourdomain.com" -Protocol https
```

## Monitoring

### Plesk Built-in Monitoring

Use Plesk's monitoring tools:
- **Statistics** - Traffic and bandwidth
- **Logs** - Access and error logs
- **Resources** - CPU and memory usage

### Custom Monitoring

```powershell
# Create monitoring script
$scriptContent = @'
$domain = "reviews.yourdomain.com"
$logFile = "C:\Plesk\Monitoring\health-check.log"

try {
    $response = Invoke-WebRequest -Uri "https://$domain/health" -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Add-Content $logFile "$(Get-Date) - OK"
    }
} catch {
    Add-Content $logFile "$(Get-Date) - FAILED: $_"
    # Send alert email via Plesk
}
'@

Set-Content "C:\Plesk\Scripts\Monitor-Application.ps1" $scriptContent

# Schedule task in Windows
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
    -Argument '-File C:\Plesk\Scripts\Monitor-Application.ps1'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) `
    -RepetitionInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask -Action $action -Trigger $trigger `
    -TaskName "360Review-HealthCheck" -User "SYSTEM"
```

## Backup Strategy

### Automatic Backups (via Script)
- Deployment script creates backup before each deployment
- Keeps last 10 backups
- Location: `C:\Plesk\Backups\360review\`

### Plesk Backup Manager

Use Plesk's built-in backup:
1. Go to **Tools & Settings** → **Backup Manager**
2. Configure scheduled backups
3. Include website files and databases
4. Set retention policy

### Manual Backup

```powershell
$domain = "reviews.yourdomain.com"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Plesk\Backups\manual-$timestamp"

# Backup files
Copy-Item "C:\Inetpub\vhosts\$domain\httpdocs" `
    -Destination $backupPath -Recurse

# Backup database
$env:PGPASSWORD = "password"
pg_dump -h localhost -U appuser -d 360review `
    -F c -f "$backupPath\database.backup"
```

## Support and Resources

### Plesk Documentation
- [Plesk Documentation](https://docs.plesk.com/)
- [ASP.NET Core on Plesk](https://docs.plesk.com/en-US/current/administrator-guide/website-management/asp-net-support.77997/)

### Microsoft Documentation
- [ASP.NET Core Hosting](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [.NET 10 Downloads](https://dotnet.microsoft.com/download/dotnet/10.0)

### GitHub Resources
- [Repository](https://github.com/mrmtb/360review)
- [Actions Workflows](https://github.com/mrmtb/360review/actions)

---

**Version:** 1.0  
**Last Updated:** January 21, 2026  
**Maintained By:** DevOps Team
