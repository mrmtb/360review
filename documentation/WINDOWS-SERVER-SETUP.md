# Windows Server Setup Guide - Endatix + 360Review

## Deployment Architecture

```
Windows Server
├── IIS / Kestrel
│   └── 360Review MVC App (Port 5000 or 80/443)
├── Endatix Service
│   └── .NET API (Port 5001)
└── PostgreSQL
    ├── reviews_mvc (360Review database)
    └── endatix (Endatix database)
```

Both applications run on the same Windows Server, communicating via localhost.

## Prerequisites

- ✅ Windows Server 2016 or later
- ✅ .NET 6.0 or later SDK installed
- ✅ PostgreSQL installed and running
- ✅ Git installed
- ✅ PowerShell 5.1 or later

## Step-by-Step Setup

### Step 1: Create Endatix Database

Open PowerShell as Administrator and connect to PostgreSQL:

```powershell
# Connect to PostgreSQL
psql -U reviews_user -d postgres

# Or if using postgres superuser
psql -U postgres
```

Create the Endatix database:

```sql
-- Create database
CREATE DATABASE endatix;

-- Grant privileges to existing user
GRANT ALL PRIVILEGES ON DATABASE endatix TO reviews_user;

-- Exit psql
\q
```

### Step 2: Clone and Setup Endatix

```powershell
# Navigate to your applications directory
cd C:\inetpub

# Clone Endatix repository
git clone https://github.com/endatix/endatix.git
cd endatix
```

### Step 3: Configure Endatix

Edit the configuration file:

```powershell
notepad src\Endatix.Api\appsettings.json
```

Update the configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=endatix;Username=reviews_user;Password=YOUR_STRONG_PASSWORD"
  },
  "Urls": "http://localhost:5001",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 4: Build and Initialize Endatix

```powershell
# Restore dependencies
dotnet restore src/Endatix.Api/Endatix.Api.csproj

# Build the project
dotnet build src/Endatix.Api/Endatix.Api.csproj --configuration Release

# Apply database migrations
cd src\Endatix.Api
dotnet ef database update

# If dotnet ef is not installed:
# dotnet tool install --global dotnet-ef
```

### Step 5: Test Endatix Locally

Start Endatix in development mode to verify it works:

```powershell
# Run from the Endatix.Api directory
dotnet run --urls "http://localhost:5001"
```

In another PowerShell window, test the health endpoint:

```powershell
Invoke-WebRequest -Uri http://localhost:5001/api/health
```

You should see a 200 OK response. Press `Ctrl+C` in the first window to stop the test run.

### Step 6: Publish Endatix for Production

```powershell
# Go back to the repository root
cd C:\inetpub\endatix

# Publish to a production directory
dotnet publish src/Endatix.Api/Endatix.Api.csproj `
  -c Release `
  -o C:\Services\Endatix `
  --self-contained false
```

### Step 7: Setup Endatix as a Windows Service

#### Option A: Using NSSM (Recommended)

1. **Download NSSM** (Non-Sucking Service Manager):
   - Visit: https://nssm.cc/download
   - Download nssm-2.24.zip (or latest)
   - Extract to `C:\Tools\nssm`

2. **Install the service**:

```powershell
# Add NSSM to PATH temporarily
$env:Path += ";C:\Tools\nssm\win64"

# Install Endatix as a service
nssm install EndatixService "C:\Program Files\dotnet\dotnet.exe" "C:\Services\Endatix\Endatix.Api.dll"

# Set working directory
nssm set EndatixService AppDirectory "C:\Services\Endatix"

# Set startup type to automatic
nssm set EndatixService Start SERVICE_AUTO_START

# Set service description
nssm set EndatixService Description "Endatix Survey Backend API"

# Configure logging
nssm set EndatixService AppStdout "C:\Services\Endatix\logs\stdout.log"
nssm set EndatixService AppStderr "C:\Services\Endatix\logs\stderr.log"

# Start the service
nssm start EndatixService

# Check service status
nssm status EndatixService
```

#### Option B: Using sc.exe (Built-in)

```powershell
sc.exe create EndatixService `
  binPath= "\"C:\Program Files\dotnet\dotnet.exe\" \"C:\Services\Endatix\Endatix.Api.dll\"" `
  start= auto `
  DisplayName= "Endatix Survey API"

sc.exe start EndatixService
```

### Step 8: Verify Endatix Service

```powershell
# Check if service is running
Get-Service EndatixService

# Test the API
Invoke-WebRequest -Uri http://localhost:5001/api/health

# View service logs (if using NSSM)
Get-Content C:\Services\Endatix\logs\stdout.log -Tail 50
```

### Step 9: Configure Firewall (If Needed)

If you need to access Endatix from other machines (not typical for this setup):

```powershell
New-NetFirewallRule `
  -DisplayName "Endatix API" `
  -Direction Inbound `
  -LocalPort 5001 `
  -Protocol TCP `
  -Action Allow `
  -Profile Domain,Private
```

### Step 10: Upload Survey Template to Endatix

Once Endatix is running, upload your survey template:

```powershell
# Read the survey JSON
$surveyJson = Get-Content "C:\inetpub\360review_mvc\survey3.json" -Raw | ConvertFrom-Json | ConvertTo-Json -Compress

# Create the request body
$body = @{
    name = "Leadership Competency Survey"
    description = "360-degree feedback survey for leadership assessment"
    formDefinition = $surveyJson
} | ConvertTo-Json

# Upload to Endatix
$response = Invoke-RestMethod `
  -Uri "http://localhost:5001/api/forms" `
  -Method Post `
  -Body $body `
  -ContentType "application/json"

# Display the template ID
Write-Host "Template ID: $($response.id)"
Write-Host "Save this ID and update appsettings.json with Survey:DefaultTemplateId"
```

### Step 11: Update 360Review Configuration

Edit your 360Review `appsettings.json`:

```powershell
cd C:\inetpub\360review_mvc
notepad appsettings.json
```

Update the Survey section with the template ID from Step 10:

```json
{
  "Survey": {
    "DefaultTemplateId": "PASTE_TEMPLATE_ID_HERE",
    "TemplatePath": "survey3.json"
  }
}
```

### Step 12: Apply 360Review Database Migration

```powershell
cd C:\inetpub\360review_mvc

# Apply the migration
dotnet ef database update

# Or just start the application (auto-migration is enabled)
```

### Step 13: Test the Integration

1. **Get a test survey password**:

```powershell
# Connect to PostgreSQL
psql -U reviews_user -d reviews_mvc

# Query for a survey password
SELECT "EvaluationParticipantSurveyPassword" 
FROM "EvaluationParticipants" 
LIMIT 1;

# Copy the password and exit
\q
```

2. **Start your 360Review application** (if not already running)

3. **Navigate to**: `http://localhost:5000/survey/{password}`

4. **Verify**:
   - ✅ Survey page loads
   - ✅ SurveyJS renders the survey
   - ✅ You can fill out questions
   - ✅ Autosave works (check browser console after 3 seconds)
   - ✅ Final submission works

## Service Management Commands

### Check Service Status

```powershell
# Using NSSM
nssm status EndatixService

# Using Windows Service Manager
Get-Service EndatixService

# View detailed info
Get-Service EndatixService | Select-Object *
```

### Start/Stop/Restart Service

```powershell
# Using NSSM
nssm start EndatixService
nssm stop EndatixService
nssm restart EndatixService

# Using PowerShell
Start-Service EndatixService
Stop-Service EndatixService
Restart-Service EndatixService
```

### View Logs

```powershell
# If using NSSM with logging configured
Get-Content C:\Services\Endatix\logs\stdout.log -Tail 50 -Wait

# View errors
Get-Content C:\Services\Endatix\logs\stderr.log -Tail 50
```

### Update Endatix

```powershell
# Stop the service
Stop-Service EndatixService

# Pull latest changes
cd C:\inetpub\endatix
git pull

# Build and publish
dotnet publish src/Endatix.Api/Endatix.Api.csproj `
  -c Release `
  -o C:\Services\Endatix `
  --self-contained false

# Apply any new migrations
cd src\Endatix.Api
dotnet ef database update

# Start the service
Start-Service EndatixService
```

## Troubleshooting

### Service Won't Start

1. **Check Event Viewer**:
   ```powershell
   Get-EventLog -LogName Application -Source EndatixService -Newest 10
   ```

2. **Verify .NET is installed**:
   ```powershell
   dotnet --version
   ```

3. **Test manual start**:
   ```powershell
   cd C:\Services\Endatix
   dotnet Endatix.Api.dll
   ```

### Database Connection Issues

1. **Test PostgreSQL connection**:
   ```powershell
   psql -U reviews_user -d endatix
   ```

2. **Verify connection string** in `C:\Services\Endatix\appsettings.json`

3. **Check PostgreSQL is accepting connections**:
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 5432
   ```

### Port Already in Use

```powershell
# Check what's using port 5001
netstat -ano | findstr :5001

# Kill the process if needed (use Task Manager or taskkill)
```

### Survey Page Shows Error

1. **Check 360Review logs**
2. **Verify Endatix is running**: `http://localhost:5001/api/health`
3. **Check browser console** for JavaScript errors
4. **Verify template ID** in appsettings.json

## Production Considerations

### SSL/TLS Configuration

For production, configure HTTPS:

1. **Obtain SSL certificate** (Let's Encrypt, commercial CA, or self-signed)

2. **Configure Endatix for HTTPS**:

Edit `C:\Services\Endatix\appsettings.json`:

```json
{
  "Urls": "https://localhost:5001",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "C:\\Certificates\\endatix.pfx",
          "Password": "your-cert-password"
        }
      }
    }
  }
}
```

Update 360Review `appsettings.Production.json`:

```json
{
  "Endatix": {
    "ApiBaseUrl": "https://localhost:5001"
  }
}
```

### Performance Tuning

1. **Connection Pooling**: Already configured in PostgreSQL connection strings

2. **Memory Limits**: Set in NSSM if needed:
   ```powershell
   nssm set EndatixService AppEnvironmentExtra DOTNET_GCHeapHardLimit=1000000000
   ```

3. **Logging Level**: Set to Warning or Error in production

### Backup Strategy

```powershell
# Backup Endatix database
pg_dump -U reviews_user -d endatix > C:\Backups\endatix_backup_$(Get-Date -Format 'yyyyMMdd').sql

# Include in your existing backup script
```

### Monitoring

Set up monitoring for:
- Endatix service uptime
- API response times
- Database connection pool
- Disk space for logs

## Security Checklist

- [ ] Strong passwords for database users
- [ ] Firewall rules configured (only allow localhost for Endatix)
- [ ] SSL/TLS enabled for production
- [ ] Service account with minimal permissions
- [ ] Regular security updates applied
- [ ] Logs reviewed regularly
- [ ] Database backups tested

## Support

- **Endatix Documentation**: https://docs.endatix.com
- **Endatix GitHub**: https://github.com/endatix/endatix
- **Report Issues**: Create issue in your repository or Endatix GitHub

---

**Last Updated**: March 3, 2026
**Platform**: Windows Server 2016+
**Status**: Production Ready
