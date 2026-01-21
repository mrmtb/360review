# Plesk Deployment Quick Reference

Fast reference for common Plesk deployment operations.

## Quick Start

### Initial Setup
```powershell
# 1. Create domain in Plesk interface first
# 2. Run setup script
.\Setup-Plesk.ps1 -Domain "reviews.yourdomain.com"

# 3. Set environment variables in Plesk
# Go to: Websites & Domains > Domain > Hosting Settings > Environment Variables
```

### First Deployment
```powershell
# Get build SHA from GitHub Actions
$buildSHA = "abc123def456"

# Deploy
cd C:\Plesk\Scripts
.\Deploy-Production-Plesk.ps1 -BuildNumber $buildSHA -Domain "reviews.yourdomain.com"
```

## Common Commands

### Deployment

```powershell
# Standard deployment
.\Deploy-Production-Plesk.ps1 -BuildNumber "abc123" -Domain "reviews.yourdomain.com"

# Skip migrations
.\Deploy-Production-Plesk.ps1 -BuildNumber "abc123" -Domain "reviews.yourdomain.com" -SkipMigrations

# Dry run (test without changes)
.\Deploy-Production-Plesk.ps1 -BuildNumber "abc123" -Domain "reviews.yourdomain.com" -DryRun
```

### Restart Application

```powershell
# Via Plesk command
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name="reviews.yourdomain.com"

# Via IIS (if accessible)
Import-Module WebAdministration
Restart-WebAppPool -Name "reviews.yourdomain.com"
```

### View Logs

```powershell
# Application stdout logs
$domain = "reviews.yourdomain.com"
Get-Content "C:\Inetpub\vhosts\$domain\httpdocs\logs\stdout-*.log" -Tail 50

# Latest deployment log
Get-ChildItem "C:\Plesk\Backups\360review" -Filter "deployment-*.log" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1 | 
    Get-Content -Tail 50

# IIS logs
Get-ChildItem "C:\Inetpub\vhosts\$domain\logs" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1 | 
    Get-Content -Tail 20
```

### Health Checks

```powershell
$domain = "reviews.yourdomain.com"

# Test site
Invoke-WebRequest -Uri "https://$domain" -UseBasicParsing

# Test health endpoint
Invoke-WebRequest -Uri "https://$domain/health" -UseBasicParsing

# Check app pool
Import-Module WebAdministration
Get-WebAppPoolState -Name $domain

# Check domain info
& "${env:plesk_bin}\domain.exe" --info $domain
```

## Rollback

```powershell
$domain = "reviews.yourdomain.com"
$httpdocs = "C:\Inetpub\vhosts\$domain\httpdocs"

# Stop app
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name=$domain

# Get latest backup
$backup = Get-ChildItem "C:\Plesk\Backups\360review" -Directory | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1

# Restore
Remove-Item "$httpdocs\*" -Recurse -Force
Copy-Item "$($backup.FullName)\*" -Destination $httpdocs -Recurse

# Restart
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name=$domain
```

## File Locations

| Item | Path |
|------|------|
| **Website Root** | `C:\Inetpub\vhosts\{domain}\httpdocs` |
| **Application Logs** | `C:\Inetpub\vhosts\{domain}\httpdocs\logs` |
| **IIS Logs** | `C:\Inetpub\vhosts\{domain}\logs` |
| **Deployment Scripts** | `C:\Plesk\Scripts` |
| **Backups** | `C:\Plesk\Backups\360review` |
| **DB Backups** | `C:\Plesk\Backups\DB_Backups` |
| **Deployment Logs** | `C:\Plesk\Backups\360review\deployment-*.log` |
| **Plesk Binaries** | `%plesk_bin%` (usually `C:\Program Files (x86)\Plesk\bin`) |

## Plesk Commands

### Domain Management
```powershell
# Get domain info
& "${env:plesk_bin}\domain.exe" --info "reviews.yourdomain.com"

# List all domains
& "${env:plesk_bin}\domain.exe" --list

# Reconfigure domain
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name="reviews.yourdomain.com"
```

### SSL Management
```powershell
# List certificates
& "${env:plesk_bin}\certificate.exe" --list

# Get certificate info
& "${env:plesk_bin}\certificate.exe" --info "reviews.yourdomain.com"
```

### Database Management (via Plesk)
```powershell
# List databases
& "${env:plesk_bin}\database.exe" --list

# Create database
& "${env:plesk_bin}\database.exe" --create "360review" -domain "reviews.yourdomain.com"

# Create user
& "${env:plesk_bin}\database.exe" --create-user "appuser" -passwd "password" -domain "reviews.yourdomain.com"
```

## Environment Variables

### Required Variables (Set in Plesk)

| Variable | Purpose | Example |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `DB_HOST` | Database server | `db.yourdomain.com` |
| `DB_NAME` | Database name | `360review` |
| `DB_USER` | Database user | `appuser` |
| `DB_PASSWORD` | Database password | `SecureYOUR_STRONG_PASSWORD` |
| `GH_PAT` | GitHub token (for deployments) | `ghp_xxx...` |

### Set via PowerShell
```powershell
# Set machine-level variable
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")

# Set for user
[Environment]::SetEnvironmentVariable("DB_HOST", "db.server.com", "User")

# Reconfigure vhost to apply
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name="reviews.yourdomain.com"
```

## Troubleshooting

### App Won't Start

```powershell
# Check .NET hosting bundle
Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET" | 
    Where-Object { $_.GetValue("DisplayName") -like "*ASP.NET Core*10*" }

# Check web.config
Get-Content "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs\web.config"

# Check DLL exists
Test-Path "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs\360review.dll"

# Check logs
Get-Content "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs\logs\stdout-*.log" -Tail 100
```

### Permission Issues

```powershell
$domain = "reviews.yourdomain.com"
$path = "C:\Inetpub\vhosts\$domain\httpdocs"

# Reset permissions
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $path $acl

# Reconfigure
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name=$domain
```

### Database Connection

```powershell
# Test connection
$env:PGPASSWORD = "password"
psql -h db.server.com -U appuser -d 360review -c "SELECT version();"

# Check connection string
Get-Content "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs\appsettings.json" | 
    Select-String "ConnectionString"
```

### Artifact Download Fails

```powershell
# Verify token
$env:GH_PAT

# Test GitHub API
$headers = @{"Authorization" = "Bearer $env:GH_PAT"}
Invoke-RestMethod -Uri "https://api.github.com/user" -Headers $headers
```

## Backup & Restore

### Manual Backup
```powershell
$domain = "reviews.yourdomain.com"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "C:\Plesk\Backups\manual-$timestamp"

# Backup files
Copy-Item "C:\Inetpub\vhosts\$domain\httpdocs" -Destination $backupPath -Recurse

# Backup database
$env:PGPASSWORD = "password"
pg_dump -h localhost -U appuser -d 360review -F c -f "$backupPath\database.backup"
```

### Manual Restore
```powershell
$domain = "reviews.yourdomain.com"
$backupPath = "C:\Plesk\Backups\manual-20260121_143000"

# Stop app
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name=$domain

# Restore files
Remove-Item "C:\Inetpub\vhosts\$domain\httpdocs\*" -Recurse -Force
Copy-Item "$backupPath\*" -Destination "C:\Inetpub\vhosts\$domain\httpdocs" -Recurse

# Restore database
$env:PGPASSWORD = "password"
pg_restore -h localhost -U appuser -d 360review -c "$backupPath\database.backup"

# Start app
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name=$domain
```

## Monitoring

### Create Health Check Script
```powershell
$scriptContent = @'
$domain = "reviews.yourdomain.com"
$logFile = "C:\Plesk\Monitoring\health-check.log"

try {
    $response = Invoke-WebRequest -Uri "https://$domain/health" -UseBasicParsing -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Add-Content $logFile "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - OK"
    }
} catch {
    Add-Content $logFile "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - FAILED: $_"
}
'@

New-Item -ItemType Directory -Path "C:\Plesk\Monitoring" -Force
Set-Content "C:\Plesk\Scripts\Monitor-Application.ps1" $scriptContent
```

### Schedule Health Check
```powershell
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
    -Argument '-File C:\Plesk\Scripts\Monitor-Application.ps1'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) `
    -RepetitionInterval (New-TimeSpan -Minutes 5)

Register-ScheduledTask -Action $action -Trigger $trigger `
    -TaskName "360Review-HealthCheck" -User "SYSTEM"
```

### View Monitoring Logs
```powershell
Get-Content "C:\Plesk\Monitoring\health-check.log" -Tail 50
```

## Security

### SSL Certificate Setup (via Plesk UI)
1. Go to **Websites & Domains** → Domain → **SSL/TLS Certificates**
2. Click **Install** under Let's Encrypt
3. Enter email and select domains
4. Click **Get it free**
5. Enable **Permanent SEO-safe 301 redirect from HTTP to HTTPS**

### Update Environment Variables Securely
```powershell
# Never store passwords in scripts!
# Use Plesk UI: Websites & Domains > Domain > Hosting Settings

# Or secure prompt:
$securePassword = Read-Host "Enter DB Password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

[Environment]::SetEnvironmentVariable("DB_PASSWORD", $password, "Machine")

# Clean up
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
Remove-Variable password
```

## Performance Tuning

### App Pool Settings
```powershell
Import-Module WebAdministration
$appPool = "reviews.yourdomain.com"

# Set to always running
Set-ItemProperty "IIS:\AppPools\$appPool" -Name "startMode" -Value "AlwaysRunning"

# Disable idle timeout
Set-ItemProperty "IIS:\AppPools\$appPool" -Name "processModel.idleTimeout" -Value "00:00:00"

# Set recycling
Set-ItemProperty "IIS:\AppPools\$appPool" -Name "recycling.periodicRestart.time" -Value "00:00:00"
Set-ItemProperty "IIS:\AppPools\$appPool" -Name "recycling.periodicRestart.schedule" -Value @{value="03:00:00"}
```

## Support Links

- **Plesk Docs**: https://docs.plesk.com/
- **GitHub Repo**: https://github.com/mrmtb/360review
- **GitHub Actions**: https://github.com/mrmtb/360review/actions

---

**Quick Links**:
- [Full Deployment Guide](PLESK-DEPLOYMENT-GUIDE.md)
- [Troubleshooting](PLESK-DEPLOYMENT-GUIDE.md#troubleshooting)
- [Setup Checklist](PLESK-DEPLOYMENT-GUIDE.md#installation-steps)
