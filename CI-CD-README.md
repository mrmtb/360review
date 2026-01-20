# CI/CD Pipeline for 360Review

Complete continuous integration and deployment pipeline for Windows Server deployment.

## 📋 Documentation

- **[CI/CD Pipeline Documentation](CI-CD-PIPELINE.md)** - Complete guide to the CI/CD pipeline
- **[Quick Reference Guide](CI-CD-QUICK-REFERENCE.md)** - Common commands and procedures
- **[Server Setup Checklist](SERVER-SETUP-CHECKLIST.md)** - Step-by-step server configuration

## 🚀 Quick Start

### For Developers

1. **Make changes and push to GitHub:**
   ```bash
   git checkout -b feature/my-feature
   # Make changes
   git add .
   git commit -m "Add new feature"
   git push origin feature/my-feature
   ```

2. **Create pull request to `develop`** for staging deployment

3. **Merge to `main`** for production deployment

### For DevOps/Administrators

1. **Initial server setup:**
   ```powershell
   # Run as Administrator
   .\Powershell\Setup-IIS.ps1 -HostName "reviews.yourdomain.com"
   ```

2. **Configure GitHub secrets** (see [CI-CD-PIPELINE.md](CI-CD-PIPELINE.md#github-configuration))

3. **Manual deployment:**
   ```powershell
   .\Powershell\Deploy-Production.ps1 -BuildNumber "<commit-sha>"
   ```

## 📁 Repository Structure

```
.github/
└── workflows/
    └── build-publish.yml          # GitHub Actions workflow
Powershell/
├── Deploy-Production.ps1          # Main deployment script
├── Setup-IIS.ps1                  # IIS initial setup
├── Run-Migration.ps1              # Database migration script
└── DeployFromGithub.ps1          # Legacy deployment script
CI-CD-PIPELINE.md                  # Full documentation
CI-CD-QUICK-REFERENCE.md          # Quick commands
SERVER-SETUP-CHECKLIST.md         # Setup checklist
```

## 🔄 Deployment Flow

```
┌──────────────┐
│ Push to Git  │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│GitHub Actions│
│  Build & Test│
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Artifact   │
│   Created    │
└──────┬───────┘
       │
       ├─────────────────┐
       ▼                 ▼
┌─────────────┐   ┌──────────────┐
│   Staging   │   │ Production   │
│(auto-deploy)│   │(manual/auto) │
└─────────────┘   └──────────────┘
```

## 🛠️ Features

### CI/CD Pipeline
- ✅ Automated build and test on push
- ✅ Test result reporting
- ✅ Build artifact creation
- ✅ Separate staging and production environments
- ✅ Manual deployment approval for production
- ✅ Environment-specific configurations

### Deployment Script
- ✅ Zero-downtime deployment
- ✅ Automatic backup before deployment
- ✅ Database migration execution
- ✅ Health checks
- ✅ Automatic rollback on failure
- ✅ Comprehensive logging
- ✅ Dry-run mode for testing

### Database Migrations
- ✅ Automatic database backup
- ✅ Transaction-wrapped migrations
- ✅ Rollback on failure
- ✅ Support for PostgreSQL and SQL Server
- ✅ Idempotent migration scripts

## 🔐 Required Secrets

Configure these in GitHub repository settings:

| Secret | Description |
|--------|-------------|
| `DEPLOY_SERVER` | Target server hostname or IP |
| `DEPLOY_USER` | Windows user with deployment permissions |
| `DEPLOY_PASSWORD` | User password |
| `GITHUB_TOKEN` | Personal access token with repo access |
| `DB_HOST` | Database server hostname |
| `DB_NAME` | Database name |
| `DB_USER` | Database username |
| `DB_PASSWORD` | Database password |

## 📊 Monitoring

### Health Checks
- Application pool status
- Website binding status
- Application health endpoint
- Database connectivity

### Logs
- **Deployment:** `C:\Deployments\Logs\`
- **Application:** `C:\inetpub\wwwroot\360review\logs\`
- **IIS:** `C:\inetpub\logs\LogFiles\`

### Backups
- **Location:** `C:\Deployments\Backups\360review\`
- **Retention:** Last 10 deployments
- **Database:** `C:\Deployments\DB_Backups\`

## 🆘 Emergency Procedures

### Quick Rollback
```powershell
$latestBackup = Get-ChildItem "C:\Deployments\Backups\360review" | 
    Sort-Object CreationTime -Descending | Select-Object -First 1
Stop-WebAppPool -Name "360ReviewAppPool"
Remove-Item "C:\inetpub\wwwroot\360review\*" -Recurse -Force
Copy-Item "$($latestBackup.FullName)\*" -Destination "C:\inetpub\wwwroot\360review" -Recurse
Start-WebAppPool -Name "360ReviewAppPool"
```

### Restart Application
```powershell
Restart-WebAppPool -Name "360ReviewAppPool"
```

### View Recent Errors
```powershell
Get-Content "C:\inetpub\wwwroot\360review\logs\stdout-*.log" -Tail 50
```

## 📞 Support

- **DevOps Team:** devops@yourdomain.com
- **On-Call:** [Rotation Schedule]
- **Documentation:** See [CI-CD-PIPELINE.md](CI-CD-PIPELINE.md)

## 🔧 Troubleshooting

See [CI-CD-QUICK-REFERENCE.md](CI-CD-QUICK-REFERENCE.md#troubleshooting) for common issues and solutions.

## 📚 Additional Resources

- [ASP.NET Core on IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Entity Framework Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PowerShell Documentation](https://learn.microsoft.com/en-us/powershell/)

---

**Version:** 1.0  
**Last Updated:** January 20, 2026  
**Maintained By:** DevOps Team
