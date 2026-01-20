# Server Setup Checklist

Use this checklist when setting up a new Windows Server for the 360review application.

## Pre-Setup Requirements

- [ ] Windows Server 2019 or later installed
- [ ] Server has internet connectivity
- [ ] Remote Desktop access configured
- [ ] Administrator account credentials available
- [ ] Domain name configured and pointing to server IP
- [ ] SSL certificate obtained (or plan to use self-signed for testing)

## Phase 1: Windows Server Configuration

### 1.1 Initial Server Setup

- [ ] Set server hostname: `Set-ComputerName -Name "PROD-WEB-01"`
- [ ] Configure Windows Updates
  ```powershell
  Install-Module PSWindowsUpdate
  Get-WindowsUpdate
  Install-WindowsUpdate -AcceptAll -AutoReboot
  ```
- [ ] Configure Windows Firewall
  ```powershell
  # Allow HTTP
  New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
  
  # Allow HTTPS
  New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
  
  # Allow RDP (if not already)
  New-NetFirewallRule -DisplayName "RDP" -Direction Inbound -Protocol TCP -LocalPort 3389 -Action Allow
  ```
- [ ] Set timezone: `Set-TimeZone -Name "Eastern Standard Time"`
- [ ] Enable PowerShell remoting: `Enable-PSRemoting -Force`

### 1.2 Create Directory Structure

```powershell
# Run as Administrator
$directories = @(
    "C:\inetpub\wwwroot\360review",
    "C:\inetpub\wwwroot\360review\logs",
    "C:\Deployments\Scripts",
    "C:\Deployments\Backups\360review",
    "C:\Deployments\Logs",
    "C:\Deployments\DB_Backups",
    "C:\temp"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force
        Write-Host "Created: $dir" -ForegroundColor Green
    }
}
```

- [ ] Directories created successfully

## Phase 2: Install Required Software

### 2.1 Install IIS

```powershell
# Install IIS with management tools
Install-WindowsFeature -Name Web-Server -IncludeManagementTools

# Install additional required features
Install-WindowsFeature -Name Web-WebSockets
Install-WindowsFeature -Name Web-Request-Monitor
Install-WindowsFeature -Name Web-Http-Tracing
Install-WindowsFeature -Name Web-Stat-Compression
Install-WindowsFeature -Name Web-Dyn-Compression
```

- [ ] IIS installed
- [ ] Verify: `Get-WindowsFeature | Where-Object {$_.Name -eq "Web-Server"}`

### 2.2 Install .NET 10.0 Hosting Bundle

```powershell
# Download and install .NET 10.0 Hosting Bundle
$downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/.../dotnet-hosting-10.0-win.exe"
$installerPath = "C:\temp\dotnet-hosting-10.0.exe"

# Download
Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath

# Install (silent mode)
Start-Process -FilePath $installerPath -ArgumentList "/quiet", "/norestart" -Wait

# Restart IIS
iisreset
```

- [ ] .NET 10.0 Hosting Bundle installed
- [ ] IIS restarted
- [ ] Verify: Check "Programs and Features" for ASP.NET Core Runtime

### 2.3 Install PostgreSQL Client Tools (if using PostgreSQL)

```powershell
# Download PostgreSQL installer or just the client tools
# Visit: https://www.postgresql.org/download/windows/

# Or install via chocolatey
choco install postgresql --params '/Password:YourPassword' -y
```

- [ ] PostgreSQL client tools installed
- [ ] `psql` command available
- [ ] Test connection to database server

### 2.4 Install Additional Tools

```powershell
# Install Chocolatey (package manager)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install useful tools
choco install git -y
choco install notepadplusplus -y
choco install 7zip -y
```

- [ ] Chocolatey installed
- [ ] Git installed
- [ ] Text editor installed

## Phase 3: IIS Configuration

### 3.1 Run IIS Setup Script

```powershell
cd C:\Deployments\Scripts

# Download setup script from repository
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/mrmtb/360review/main/Powershell/Setup-IIS.ps1" `
    -OutFile ".\Setup-IIS.ps1"

# Run setup
.\Setup-IIS.ps1 `
    -SiteName "360Review" `
    -AppPoolName "360ReviewAppPool" `
    -SitePath "C:\inetpub\wwwroot\360review" `
    -HostName "reviews.yourdomain.com" `
    -HttpPort 80 `
    -HttpsPort 443 `
    -CreateSelfSignedCert
```

- [ ] IIS setup script executed successfully
- [ ] Application pool created
- [ ] Website created
- [ ] Certificate configured

### 3.2 Verify IIS Configuration

```powershell
# Check application pool
Get-WebAppPoolState -Name "360ReviewAppPool"

# Check website
Get-Website -Name "360Review"

# Check bindings
Get-WebBinding -Name "360Review"
```

- [ ] Application pool is running
- [ ] Website is started
- [ ] HTTP binding on port 80
- [ ] HTTPS binding on port 443

## Phase 4: Security Configuration

### 4.1 Configure Folder Permissions

```powershell
$sitePath = "C:\inetpub\wwwroot\360review"

# Grant IIS_IUSRS modify permissions
$acl = Get-Acl $sitePath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $sitePath $acl

# Verify
Get-Acl $sitePath | Format-List
```

- [ ] IIS_IUSRS has modify permissions
- [ ] Logs folder permissions verified

### 4.2 Configure SSL Certificate (Production Only)

```powershell
# If using a real certificate, import it
$certPassword = ConvertTo-SecureString -String "cert_password" -Force -AsPlainText
Import-PfxCertificate -FilePath "C:\temp\certificate.pfx" `
    -CertStoreLocation Cert:\LocalMachine\My `
    -Password $certPassword

# Bind certificate to website
$cert = Get-ChildItem Cert:\LocalMachine\My | 
    Where-Object { $_.Subject -like "*yourdomain.com*" } | 
    Select-Object -First 1

$binding = Get-WebBinding -Name "360Review" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

- [ ] SSL certificate imported
- [ ] Certificate bound to HTTPS binding
- [ ] Verify: `https://reviews.yourdomain.com` loads with valid cert

### 4.3 Configure Environment Variables

```powershell
# Set system environment variables
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "your_github_token", "Machine")
[Environment]::SetEnvironmentVariable("DB_HOST", "db.yourdomain.com", "Machine")
[Environment]::SetEnvironmentVariable("DB_NAME", "360review", "Machine")
[Environment]::SetEnvironmentVariable("DB_USER", "appuser", "Machine")
[Environment]::SetEnvironmentVariable("DB_PASSWORD", "secure_password", "Machine")

# Restart IIS to apply environment variables
iisreset
```

- [ ] ASPNETCORE_ENVIRONMENT set to Production
- [ ] GITHUB_TOKEN configured
- [ ] Database environment variables set
- [ ] IIS restarted

## Phase 5: Database Configuration

### 5.1 Create Database

```powershell
# Connect to PostgreSQL
$env:PGPASSWORD = "admin_password"
psql -h db.yourdomain.com -U postgres

# Run in psql:
# CREATE DATABASE "360review";
# CREATE USER appuser WITH ENCRYPTED PASSWORD 'secure_password';
# GRANT ALL PRIVILEGES ON DATABASE "360review" TO appuser;
# \q
```

- [ ] Database created
- [ ] Application user created
- [ ] Permissions granted

### 5.2 Test Database Connection

```powershell
# Test connection
$env:PGPASSWORD = "secure_password"
psql -h db.yourdomain.com -U appuser -d "360review" -c "SELECT version();"
```

- [ ] Connection successful
- [ ] User can query database

## Phase 6: Application Configuration

### 6.1 Create appsettings.json

```powershell
$appSettings = @"
{
  "ConnectionStrings": {
    "ApplicationDBConnection": "Host=db.yourdomain.com;Database=360review;Username=appuser;Password=secure_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@

Set-Content -Path "C:\inetpub\wwwroot\360review\appsettings.json" -Value $appSettings
```

- [ ] appsettings.json created
- [ ] Connection string configured
- [ ] Sensitive values secured

### 6.2 Configure web.config

```powershell
$webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\360review.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="DetailedLocalOnly" />
    </system.webServer>
  </location>
</configuration>
"@

Set-Content -Path "C:\inetpub\wwwroot\360review\web.config" -Value $webConfig
```

- [ ] web.config created
- [ ] ASP.NET Core module configured
- [ ] Logging enabled

## Phase 7: Deployment Scripts

### 7.1 Download Deployment Scripts

```powershell
cd C:\Deployments\Scripts

# Download all scripts
$scripts = @(
    "Deploy-Production.ps1",
    "Setup-IIS.ps1",
    "Run-Migration.ps1"
)

foreach ($script in $scripts) {
    $url = "https://raw.githubusercontent.com/mrmtb/360review/main/Powershell/$script"
    Invoke-WebRequest -Uri $url -OutFile ".\$script"
    Write-Host "Downloaded: $script" -ForegroundColor Green
}
```

- [ ] Deploy-Production.ps1 downloaded
- [ ] Setup-IIS.ps1 downloaded
- [ ] Run-Migration.ps1 downloaded

### 7.2 Test Deployment Script (Dry Run)

```powershell
cd C:\Deployments\Scripts

# Test deployment (dry run)
.\Deploy-Production.ps1 -BuildNumber "test" -DryRun
```

- [ ] Deployment script runs without errors
- [ ] All prerequisites verified

## Phase 8: Initial Deployment

### 8.1 Deploy Application

```powershell
# Get latest build SHA from GitHub
# Visit: https://github.com/mrmtb/360review/actions

$buildSHA = "abc123def456..."  # Replace with actual SHA

# Deploy
cd C:\Deployments\Scripts
.\Deploy-Production.ps1 -BuildNumber $buildSHA
```

- [ ] Application deployed successfully
- [ ] No errors in deployment log
- [ ] Application pool started

### 8.2 Run Database Migrations

```powershell
# Migrations should run automatically with deployment
# If manual run needed:
.\Run-Migration.ps1 `
    -ProjectPath "C:\inetpub\wwwroot\360review\360review.csproj" `
    -DbType "Postgres" `
    -DbHost "db.yourdomain.com" `
    -DbName "360review" `
    -DbUser "appuser" `
    -DbPassword "secure_password"
```

- [ ] Migrations completed successfully
- [ ] Database schema created
- [ ] Seed data inserted (if applicable)

## Phase 9: Testing and Verification

### 9.1 Application Health Checks

```powershell
# Test HTTP
Invoke-WebRequest -Uri "http://reviews.yourdomain.com" -UseBasicParsing

# Test HTTPS
Invoke-WebRequest -Uri "https://reviews.yourdomain.com" -UseBasicParsing

# Test health endpoint
Invoke-WebRequest -Uri "https://reviews.yourdomain.com/health" -UseBasicParsing
```

- [ ] HTTP responds (200 OK)
- [ ] HTTPS responds (200 OK)
- [ ] Health endpoint returns healthy status

### 9.2 Functional Testing

- [ ] Can access login page
- [ ] Can create account
- [ ] Can login
- [ ] Can access admin area (if admin)
- [ ] Database reads working
- [ ] Database writes working
- [ ] Email functionality working (if configured)

### 9.3 Check Logs

```powershell
# Check application logs
Get-Content "C:\inetpub\wwwroot\360review\logs\stdout-*.log" -Tail 50

# Check deployment logs
Get-ChildItem "C:\Deployments\Logs" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1 | 
    Get-Content -Tail 50

# Check IIS logs
Get-ChildItem "C:\inetpub\logs\LogFiles\W3SVC*" | 
    Sort-Object LastWriteTime -Descending | 
    Select-Object -First 1 | 
    Get-Content -Tail 20
```

- [ ] No critical errors in application logs
- [ ] No errors in deployment logs
- [ ] IIS logs show successful requests

### 9.4 Performance Testing

- [ ] Page load time acceptable (< 3 seconds)
- [ ] Database queries performing well
- [ ] Memory usage reasonable
- [ ] CPU usage reasonable

## Phase 10: Monitoring Setup

### 10.1 Configure Windows Event Log Monitoring

```powershell
# Create event log source for application
New-EventLog -LogName Application -Source "360Review"

# Test event logging
Write-EventLog -LogName Application -Source "360Review" `
    -EntryType Information -EventId 1000 -Message "Application setup complete"
```

- [ ] Event log source created
- [ ] Test event logged successfully

### 10.2 Configure Application Insights (Optional)

- [ ] Application Insights resource created in Azure
- [ ] Instrumentation key configured in appsettings.json
- [ ] Telemetry flowing to Application Insights

### 10.3 Configure Alerts

- [ ] Disk space alerts configured
- [ ] Application pool crash alerts configured
- [ ] Database connection failure alerts configured
- [ ] High CPU/Memory alerts configured

## Phase 11: Documentation

### 11.1 Document Server Details

Create a document with:
- [ ] Server name and IP address
- [ ] Administrator credentials (secure location)
- [ ] Database connection details
- [ ] SSL certificate details and expiration
- [ ] GitHub token and repository details
- [ ] Contact information for support

### 11.2 Update DNS Records

- [ ] A record for reviews.yourdomain.com points to server IP
- [ ] AAAA record for IPv6 (if applicable)
- [ ] CAA record for SSL certificate authority (if applicable)

## Phase 12: Backup and Disaster Recovery

### 12.1 Configure Automated Backups

```powershell
# Create scheduled task for nightly backups
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
    -Argument '-File C:\Deployments\Scripts\Backup-Application.ps1'

$trigger = New-ScheduledTaskTrigger -Daily -At 2am

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" `
    -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -Action $action -Trigger $trigger `
    -Principal $principal -TaskName "360Review-Backup" `
    -Description "Nightly application backup"
```

- [ ] Backup scheduled task created
- [ ] Test backup runs successfully
- [ ] Backup location has sufficient space

### 12.2 Test Restore Procedure

```powershell
# Test restoring from backup
$latestBackup = Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending | 
    Select-Object -First 1

Write-Host "Testing restore from: $($latestBackup.FullName)"
# Don't actually restore in production, just verify backup is readable
Test-Path "$($latestBackup.FullName)\360review.dll"
```

- [ ] Backup files verified
- [ ] Restore procedure documented
- [ ] Recovery time objective (RTO) acceptable

## Final Checklist

- [ ] All phases completed successfully
- [ ] Application accessible from internet
- [ ] HTTPS working with valid certificate
- [ ] Database migrations applied
- [ ] Logs being written correctly
- [ ] Backups configured
- [ ] Monitoring in place
- [ ] Documentation complete
- [ ] Team notified of go-live
- [ ] Support contacts documented

## Post-Setup Tasks

- [ ] Schedule security patching window
- [ ] Schedule SSL certificate renewal reminder
- [ ] Schedule quarterly disaster recovery test
- [ ] Add server to monitoring dashboard
- [ ] Create runbook for common issues
- [ ] Train team on deployment procedures

---

**Setup Completed By:** ___________________  
**Date:** ___________________  
**Sign-off:** ___________________
