# Development vs Production Setup

## Overview

This project supports development on **MacOS** and production deployment on **Windows Server**.

## Quick Start by Platform

### 🍎 MacOS Development

**Quick Start:**
```bash
./setup-endatix-dev.sh
```

**Full Guide:** [MACOS-DEVELOPMENT-SETUP.md](MACOS-DEVELOPMENT-SETUP.md)  
**Quick Reference:** [MACOS-QUICKSTART.md](MACOS-QUICKSTART.md)

**Key Points:**
- Uses Docker or native .NET for Endatix
- Configuration: `appsettings.Development.json`
- URL: `http://localhost:5001`

---

### 🪟 Windows Server Production

**Full Guide:** [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md)  
**Quick Reference:** [WINDOWS-QUICKSTART.md](WINDOWS-QUICKSTART.md)

**Key Points:**
- Endatix runs as Windows Service
- Configuration: `appsettings.Production.json`
- Same server as 360Review app
- URL: `http://localhost:5001` (or HTTPS)

---

## Documentation Structure

```
Development (MacOS)
├── MACOS-DEVELOPMENT-SETUP.md     ← Complete setup guide
├── MACOS-QUICKSTART.md            ← Quick reference
├── setup-endatix-dev.sh           ← Automated setup script
└── appsettings.Development.json   ← Dev config

Production (Windows)
├── WINDOWS-SERVER-SETUP.md        ← Complete setup guide
├── WINDOWS-QUICKSTART.md          ← Quick reference
└── appsettings.Production.json    ← Prod config (create from .example)

General Documentation
├── ENDATIX-INTEGRATION-GUIDE.md   ← Architecture & integration details
├── ENDATIX-IMPLEMENTATION-SUMMARY.md ← What was built
└── CONFIGURATION-UPDATE.md         ← Recent changes
```

---

## Configuration Files by Environment

### Development (MacOS)
```json
// appsettings.Development.json
{
  "Endatix": {
    "ApiBaseUrl": "http://localhost:5001"
  },
  "Survey": {
    "DefaultTemplateId": "your-dev-template-id"
  }
}
```

### Production (Windows)
```json
// appsettings.Production.json
{
  "Endatix": {
    "ApiBaseUrl": "http://localhost:5001"  // or https
  },
  "Survey": {
    "DefaultTemplateId": "your-prod-template-id"
  }
}
```

---

## Common Tasks

### Start Development Environment

**MacOS:**
```bash
docker start endatix-dev      # Start Endatix
dotnet run                     # Start 360Review
```

**Windows:**
```powershell
Start-Service EndatixService   # Start Endatix
# Start 360Review via IIS or dotnet run
```

### Check Endatix Status

**MacOS:**
```bash
docker ps | grep endatix-dev
curl http://localhost:5001/api/health
```

**Windows:**
```powershell
Get-Service EndatixService
Invoke-WebRequest -Uri http://localhost:5001/api/health
```

### View Logs

**MacOS:**
```bash
docker logs -f endatix-dev
```

**Windows:**
```powershell
Get-Content C:\Services\Endatix\logs\stdout.log -Tail 50 -Wait
```

---

## Which Guide Should I Use?

**I'm developing on my Mac:**
→ Start with [MACOS-QUICKSTART.md](MACOS-QUICKSTART.md)

**I'm deploying to Windows Server:**
→ Start with [WINDOWS-QUICKSTART.md](WINDOWS-QUICKSTART.md)

**I want to understand the architecture:**
→ Read [ENDATIX-INTEGRATION-GUIDE.md](ENDATIX-INTEGRATION-GUIDE.md)

**I want to know what code was added:**
→ Read [ENDATIX-IMPLEMENTATION-SUMMARY.md](ENDATIX-IMPLEMENTATION-SUMMARY.md)

---

## Support

- **MacOS Issues**: See [MACOS-DEVELOPMENT-SETUP.md](MACOS-DEVELOPMENT-SETUP.md) Troubleshooting
- **Windows Issues**: See [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md) Troubleshooting
- **General Integration**: See [ENDATIX-INTEGRATION-GUIDE.md](ENDATIX-INTEGRATION-GUIDE.md)

---

**Last Updated**: March 3, 2026
