# Your MacOS Environment Setup - Specific Instructions

## What We Found

✅ **PostgreSQL 16** installed at: `/opt/homebrew/opt/postgresql@16/bin/psql`  
✅ **Endatix source** cloned to: `~/endatix-dev`  
✅ **Database created**: `endatix`  
⚠️ **PATH needs configuration** for psql and dotnet

## Quick Setup (Copy-Paste These Commands)

### Step 1: Configure PATH

Add both PostgreSQL and .NET to your PATH. Open a terminal and run:

```bash
# Add to current session
export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"
export PATH="/usr/local/share/dotnet:$PATH"

# Or find your dotnet installation
which dotnet || find /usr/local/share -name dotnet 2>/dev/null | head -1
```

### Step 2: Make PATH Permanent (Recommended)

Add these lines to `~/.zshrc`:

```bash
# Open your shell config
nano ~/.zshrc

# Add these lines at the end:
export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"
export PATH="/usr/local/share/dotnet:$PATH"

# Save (Ctrl+O, Enter, Ctrl+X) and reload:
source ~/.zshrc
```

### Step 3: Build and Run Endatix

```bash
# Navigate to Endatix
cd ~/endatix-dev

# Install EF tools if needed
dotnet tool install --global dotnet-ef

# Restore packages
dotnet restore src/Endatix.Api/Endatix.Api.csproj

# Build
dotnet build src/Endatix.Api/Endatix.Api.csproj

# Apply migrations
cd src/Endatix.Api
dotnet ef database update

# Run Endatix in background
dotnet run --urls "http://localhost:5001" &

# Save the process ID
echo $! > ~/endatix.pid
```

### Step 4: Verify Endatix is Running

```bash
# Wait a few seconds, then test
sleep 5
curl http://localhost:5001/api/health

# You should see a response indicating Endatix is running
```

### Step 5: Upload Survey Template

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Upload your survey
curl -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Leadership Competency Survey\",
    \"description\": \"360-degree feedback survey for leadership assessment\",
    \"formDefinition\": $(cat survey3.json)
  }"

# IMPORTANT: Copy the "id" from the response!
```

### Step 6: Update 360Review Configuration

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Edit the config
nano appsettings.Development.json

# Update the Survey:DefaultTemplateId with the ID from step 5
# Save and exit
```

### Step 7: Apply Migration and Test

```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Apply the migration
dotnet ef database update

# Start your app
dotnet run

# In another terminal, get a test password:
psql -U mblackmon -d reviews_mvc -c \
  "SELECT \"EvaluationParticipantSurveyPassword\" 
   FROM \"EvaluationParticipants\" LIMIT 1;"

# Visit: http://localhost:5000/survey/{password}
```

## Managing Endatix

### Start Endatix
```bash
cd ~/endatix-dev/src/Endatix.Api
dotnet run --urls "http://localhost:5001" &
echo $! > ~/endatix.pid
```

### Stop Endatix
```bash
kill $(cat ~/endatix.pid)
rm ~/endatix.pid
```

### Check if Endatix is Running
```bash
curl http://localhost:5001/api/health
# Or check the process:
ps aux | grep Endatix.Api
```

### View Endatix Logs
The logs will appear in the terminal where you started Endatix.

## Alternative: Use VS Code Terminal

If you prefer, you can:
1. Open a VS Code terminal
2. Run Endatix there (it will stay in the foreground)
3. Use another terminal for your 360Review app

```bash
# Terminal 1 (Endatix):
cd ~/endatix-dev/src/Endatix.Api
dotnet run --urls "http://localhost:5001"

# Terminal 2 (360Review):
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run
```

## Troubleshooting

### Can't find dotnet

```bash
# Find dotnet installation
find /usr/local -name dotnet 2>/dev/null
find /opt/homebrew -name dotnet 2>/dev/null

# Or install .NET SDK
brew install --cask dotnet-sdk
```

### Port 5001 Already in Use

```bash
# Find and kill the process
lsof -i :5001
kill -9 <PID>
```

### Database Connection Errors

Verify PostgreSQL is running:
```bash
brew services list | grep postgres
brew services start postgresql@16
```

### Migration Errors

```bash
cd ~/endatix-dev/src/Endatix.Api

# Reset and try again
dotnet ef database drop --force
dotnet ef database update
```

## Daily Workflow

### Starting Your Day

```bash
# 1. Make sure PATH is set (if not permanent)
export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"

# 2. Start Endatix
cd ~/endatix-dev/src/Endatix.Api
dotnet run --urls "http://localhost:5001" &

# 3. Start 360Review (in another terminal or VS Code)
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet run
```

### Ending Your Day

```bash
# Stop 360Review: Ctrl+C in terminal

# Stop Endatix
kill $(cat ~/endatix.pid) 2>/dev/null || pkill -f Endatix.Api
```

## Next Steps

Once everything is running:
1. ✅ Test the survey page
2. ✅ Verify autosave works (wait 3 seconds after typing)
3. ✅ Complete and submit a test survey
4. ✅ Check the submission in Endatix: `curl http://localhost:5001/api/submissions`

---

**Your Environment:**
- PostgreSQL: 16.12 (Homebrew)
- Endatix: ~/endatix-dev
- 360Review: /Users/mblackmon/Documents/vscode/360review_mvc
- Platform: Apple Silicon Mac

**Created**: March 3, 2026
