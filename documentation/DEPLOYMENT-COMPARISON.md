# Deployment Options Comparison

Comparison between Standard IIS and Plesk deployment for 360Review application.

## Overview

This project now includes two deployment options:

1. **Standard IIS Deployment** - Direct deployment to IIS on Windows Server
2. **Plesk Deployment** - Deployment through Plesk control panel

## Quick Comparison

| Feature | Standard IIS | Plesk |
|---------|-------------|-------|
| **Setup Complexity** | Medium | Easy |
| **Management** | PowerShell/IIS Manager | Plesk UI + PowerShell |
| **SSL Certificates** | Manual or ACME | Built-in Let's Encrypt |
| **Multi-site Hosting** | Manual configuration | Easy multi-tenant |
| **Monitoring** | Custom scripts | Built-in + Custom |
| **Backup** | Custom scripts | Built-in + Custom |
| **Cost** | Free (Windows Server included) | Plesk license required |
| **Best For** | Single application servers | Hosting multiple sites |

## Detailed Comparison

### Setup & Configuration

#### Standard IIS
**Pros:**
- Full control over IIS configuration
- No additional licensing costs
- Direct access to all Windows Server features
- Simpler architecture (fewer layers)

**Cons:**
- More manual configuration required
- Need to manage SSL certificates manually
- No web-based management UI
- Requires more Windows/IIS knowledge

**Setup Steps:**
```powershell
# Standard IIS setup
.\Setup-IIS.ps1 -SiteName "360review" -Port 80 -AppPoolName "360ReviewAppPool"
```

#### Plesk
**Pros:**
- Web-based UI for easy management
- Automatic Let's Encrypt SSL
- Multi-domain management built-in
- Email server integration
- Built-in backups and monitoring

**Cons:**
- Additional licensing cost
- Extra layer of complexity
- Plesk-specific directory structure
- Must work within Plesk conventions

**Setup Steps:**
```powershell
# Plesk setup (after creating domain in UI)
.\Setup-Plesk.ps1 -Domain "reviews.yourdomain.com"
```

### File Structure

#### Standard IIS
```
C:\inetpub\
└── wwwroot\
    └── 360review\
        ├── 360review.dll
        ├── appsettings.json
        ├── web.config
        ├── logs\
        └── wwwroot\
```

#### Plesk
```
C:\Inetpub\vhosts\
└── reviews.yourdomain.com\
    ├── httpdocs\           # Your application
    │   ├── 360review.dll
    │   ├── appsettings.json
    │   ├── web.config
    │   └── logs\
    ├── logs\               # IIS logs
    ├── statistics\         # Plesk stats
    └── plesk-stat\         # Plesk analytics
```

### Deployment Scripts

#### Standard IIS
**File:** `Deploy-Production.ps1`
- Deploys to `C:\inetpub\wwwroot\sitename`
- Manages IIS app pool directly
- Uses `Restart-WebAppPool`
- Simpler backup/restore

#### Plesk
**File:** `Deploy-Production-Plesk.ps1`
- Deploys to `C:\Inetpub\vhosts\domain\httpdocs`
- Works with Plesk's domain structure
- Uses `websrvmng.exe` for recycling
- Respects Plesk protected directories

### SSL Certificate Management

#### Standard IIS
```powershell
# Manual certificate installation
$cert = Import-PfxCertificate -FilePath "cert.pfx" -CertStoreLocation Cert:\LocalMachine\My
New-WebBinding -Name "360review" -Protocol https -Port 443
$binding = Get-WebBinding -Name "360review" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "my")
```

**Or use Certify The Web or win-acme for Let's Encrypt:**
```powershell
# Using win-acme
wacs.exe --target iis --siteid 1 --accepttos --emailaddress admin@domain.com
```

#### Plesk
**Via UI:**
1. Navigate to SSL/TLS Certificates
2. Click "Install" under Let's Encrypt
3. Automatic renewal configured

**Via Command Line:**
```powershell
& "${env:plesk_bin}\certificate.exe" --install-letsencrypt -domain reviews.yourdomain.com
```

### Multi-Site Hosting

#### Standard IIS
**Manual configuration for each site:**
```powershell
# Site 1
New-WebAppPool -Name "Site1AppPool"
New-Website -Name "Site1" -PhysicalPath "C:\inetpub\wwwroot\site1" -ApplicationPool "Site1AppPool" -Port 80 -HostHeader "site1.com"

# Site 2
New-WebAppPool -Name "Site2AppPool"
New-Website -Name "Site2" -PhysicalPath "C:\inetpub\wwwroot\site2" -ApplicationPool "Site2AppPool" -Port 80 -HostHeader "site2.com"
```

#### Plesk
**Easy domain management:**
- Add new domain through UI
- Automatic app pool creation
- Automatic directory structure
- Individual SSL per domain
- Per-domain PHP/ASP.NET settings

### Monitoring & Maintenance

#### Standard IIS
**Custom monitoring required:**
```powershell
# Custom health check script
$response = Invoke-WebRequest -Uri "http://localhost/health"
if ($response.StatusCode -ne 200) {
    Restart-WebAppPool -Name "360ReviewAppPool"
}
```

**Logs:**
- Application logs: Custom location
- IIS logs: `C:\inetpub\logs\LogFiles`
- Event Viewer for system events

#### Plesk
**Built-in monitoring:**
- Resource usage graphs in UI
- Application pool monitoring
- Disk space alerts
- Custom health checks via scripts

**Logs accessible via:**
- Plesk UI: Websites & Domains → Logs
- File system: Multiple locations
- Integrated log viewer

### Backup & Restore

#### Standard IIS
**Custom backup script:**
```powershell
# Backup
Copy-Item "C:\inetpub\wwwroot\360review" -Destination "D:\Backups\360review-$(Get-Date -Format yyyyMMdd)" -Recurse

# Restore
Copy-Item "D:\Backups\360review-20260121" -Destination "C:\inetpub\wwwroot\360review" -Recurse
Restart-WebAppPool -Name "360ReviewAppPool"
```

#### Plesk
**Built-in backup manager:**
- Scheduled backups via UI
- Includes files, database, email, DNS
- FTP backup to remote location
- One-click restore

**Plus custom scripting:**
```powershell
# Backup via script (included in deployment)
.\Deploy-Production-Plesk.ps1 creates automatic backups
```

### Application Pool Management

#### Standard IIS
```powershell
# Direct access
Import-Module WebAdministration

# Control app pool
Stop-WebAppPool -Name "360ReviewAppPool"
Start-WebAppPool -Name "360ReviewAppPool"
Restart-WebAppPool -Name "360ReviewAppPool"

# Configure settings
Set-ItemProperty "IIS:\AppPools\360ReviewAppPool" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\360ReviewAppPool" -Name "processModel.idleTimeout" -Value "00:00:00"
```

#### Plesk
```powershell
# Via Plesk command
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name="reviews.yourdomain.com"

# Or direct IIS access if available
Import-Module WebAdministration
Restart-WebAppPool -Name "reviews.yourdomain.com"
```

### Database Management

#### Standard IIS
**Fully manual:**
- Install PostgreSQL/SQL Server manually
- Configure connections manually
- Set up backups via pg_dump/SQL Agent
- Manage users and permissions

#### Plesk
**Integrated database management:**
- PostgreSQL/MySQL/SQL Server via Plesk
- Web-based phpPgAdmin/phpMyAdmin
- Built-in backup scheduling
- User management via UI

**Or external database:**
- Connect to external DB server
- Same as standard IIS

### Environment Variables

#### Standard IIS
```powershell
# Machine-level
[Environment]::SetEnvironmentVariable("DB_HOST", "localhost", "Machine")

# App pool level (via IIS Manager or PowerShell)
Set-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
    -filter "system.applicationHost/applicationPools/add[@name='360ReviewAppPool']/environmentVariables" `
    -name "." -value @{name='DB_HOST';value='localhost'}
```

#### Plesk
```powershell
# Via Plesk UI:
# Websites & Domains → Domain → Hosting Settings → Environment Variables

# Via PowerShell:
[Environment]::SetEnvironmentVariable("DB_HOST", "localhost", "Machine")
& "${env:plesk_bin}\websrvmng.exe" --reconfigure-vhost --vhost-name="reviews.yourdomain.com"
```

### Cost Analysis

#### Standard IIS
**Initial Costs:**
- Windows Server license: ~$500-$1000 (one-time)
- No additional software costs

**Ongoing Costs:**
- Server maintenance
- Your time for management
- Optional: Monitoring tools

**Total Annual:** Server costs + admin time

#### Plesk
**Initial Costs:**
- Windows Server license: ~$500-$1000 (one-time)
- Plesk license: ~$10-40/month depending on plan

**Ongoing Costs:**
- Plesk license: ~$120-480/year
- Less admin time due to UI
- Built-in features (no additional tools needed)

**Total Annual:** Server costs + Plesk license + reduced admin time

### Performance Considerations

#### Standard IIS
- Slightly lower overhead (no Plesk layer)
- Direct IIS access
- More flexibility for optimization
- Can fine-tune every setting

#### Plesk
- Minimal overhead from Plesk
- Plesk manages IIS configuration
- Some settings restricted by Plesk
- Generally comparable performance

**Verdict:** Performance difference is negligible for most applications.

## Which Should You Choose?

### Choose Standard IIS If:
- ✅ Single application deployment
- ✅ Want full control over Windows Server
- ✅ Have Windows Server/IIS expertise
- ✅ Want to minimize licensing costs
- ✅ Don't need web-based management
- ✅ Managing your own SSL certificates

### Choose Plesk If:
- ✅ Hosting multiple websites/domains
- ✅ Want web-based management UI
- ✅ Need easy SSL certificate management
- ✅ Want built-in backup/monitoring
- ✅ Less Windows Server experience
- ✅ Managing sites for multiple clients
- ✅ Need email hosting integration
- ✅ Want streamlined domain management

## Migration Between Options

### IIS to Plesk
1. Create domain in Plesk
2. Run `Setup-Plesk.ps1`
3. Copy application files to new location:
   ```powershell
   Copy-Item "C:\inetpub\wwwroot\360review\*" `
       -Destination "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs" -Recurse
   ```
4. Update DNS if needed
5. Configure SSL in Plesk
6. Test application

### Plesk to IIS
1. Run `Setup-IIS.ps1`
2. Copy application files:
   ```powershell
   Copy-Item "C:\Inetpub\vhosts\reviews.yourdomain.com\httpdocs\*" `
       -Destination "C:\inetpub\wwwroot\360review" -Recurse
   ```
3. Configure SSL certificates manually
4. Update DNS if needed
5. Remove domain from Plesk (optional)
6. Test application

## Files for Each Option

### Standard IIS Files
- `Powershell/Deploy-Production.ps1`
- `Powershell/Setup-IIS.ps1`
- `Powershell/Run-Migration.ps1`
- `.github/workflows/build-publish.yml`
- `CI-CD-PIPELINE.md`
- `CI-CD-QUICK-REFERENCE.md`
- `SERVER-SETUP-CHECKLIST.md`

### Plesk Files
- `Powershell/Deploy-Production-Plesk.ps1`
- `Powershell/Setup-Plesk.ps1`
- `Powershell/Run-Migration.ps1` (shared)
- `.github/workflows/build-publish-plesk.yml`
- `PLESK-DEPLOYMENT-GUIDE.md`
- `PLESK-QUICK-REFERENCE.md`

### Shared Files
- `appsettings.json`
- `web.config`
- `Program.cs`
- All application code
- Database migration scripts

## Recommendations

### Development/Staging
**Recommendation:** Either option works well
- Standard IIS for simplicity if single environment
- Plesk if you want to match production setup

### Production - Single Application
**Recommendation:** Standard IIS
- Lower cost (no Plesk license)
- Full control
- Simpler architecture

### Production - Multiple Applications
**Recommendation:** Plesk
- Easy multi-domain management
- Built-in SSL via Let's Encrypt
- Centralized management
- Worth the licensing cost for time savings

### Production - Client Hosting
**Recommendation:** Plesk
- Client-friendly web interface
- Per-domain isolation
- Easy delegation of access
- Professional appearance

## Support Resources

### Standard IIS
- **Microsoft IIS Docs**: https://learn.microsoft.com/en-us/iis/
- **ASP.NET Core Hosting**: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis
- **Project Documentation**: See `CI-CD-PIPELINE.md`

### Plesk
- **Plesk Documentation**: https://docs.plesk.com/
- **ASP.NET Core on Plesk**: https://docs.plesk.com/en-US/current/administrator-guide/website-management/asp-net-support.77997/
- **Project Documentation**: See `PLESK-DEPLOYMENT-GUIDE.md`

---

## Summary Decision Matrix

| Scenario | Best Choice | Why |
|----------|-------------|-----|
| Single .NET app, experienced admin | Standard IIS | Full control, no extra cost |
| Multiple websites | Plesk | Easy management, worth license |
| Need Let's Encrypt automation | Plesk | Built-in support |
| Budget-conscious, single app | Standard IIS | No license fees |
| Less Windows Server experience | Plesk | User-friendly UI |
| Maximum performance tuning | Standard IIS | Direct access to all settings |
| Client/customer hosting | Plesk | Professional control panel |
| Quick deployment of multiple apps | Plesk | Faster setup per site |

**Both options are fully supported in this project. Choose based on your specific needs!**
