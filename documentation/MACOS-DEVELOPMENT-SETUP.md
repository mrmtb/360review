# MacOS Development Setup - Endatix + 360Review

## Development Environment

This guide is for setting up Endatix on **MacOS** for local development and testing. Production deployment to Windows Server uses the separate [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md) guide.

## Prerequisites

- ✅ MacOS (you're here!)
- ✅ .NET 6.0+ SDK installed
- ✅ PostgreSQL installed (already configured for 360Review)
- ✅ Docker Desktop (recommended) OR .NET for running Endatix natively

## Quick Setup Options

Choose one of these approaches:

### Option A: Docker (Recommended - Fastest)
- Easiest setup
- Isolated environment
- Closest to production

### Option B: Native .NET (More control)
- No Docker required
- Easier debugging
- Direct access to code

---

## Option A: Docker Setup (Recommended)

### Step 1: Install Docker Desktop

If not already installed:
```bash
# Download from: https://www.docker.com/products/docker-desktop
# Or install via Homebrew:
brew install --cask docker
```

Start Docker Desktop and wait for it to fully start.

### Step 2: Create Endatix Database

```bash
# Connect to PostgreSQL
psql -U reviews_user -d postgres

# Create database
CREATE DATABASE endatix;
GRANT ALL PRIVILEGES ON DATABASE endatix TO reviews_user;
\q
```

### Step 3: Run Endatix with Docker

```bash
# Pull the latest Endatix image
docker pull endatix/endatix:latest

# Run Endatix container
docker run -d \
  --name endatix-dev \
  -p 5001:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=endatix;Username=reviews_user;Password=YOUR_STRONG_PASSWORD" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  endatix/endatix:latest
```

**Note**: `host.docker.internal` allows the Docker container to connect to PostgreSQL running on your Mac.

### Step 4: Verify Endatix is Running

```bash
# Check container status
docker ps | grep endatix-dev

# Test the health endpoint
curl http://localhost:5001/api/health

# View logs
docker logs endatix-dev
```

### Step 5: Docker Management Commands

```bash
# Start container
docker start endatix-dev

# Stop container
docker stop endatix-dev

# Restart container
docker restart endatix-dev

# View logs (follow)
docker logs -f endatix-dev

# Remove container (if needed)
docker stop endatix-dev
docker rm endatix-dev
```

---

## Option B: Native .NET Setup

### Step 1: Clone Endatix Repository

```bash
cd ~/Development  # or your preferred location
git clone https://github.com/endatix/endatix.git
cd endatix
```

### Step 2: Create Endatix Database

```bash
psql -U reviews_user -d postgres
```

```sql
CREATE DATABASE endatix;
GRANT ALL PRIVILEGES ON DATABASE endatix TO reviews_user;
\q
```

### Step 3: Configure Endatix

Edit the configuration file:

```bash
nano src/Endatix.Api/appsettings.Development.json
```

Add this content:

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
  }
}
```

### Step 4: Install EF Tools (if not already installed)

```bash
dotnet tool install --global dotnet-ef
```

### Step 5: Build and Run Migrations

```bash
# Restore dependencies
dotnet restore src/Endatix.Api/Endatix.Api.csproj

# Build
dotnet build src/Endatix.Api/Endatix.Api.csproj

# Apply migrations
cd src/Endatix.Api
dotnet ef database update

# Run Endatix
dotnet run --urls "http://localhost:5001"
```

### Step 6: Verify Endatix

In another terminal:

```bash
curl http://localhost:5001/api/health
```

---

## Upload Survey Template to Endatix

Once Endatix is running (either via Docker or native), upload your survey template:

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Upload the template
curl -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Leadership Competency Survey\",
    \"description\": \"360-degree feedback survey for leadership assessment\",
    \"formDefinition\": $(cat survey3.json)
  }"
```

**Important**: Save the `id` from the response! You'll need it in the next step.

Example response:
```json
{
  "id": "abc123-def456-ghi789",
  "name": "Leadership Competency Survey",
  ...
}
```

---

## Update 360Review Configuration

### Step 1: Update appsettings.Development.json

Create or update the Development configuration:

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc
nano appsettings.Development.json
```

Add or update these sections:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Endatix": {
    "ApiBaseUrl": "http://localhost:5001",
    "ApiKey": ""
  },
  "Survey": {
    "DefaultTemplateId": "PUT_YOUR_TEMPLATE_ID_HERE",
    "TemplatePath": "survey3.json"
  }
}
```

Replace `PUT_YOUR_TEMPLATE_ID_HERE` with the ID from the previous step.

### Step 2: Apply 360Review Migration

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Apply the migration (or it will auto-apply on startup)
dotnet ef database update
```

---

## Test the Integration

### Step 1: Start Your 360Review App

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run
```

Or use VS Code's debug runner.

### Step 2: Get a Test Survey Password

```bash
psql -U reviews_user -d reviews_mvc -c \
  "SELECT \"EvaluationParticipantSurveyPassword\" 
   FROM \"EvaluationParticipants\" 
   LIMIT 1;"
```

Copy the password from the result.

### Step 3: Test the Survey

Navigate to:
```
http://localhost:5000/survey/{paste-password-here}
```

### Step 4: Verification Checklist

- [ ] Survey page loads without errors
- [ ] SurveyJS renders the Leadership Competency Survey
- [ ] You can fill out questions
- [ ] Browser console shows "Progress saved automatically" after 3 seconds
- [ ] You can complete and submit the survey
- [ ] Check Endatix has the submission:

```bash
curl http://localhost:5001/api/submissions | jq .
```

---

## Development Workflow

### Starting Your Dev Environment

```bash
# Terminal 1: Start Endatix (if using native .NET)
cd ~/Development/endatix/src/Endatix.Api
dotnet run --urls "http://localhost:5001"

# Or if using Docker:
docker start endatix-dev

# Terminal 2: Start 360Review
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run

# Or use VS Code debugger
```

### Stopping Your Dev Environment

```bash
# Stop 360Review: Ctrl+C in terminal or stop debugger

# Stop Endatix Docker:
docker stop endatix-dev

# Or stop native Endatix: Ctrl+C
```

### Viewing Logs

```bash
# Endatix Docker logs
docker logs -f endatix-dev

# Endatix native logs (stdout)

# 360Review logs (in your terminal)
```

---

## Troubleshooting

### Port Already in Use

```bash
# Check what's using port 5001
lsof -i :5001

# Kill the process if needed
kill -9 <PID>
```

### Docker Container Won't Start

```bash
# Check logs
docker logs endatix-dev

# Remove and recreate
docker stop endatix-dev
docker rm endatix-dev
# Then re-run the docker run command
```

### Database Connection Issues

```bash
# Test PostgreSQL connection
psql -U reviews_user -d endatix

# Verify PostgreSQL is running
pg_isready -h localhost -p 5432
```

### Survey Template Upload Failed

```bash
# Verify Endatix is running
curl http://localhost:5001/api/health

# Check survey3.json is valid JSON
cat survey3.json | jq .

# Try uploading with verbose output
curl -v -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d @survey3.json
```

### 360Review Can't Connect to Endatix

1. Verify Endatix is running: `curl http://localhost:5001/api/health`
2. Check `appsettings.Development.json` has correct URL
3. Check firewall isn't blocking localhost connections
4. Review 360Review logs for connection errors

---

## VS Code Configuration

### Recommended launch.json for Debugging

Create `.vscode/launch.json` if you want to debug:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (360Review)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net6.0/reviews.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

### Recommended tasks.json

Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/360review.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

---

## Environment Comparison

| Aspect | MacOS Dev | Windows Production |
|--------|-----------|-------------------|
| Endatix | Docker or Native .NET | Windows Service |
| Database | Local PostgreSQL | Server PostgreSQL |
| URL | http://localhost:5001 | http://localhost:5001 |
| Config | appsettings.Development.json | appsettings.Production.json |
| Port | 5001 | 5001 |

---

## Useful Commands Quick Reference

```bash
# PostgreSQL
psql -U reviews_user -d endatix
psql -U reviews_user -d reviews_mvc

# Docker Endatix
docker start endatix-dev
docker stop endatix-dev
docker logs -f endatix-dev
docker restart endatix-dev

# Test Endatix
curl http://localhost:5001/api/health
curl http://localhost:5001/api/forms
curl http://localhost:5001/api/submissions

# 360Review
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run
dotnet watch run  # Auto-reload on changes
dotnet ef database update

# Check ports
lsof -i :5000  # 360Review
lsof -i :5001  # Endatix
lsof -i :5432  # PostgreSQL
```

---

## Next Steps

1. ✅ Choose Docker or Native .NET approach
2. ✅ Setup Endatix following steps above
3. ✅ Upload survey template
4. ✅ Update appsettings.Development.json with template ID
5. ✅ Test the integration
6. ✅ Start developing!

---

## Getting Help

- **MacOS Issues**: Check this guide's Troubleshooting section
- **Endatix Docs**: https://docs.endatix.com
- **Docker Issues**: `docker logs endatix-dev`
- **Integration Issues**: See [ENDATIX-INTEGRATION-GUIDE.md](ENDATIX-INTEGRATION-GUIDE.md)

---

**Last Updated**: March 3, 2026
**Platform**: MacOS (Development)
**Production**: See [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md)
