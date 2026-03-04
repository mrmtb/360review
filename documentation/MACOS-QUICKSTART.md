# MacOS Development Quick Reference

## 🚀 Super Quick Start

```bash
# 1. Run the automated setup script
./setup-endatix-dev.sh

# 2. Copy the template ID from output and update config
nano appsettings.Development.json
# Set Survey:DefaultTemplateId to the template ID

# 3. Start your app
dotnet run

# 4. Test: http://localhost:5000/survey/{password}
```

---

## 📦 Option A: Docker Setup (Recommended)

### Install & Run

```bash
# Install Docker Desktop from https://docker.com or:
brew install --cask docker

# Create database
psql -U $USER -d postgres -c "CREATE DATABASE endatix;"

# Run Endatix
docker run -d --name endatix-dev -p 5001:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=endatix;Username=$USER;Password=YOUR_PG_PASSWORD" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  endatix/endatix:latest
```

### Manage Container

```bash
docker start endatix-dev      # Start
docker stop endatix-dev       # Stop
docker restart endatix-dev    # Restart
docker logs -f endatix-dev    # View logs
```

---

## 🔧 Option B: Native .NET Setup

```bash
# Clone Endatix
cd ~/Development
git clone https://github.com/endatix/endatix.git
cd endatix

# Create database
psql -U $USER -d postgres -c "CREATE DATABASE endatix;"

# Configure (edit src/Endatix.Api/appsettings.Development.json)

# Run migrations
cd src/Endatix.Api
dotnet ef database update

# Start Endatix
dotnet run --urls "http://localhost:5001"
```

---

## 📤 Upload Survey Template

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Upload template
curl -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Leadership Competency Survey\",
    \"description\": \"360-degree feedback survey\",
    \"formDefinition\": $(cat survey3.json)
  }"

# Save the "id" from response!
```

---

## ⚙️ Configure 360Review

Edit `appsettings.Development.json`:

```json
{
  "Endatix": {
    "ApiBaseUrl": "http://localhost:5001"
  },
  "Survey": {
    "DefaultTemplateId": "PASTE_TEMPLATE_ID_HERE"
  }
}
```

---

## 🧪 Test Everything

```bash
# 1. Verify Endatix
curl http://localhost:5001/api/health

# 2. Get test password
psql -U $USER -d reviews_mvc -c \
  "SELECT \"EvaluationParticipantSurveyPassword\" 
   FROM \"EvaluationParticipants\" LIMIT 1;"

# 3. Start 360Review
dotnet run

# 4. Open browser: http://localhost:5000/survey/{password}
```

---

## 🔍 Troubleshooting

```bash
# Check if port is in use
lsof -i :5001

# Kill process on port
kill -9 $(lsof -t -i:5001)

# View Endatix logs (Docker)
docker logs -f endatix-dev

# Test PostgreSQL
psql -U $USER -d endatix
pg_isready -h localhost -p 5432

# Check Docker status
docker ps | grep endatix
```

---

## 🛠️ Daily Development

### Start Development Environment

```bash
# Terminal 1: Start Endatix (Docker)
docker start endatix-dev

# Or native .NET:
cd ~/Development/endatix/src/Endatix.Api
dotnet run --urls "http://localhost:5001"

# Terminal 2: Start 360Review
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run
# Or use VS Code debugger (F5)
```

### Stop Development Environment

```bash
# Stop 360Review: Ctrl+C or stop debugger

# Stop Endatix (Docker)
docker stop endatix-dev

# Or native: Ctrl+C
```

---

## 📊 Useful Commands

### Endatix API

```bash
# Health check
curl http://localhost:5001/api/health

# List forms/templates
curl http://localhost:5001/api/forms | jq .

# List submissions
curl http://localhost:5001/api/submissions | jq .

# Get specific submission
curl http://localhost:5001/api/submissions/{id} | jq .
```

### Database

```bash
# Connect to databases
psql -U $USER -d endatix
psql -U $USER -d reviews_mvc

# List databases
psql -U $USER -d postgres -c "\l"

# Check table
psql -U $USER -d reviews_mvc -c \
  "SELECT * FROM \"EvaluationParticipants\" LIMIT 5;"
```

### 360Review

```bash
# Run
dotnet run

# Run with auto-reload
dotnet watch run

# Apply migrations
dotnet ef database update

# Build
dotnet build

# Clean build
dotnet clean && dotnet build
```

---

## 🔗 Important URLs

| Service | URL |
|---------|-----|
| Endatix API | http://localhost:5001 |
| Endatix Health | http://localhost:5001/api/health |
| 360Review | http://localhost:5000 |
| Survey Test | http://localhost:5000/survey/{password} |

---

## 📁 Key Files

| File | Purpose |
|------|---------|
| `appsettings.Development.json` | Dev configuration |
| `survey3.json` | Survey template |
| `setup-endatix-dev.sh` | Automated setup |
| `MACOS-DEVELOPMENT-SETUP.md` | Full guide |

---

## 🆘 Quick Help

| Issue | Solution |
|-------|----------|
| Port 5001 busy | `lsof -i :5001` then `kill -9 <PID>` |
| Docker won't start | `docker logs endatix-dev` |
| DB connection error | Check username/password in config |
| Survey not loading | Verify Endatix health endpoint |
| Template upload fails | Check survey3.json is valid JSON |

---

## 📚 Full Documentation

- **Complete Setup**: [MACOS-DEVELOPMENT-SETUP.md](MACOS-DEVELOPMENT-SETUP.md)
- **Integration Guide**: [ENDATIX-INTEGRATION-GUIDE.md](ENDATIX-INTEGRATION-GUIDE.md)
- **Windows Production**: [WINDOWS-SERVER-SETUP.md](WINDOWS-SERVER-SETUP.md)

---

**Platform**: MacOS (Development only)
**Production**: Windows Server (see other docs)
**Last Updated**: March 3, 2026
