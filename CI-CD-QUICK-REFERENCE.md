# CI/CD Quick Reference

## Common Commands

### Initial Setup

```powershell
# 1. Setup IIS (run as Administrator)
cd C:\Deployments\Scripts
.\Setup-IIS.ps1 -HostName "reviews.yourdomain.com" -CreateSelfSignedCert

# 2. Set environment variables
$env:GITHUB_TOKEN = "your_token"
$env:DB_HOST = "db.server.com"
$env:DB_NAME = "360review"
$env:DB_USER = "appuser"
$env:DB_PASSWORD = "password"

# 3. First deployment
.\Deploy-Production.ps1 -BuildNumber "latest_sha"
```

### Manual Deployment

```powershell
# Standard deployment
.\Deploy-Production.ps1 -BuildNumber "abc123"

# Dry run (test without changes)
.\Deploy-Production.ps1 -BuildNumber "abc123" -DryRun

# Skip database migrations
.\Deploy-Production.ps1 -BuildNumber "abc123" -SkipMigrations

# Skip backup
.\Deploy-Production.ps1 -BuildNumber "abc123" -SkipBackup
```

### Database Migrations

```powershell
# Run migrations manually
.\Run-Migration.ps1 `
    -ProjectPath "C:\inetpub\wwwroot\360review\360review.csproj" `
    -DbType "Postgres" `
    -DbHost "db.server.com" `
    -DbName "360review" `
    -DbUser "appuser" `
    -DbPassword "password"
```

### IIS Management

```powershell
# Import WebAdministration
Import-Module WebAdministration

# Stop application pool
Stop-WebAppPool -Name "360ReviewAppPool"

# Start application pool
Start-WebAppPool -Name "360ReviewAppPool"

# Restart application pool
Restart-WebAppPool -Name "360ReviewAppPool"

# Check application pool status
Get-WebAppPoolState -Name "360ReviewAppPool"

# Recycle application pool
Restart-WebAppPool -Name "360ReviewAppPool"

# Stop website
Stop-Website -Name "360Review"

# Start website
Start-Website -Name "360Review"
```

### Rollback

```powershell
# List available backups
Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending

# Rollback to specific backup
$backupDate = "20260120_143022"
Stop-WebAppPool -Name "360ReviewAppPool"
Remove-Item "C:\inetpub\wwwroot\360review\*" -Recurse -Force
Copy-Item "C:\Deployments\Backups\360review\$backupDate\*" `
    -Destination "C:\inetpub\wwwroot\360review" -Recurse
Start-WebAppPool -Name "360ReviewAppPool"
```

### Log Viewing

```powershell
# View latest deployment log
$latestLog = Get-ChildItem "C:\Deployments\Logs" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1
Get-Content $latestLog.FullName -Tail 50

# View application logs
Get-Content "C:\inetpub\wwwroot\360review\logs\stdout-*.log" -Tail 100

# View IIS logs
$iisLogs = "C:\inetpub\logs\LogFiles\W3SVC*"
Get-ChildItem $iisLogs | Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1 | Get-Content -Tail 50
```

### Health Checks

```powershell
# Check if site is responding
Invoke-WebRequest -Uri "https://reviews.yourdomain.com" -UseBasicParsing

# Check health endpoint
Invoke-WebRequest -Uri "https://reviews.yourdomain.com/health" -UseBasicParsing

# Check app pool status
Get-WebAppPoolState -Name "360ReviewAppPool"

# Check website status
Get-Website -Name "360Review"
```

### Troubleshooting

```powershell
# Clear ASP.NET Core cache
Remove-Item "C:\inetpub\wwwroot\360review\*.deps.json" -Force
Remove-Item "C:\inetpub\wwwroot\360review\*.runtimeconfig.json" -Force

# Reset application pool
Stop-WebAppPool -Name "360ReviewAppPool"
Start-Sleep -Seconds 10
Start-WebAppPool -Name "360ReviewAppPool"

# Full IIS reset (use with caution)
iisreset /stop
Start-Sleep -Seconds 5
iisreset /start

# Check .NET hosting bundle version
Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET" | 
    Where-Object { $_.GetValue("DisplayName") -like "*ASP.NET Core*" }
```

### Backup Management

```powershell
# Create manual backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
Copy-Item "C:\inetpub\wwwroot\360review" `
    -Destination "C:\Deployments\Backups\360review\manual-$timestamp" `
    -Recurse

# Clean old backups (keep last 10)
Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -Skip 10 | 
    Remove-Item -Recurse -Force

# Verify backup integrity
$latestBackup = Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1
Test-Path "$($latestBackup.FullName)\360review.dll"
```

## GitHub Actions

### Trigger Manual Deployment

1. Go to: `https://github.com/mrmtb/360review/actions`
2. Click "Build, Test and Deploy"
3. Click "Run workflow"
4. Select branch: `main` or `develop`
5. Choose environment: `staging` or `production`
6. Click "Run workflow"

### View Build Status

```bash
# Using GitHub CLI
gh run list --limit 10
gh run view <run_id>
gh run watch <run_id>
```

### Download Artifact

```bash
# Using GitHub CLI
gh run download <run_id> --name 360review-<sha>
```

## Quick Diagnostics

### Check Everything Status

```powershell
Write-Host "=== App Pool Status ===" -ForegroundColor Cyan
Get-WebAppPoolState -Name "360ReviewAppPool"

Write-Host "`n=== Website Status ===" -ForegroundColor Cyan
Get-Website -Name "360Review"

Write-Host "`n=== Disk Space ===" -ForegroundColor Cyan
Get-PSDrive C | Select-Object Used,Free

Write-Host "`n=== Latest Deployment ===" -ForegroundColor Cyan
Get-ChildItem "C:\Deployments\Logs" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1

Write-Host "`n=== Recent Errors ===" -ForegroundColor Cyan
Get-EventLog -LogName Application -EntryType Error -Newest 5 | 
    Where-Object { $_.Source -like "*IIS*" -or $_.Source -like "*ASP.NET*" }
```

## Emergency Procedures

### Site Down

```powershell
# 1. Check app pool
Get-WebAppPoolState -Name "360ReviewAppPool"

# 2. Restart app pool
Restart-WebAppPool -Name "360ReviewAppPool"

# 3. If still down, rollback
$latestBackup = Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1
Stop-WebAppPool -Name "360ReviewAppPool"
Remove-Item "C:\inetpub\wwwroot\360review\*" -Recurse -Force
Copy-Item "$($latestBackup.FullName)\*" `
    -Destination "C:\inetpub\wwwroot\360review" -Recurse
Start-WebAppPool -Name "360ReviewAppPool"

# 4. Verify
Start-Sleep -Seconds 10
Invoke-WebRequest -Uri "https://reviews.yourdomain.com/health"
```

### Database Connection Issues

```powershell
# 1. Test database connection
$env:PGPASSWORD = "your_password"
psql -h db.server.com -U appuser -d 360review -c "SELECT 1;"

# 2. Check connection string in appsettings
Get-Content "C:\inetpub\wwwroot\360review\appsettings.json" | 
    Select-String "ConnectionString"

# 3. Restart app pool
Restart-WebAppPool -Name "360ReviewAppPool"
```

### Certificate Issues

```powershell
# List certificates
Get-ChildItem Cert:\LocalMachine\My | Where-Object { 
    $_.Subject -like "*yourdomain*" 
}

# Check binding
Get-WebBinding -Name "360Review" -Protocol "https"

# Re-bind certificate
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { 
    $_.Subject -like "*yourdomain*" 
} | Select-Object -First 1

$binding = Get-WebBinding -Name "360Review" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

## Monitoring URLs

- **Production:** https://reviews.yourdomain.com
- **Staging:** https://staging.yourdomain.com
- **Health Check:** /health
- **GitHub Actions:** https://github.com/mrmtb/360review/actions

## Support Contacts

- **DevOps:** devops@yourdomain.com
- **On-Call:** [Rotation Schedule]
- **Database:** dba@yourdomain.com
