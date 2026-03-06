# Windows Server Quick Reference Card

## 🚀 Quick Setup Commands

Copy and paste these commands in PowerShell (as Administrator):

### 1. Create Database
```powershell
psql -U reviews_user -d postgres -c "CREATE DATABASE endatix; GRANT ALL PRIVILEGES ON DATABASE endatix TO reviews_user;"
```

### 2. Clone & Build Endatix
```powershell
cd C:\inetpub
git clone https://github.com/endatix/endatix.git
cd endatix
dotnet build src/Endatix.Api/Endatix.Api.csproj -c Release
```

### 3. Configure Connection String
```powershell
# Edit src/Endatix.Api/appsettings.json
# Set: "DefaultConnection": "Host=localhost;Port=5432;Database=endatix;Username=reviews_user;Password=YOUR_STRONG_PASSWORD"
# Set: "Urls": "http://localhost:5001"
# Prefer storing this in environment variables or your server secret store instead of source-controlled config.
```

### 4. Run Migrations
```powershell
cd src\Endatix.Api
dotnet ef database update
```

### 5. Test Run
```powershell
dotnet run --urls "http://localhost:5001"
# In another window: Invoke-WebRequest -Uri http://localhost:5001/api/health
# Ctrl+C to stop
```

### 6. Publish for Production
```powershell
cd C:\inetpub\endatix
dotnet publish src/Endatix.Api/Endatix.Api.csproj -c Release -o C:\Services\Endatix
```

### 7. Setup Windows Service (NSSM)
```powershell
# Download NSSM from https://nssm.cc/download and extract to C:\Tools\nssm
$env:Path += ";C:\Tools\nssm\win64"
nssm install EndatixService "C:\Program Files\dotnet\dotnet.exe" "C:\Services\Endatix\Endatix.Api.dll"
nssm set EndatixService AppDirectory "C:\Services\Endatix"
nssm start EndatixService
```

### 8. Upload Survey Template
```powershell
cd C:\inetpub\360review_mvc
$surveyJson = Get-Content "survey3.json" -Raw | ConvertFrom-Json | ConvertTo-Json -Compress
$body = @{name="Leadership Competency Survey";description="360-degree feedback";formDefinition=$surveyJson} | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:5001/api/forms" -Method Post -Body $body -ContentType "application/json"
Write-Host "Template ID: $($response.id)" -ForegroundColor Green
```

### 9. Update 360Review Config
```powershell
# Edit appsettings.json
# Set Survey:DefaultTemplateId to the ID from step 8
```

### 10. Apply Migration & Test
```powershell
dotnet ef database update
# Navigate to: http://localhost:5000/survey/{password}
```

---

## 🔧 Service Management

```powershell
# Check status
Get-Service EndatixService

# Start/Stop/Restart
Start-Service EndatixService
Stop-Service EndatixService
Restart-Service EndatixService

# View logs (if configured)
Get-Content C:\Services\Endatix\logs\stdout.log -Tail 50 -Wait
```

---

## 🧪 Testing Commands

```powershell
# Test Endatix health
Invoke-WebRequest -Uri http://localhost:5001/api/health

# Get survey password for testing
psql -U reviews_user -d reviews_mvc -c "SELECT \"EvaluationParticipantSurveyPassword\" FROM \"EvaluationParticipants\" LIMIT 1;"

# Test PostgreSQL connection
Test-NetConnection -ComputerName localhost -Port 5432
```

---

## 📁 Important Paths

| Component | Path |
|-----------|------|
| Endatix Source | `C:\inetpub\endatix` |
| Endatix Production | `C:\Services\Endatix` |
| 360Review App | `C:\inetpub\360review_mvc` |
| Endatix Config | `C:\Services\Endatix\appsettings.json` |
| Endatix Logs | `C:\Services\Endatix\logs\` |

---

## 🔗 URLs

| Service | URL |
|---------|-----|
| Endatix API | http://localhost:5001 |
| Endatix Health | http://localhost:5001/api/health |
| 360Review | http://localhost:5000 |
| Survey | http://localhost:5000/survey/{password} |

---

## 🆘 Troubleshooting

| Issue | Solution |
|-------|----------|
| Service won't start | Check Event Viewer: `Get-EventLog -LogName Application -Newest 10` |
| Port in use | `netstat -ano \| findstr :5001` |
| DB connection error | Test: `psql -U reviews_user -d endatix` |
| Survey not loading | Verify Endatix: `http://localhost:5001/api/health` |

---

## 📚 Documentation

- **Full Setup Guide**: [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md)
- **Integration Guide**: [ENDATIX-INTEGRATION-GUIDE.md](ENDATIX-INTEGRATION-GUIDE.md)
- **Implementation Summary**: [ENDATIX-IMPLEMENTATION-SUMMARY.md](ENDATIX-IMPLEMENTATION-SUMMARY.md)

---

**Last Updated**: March 3, 2026
