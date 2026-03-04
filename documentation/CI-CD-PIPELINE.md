# CI/CD Pipeline Documentation

## Overview

This document describes the CI/CD pipeline for the 360Review application, including build, test, and deployment processes for Windows Server environments.

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐     ┌──────────────┐
│   GitHub    │────>│    GitHub    │────>│   Staging   │────>│  Production  │
│  Repository │     │    Actions   │     │   Server    │     │    Server    │
└─────────────┘     └──────────────┘     └─────────────┘     └──────────────┘
     Code               Build & Test       Auto Deploy         Manual Deploy
```

## Components

### 1. GitHub Actions Workflow

**File:** `.github/workflows/build-publish.yml`

#### Triggers:
- Push to `main` branch → Production deployment
- Push to `develop` branch → Staging deployment
- Pull requests to `main` → Build and test only
- Manual workflow dispatch → Choose staging or production

#### Jobs:

##### Build and Test
- Runs on: `windows-latest`
- Steps:
  1. Checkout code
  2. Setup .NET 10.0 SDK
  3. Cache NuGet packages
  4. Restore dependencies
  5. Build solution
  6. Run unit tests
  7. Publish test results
  8. Publish application
  9. Generate database migration scripts
  10. Upload build artifact

##### Deploy Staging
- Runs after: Successful build
- Condition: Push to `develop` branch or manual trigger for staging
- Downloads artifact and deploys to staging environment

##### Deploy Production
- Runs after: Successful build
- Condition: Push to `main` branch or manual trigger for production
- Creates deployment package
- Uploads to production server

### 2. Deployment Scripts

#### Deploy-Production.ps1

**Location:** `Powershell/Deploy-Production.ps1`

**Purpose:** Main deployment script for Windows Server

**Features:**
- Application pool management
- Zero-downtime deployment
- Automatic backup before deployment
- Database migration execution
- Health checks
- Automatic rollback on failure
- Deployment logging

**Usage:**
```powershell
.\Deploy-Production.ps1 -BuildNumber "abc123def456"

# With options
.\Deploy-Production.ps1 `
    -BuildNumber "abc123def456" `
    -SkipMigrations `
    -DryRun
```

**Parameters:**
- `BuildNumber` (required): GitHub Actions build SHA
- `ArtifactUrl`: GitHub API URL for artifacts
- `GitHubToken`: GitHub personal access token (use environment variable)
- `SitePath`: IIS website physical path
- `BackupPath`: Backup directory location
- `AppPoolName`: IIS Application Pool name
- `SiteName`: IIS Site name
- `SkipMigrations`: Skip database migrations
- `SkipBackup`: Skip backup creation
- `DryRun`: Test run without making changes

#### Setup-IIS.ps1

**Location:** `Powershell/Setup-IIS.ps1`

**Purpose:** Initial IIS configuration

**Features:**
- IIS feature installation
- Application pool creation
- Website creation
- SSL certificate configuration
- Folder permissions setup
- Sample web.config generation

**Usage:**
```powershell
# Basic setup
.\Setup-IIS.ps1 -HostName "reviews.yourdomain.com"

# With SSL certificate
.\Setup-IIS.ps1 `
    -HostName "reviews.yourdomain.com" `
    -CertificateThumbprint "1234567890ABCDEF..."

# With self-signed certificate (for testing)
.\Setup-IIS.ps1 `
    -HostName "reviews.yourdomain.com" `
    -CreateSelfSignedCert
```

#### Run-Migration.ps1

**Location:** `Powershell/Run-Migration.ps1`

**Purpose:** Database migration management

**Features:**
- Automatic database backup
- Migration script generation
- Transaction-wrapped execution
- Automatic rollback on failure
- Support for PostgreSQL and SQL Server

**Usage:**
```powershell
.\Run-Migration.ps1 `
    -ProjectPath "C:\inetpub\wwwroot\360review\360review.csproj" `
    -DbType "Postgres" `
    -DbHost "db.yourdomain.com" `
    -DbName "360review" `
    -DbUser "appuser" `
    -DbPassword "secure_password"
```

## Setup Instructions

### 1. Initial Server Setup

#### Prerequisites:
- Windows Server 2019 or later
- .NET 10.0 Hosting Bundle
- PostgreSQL client tools (if using PostgreSQL)
- IIS with ASP.NET Core Module v2

#### Steps:

1. **Install IIS and configure site:**
   ```powershell
   # Run as Administrator
   cd C:\Deployments\Scripts
   .\Setup-IIS.ps1 -HostName "reviews.yourdomain.com"
   ```

2. **Create deployment directories:**
   ```powershell
   New-Item -ItemType Directory -Path "C:\Deployments\Backups\360review" -Force
   New-Item -ItemType Directory -Path "C:\Deployments\Logs" -Force
   New-Item -ItemType Directory -Path "C:\Deployments\DB_Backups" -Force
   ```

3. **Configure application settings:**
   - Copy `appsettings.json.example` to `appsettings.json`
   - Update connection strings and secrets
   - Ensure file permissions are correct

### 2. GitHub Configuration

#### Required Secrets:

Add these secrets in GitHub repository settings (Settings → Secrets and variables → Actions):

1. **DEPLOY_SERVER** - Target server hostname or IP
2. **DEPLOY_USER** - Windows user with deployment permissions
3. **DEPLOY_PASSWORD** - User password
4. **GITHUB_TOKEN** - Personal access token with repo access
5. **DB_HOST** - Database server hostname
6. **DB_NAME** - Database name
7. **DB_USER** - Database username
8. **DB_PASSWORD** - Database password

#### Required Environments:

Create environments in GitHub (Settings → Environments):

1. **staging**
   - URL: https://staging.yourdomain.com
   - Protection rules: None (auto-deploy from develop branch)

2. **production**
   - URL: https://yourdomain.com
   - Protection rules: Required reviewers (recommended)
   - Protection rules: Wait timer (optional, e.g., 5 minutes)

### 3. Server-Side Configuration

#### Create Scheduled Task for Deployments

```powershell
# Create a scheduled task that checks for new deployments
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
    -Argument '-File C:\Deployments\Scripts\Deploy-Production.ps1'

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 5)

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -Action $action -Trigger $trigger -Principal $principal `
    -TaskName "360Review-AutoDeploy" -Description "Checks for new deployments"
```

#### Configure Environment Variables

Set these on the server:

```powershell
[Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "your_token_here", "Machine")
[Environment]::SetEnvironmentVariable("DB_HOST", "db.yourdomain.com", "Machine")
[Environment]::SetEnvironmentVariable("DB_NAME", "360review", "Machine")
[Environment]::SetEnvironmentVariable("DB_USER", "appuser", "Machine")
[Environment]::SetEnvironmentVariable("DB_PASSWORD", "secure_password", "Machine")
```

## Deployment Process

### Automatic Deployment Flow

1. **Developer pushes code to repository**
   - Push to `develop` → Triggers staging deployment
   - Push to `main` → Triggers production deployment

2. **GitHub Actions builds application**
   - Restores NuGet packages
   - Compiles solution
   - Runs unit tests
   - Publishes application
   - Generates migration scripts
   - Creates artifact

3. **Artifact is uploaded**
   - Artifact stored in GitHub Actions
   - Retention: 30 days
   - Naming: `360review-{commit-sha}`

4. **Deployment job executes**
   - Downloads artifact
   - Packages for deployment
   - (In production) Manual approval required
   - Triggers server-side deployment

5. **Server-side deployment executes**
   - Stops IIS Application Pool
   - Backs up current deployment
   - Downloads new artifact
   - Deploys application files
   - Runs database migrations
   - Starts Application Pool
   - Performs health checks
   - Rolls back on failure

### Manual Deployment

To manually deploy a specific build:

1. **Via GitHub Actions UI:**
   - Go to Actions tab
   - Select "Build, Test and Deploy" workflow
   - Click "Run workflow"
   - Choose branch and environment
   - Click "Run workflow"

2. **Via PowerShell on server:**
   ```powershell
   cd C:\Deployments\Scripts
   
   # Get the build SHA from GitHub Actions
   $buildSHA = "abc123def456..."
   
   # Run deployment
   .\Deploy-Production.ps1 -BuildNumber $buildSHA
   ```

### Rollback Procedure

If deployment fails or issues are discovered:

1. **Automatic Rollback:**
   - Failed health checks trigger automatic rollback
   - Previous version restored from backup
   - Application pool restarted

2. **Manual Rollback:**
   ```powershell
   # Stop the app pool
   Stop-WebAppPool -Name "360ReviewAppPool"
   
   # Restore from latest backup
   $latestBackup = Get-ChildItem "C:\Deployments\Backups\360review" | 
       Sort-Object CreationTime -Descending | 
       Select-Object -First 1
   
   Remove-Item "C:\inetpub\wwwroot\360review\*" -Recurse -Force
   Copy-Item "$($latestBackup.FullName)\*" -Destination "C:\inetpub\wwwroot\360review" -Recurse
   
   # Start the app pool
   Start-WebAppPool -Name "360ReviewAppPool"
   ```

## Monitoring and Logging

### Application Logs

**Location:** `C:\inetpub\wwwroot\360review\logs\`

- ASP.NET Core logs
- Application insights
- Error logs

### Deployment Logs

**Location:** `C:\Deployments\Logs\`

- `deployment-{timestamp}.log` - Full deployment log
- Contains all steps, errors, and timing information

### IIS Logs

**Location:** `C:\inetpub\logs\LogFiles\`

- HTTP request logs
- Performance metrics

### Monitoring Checklist

- [ ] Application pool status
- [ ] Website binding status
- [ ] Disk space (especially for backups)
- [ ] Database connection health
- [ ] SSL certificate expiration
- [ ] Recent deployment logs
- [ ] Application error logs

## Troubleshooting

### Build Failures

**Symptom:** GitHub Actions build fails

**Solutions:**
1. Check test results in Actions output
2. Verify .NET 10.0 SDK compatibility
3. Review NuGet package compatibility
4. Check for compilation errors

### Deployment Failures

**Symptom:** Deployment script fails

**Solutions:**
1. Check deployment log: `C:\Deployments\Logs\deployment-{timestamp}.log`
2. Verify GitHub token has correct permissions
3. Ensure artifact is accessible
4. Check disk space
5. Verify IIS application pool is stopped during deployment

### Application Not Starting

**Symptom:** 502.5 error or application pool crashes

**Solutions:**
1. Check `stdout` logs in `C:\inetpub\wwwroot\360review\logs\`
2. Verify .NET 10.0 Hosting Bundle is installed
3. Check `web.config` configuration
4. Verify application pool identity has correct permissions
5. Review Windows Event Viewer

### Database Migration Failures

**Symptom:** Migration script fails

**Solutions:**
1. Check migration log
2. Verify database connection string
3. Ensure database user has correct permissions
4. Review migration SQL script for errors
5. Restore from backup and retry

### Health Check Failures

**Symptom:** Deployment completes but health check fails

**Solutions:**
1. Verify `/health` endpoint is accessible
2. Check application logs for startup errors
3. Verify all required configuration is present
4. Check database connectivity
5. Review certificate configuration

## Security Best Practices

1. **Use Managed Identities** where possible instead of passwords
2. **Rotate secrets** regularly (GitHub tokens, database passwords)
3. **Enable HTTPS** for all production traffic
4. **Restrict folder permissions** to minimum required
5. **Keep backups encrypted** and in secure location
6. **Enable security headers** in IIS
7. **Regular security updates** for Windows Server and .NET
8. **Monitor access logs** for suspicious activity
9. **Use Network Security Groups** to restrict server access
10. **Enable MFA** for all administrative accounts

## Maintenance

### Regular Tasks

**Daily:**
- Review application logs for errors
- Monitor disk space

**Weekly:**
- Review deployment logs
- Check backup integrity
- Review security logs

**Monthly:**
- Test rollback procedure
- Review and clean old backups
- Update Windows Server
- Review SSL certificate expiration

**Quarterly:**
- Disaster recovery test
- Security audit
- Performance review
- Capacity planning

## Support and Escalation

### Issue Severity Levels

**P0 - Critical:** Production down
- Response: Immediate
- Action: Rollback and investigate

**P1 - High:** Major functionality impaired
- Response: Within 1 hour
- Action: Hotfix deployment

**P2 - Medium:** Minor functionality impaired
- Response: Within 4 hours
- Action: Include in next scheduled deployment

**P3 - Low:** Enhancement or minor issue
- Response: Within 1 business day
- Action: Plan for future sprint

### Contact Information

- **DevOps Team:** devops@yourdomain.com
- **On-Call:** [On-call rotation]
- **Database Admin:** dba@yourdomain.com

## Additional Resources

- [ASP.NET Core Hosting on IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [IIS Administration](https://learn.microsoft.com/en-us/iis/)
