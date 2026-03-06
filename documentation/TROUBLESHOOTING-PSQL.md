# PostgreSQL / psql Not Found - Quick Fix

## Problem
The setup script can't find `psql` even though PostgreSQL is installed via Homebrew.

## Quick Fix Options

### Option 1: Run Script Again (Now Fixed)
The script has been updated to automatically find Homebrew's psql. Just run it again:

```bash
./setup-endatix-dev.sh
```

### Option 2: Add Homebrew to PATH

Add Homebrew's bin directory to your PATH before running the script:

**For Apple Silicon (M1/M2/M3):**
```bash
export PATH="/opt/homebrew/bin:$PATH"
./setup-endatix-dev.sh
```

**For Intel Mac:**
```bash
export PATH="/usr/local/bin:$PATH"
./setup-endatix-dev.sh
```

### Option 3: Make PATH Permanent

Add to your shell profile (`~/.zshrc` or `~/.bash_profile`):

**For Apple Silicon:**
```bash
echo 'export PATH="/opt/homebrew/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

**For Intel Mac:**
```bash
echo 'export PATH="/usr/local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### Option 4: Find Your PostgreSQL Installation

```bash
# Find where Homebrew installed PostgreSQL
brew --prefix postgresql

# Or for a specific version
brew --prefix postgresql@15

# Add it to PATH
export PATH="$(brew --prefix postgresql)/bin:$PATH"
```

### Option 5: Manual Setup (Skip Script)

If the script still doesn't work, follow the manual steps from [MACOS-DEVELOPMENT-SETUP.md](MACOS-DEVELOPMENT-SETUP.md):

#### Quick Manual Steps:

```bash
# 1. Find psql
which psql
# Or: find /opt/homebrew -name psql 2>/dev/null
# Or: find /usr/local -name psql 2>/dev/null

# 2. Use full path to psql
/full/path/to/psql -U $USER -d postgres -c "CREATE DATABASE endatix;"

# 3. Start Endatix with Docker
docker run -d --name endatix-dev -p 5001:80 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=endatix;Username=$USER;Password=YOUR_PASSWORD" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  endatix/endatix:latest

# 4. Upload template
curl -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"Leadership Competency Survey\",
    \"description\": \"360-degree feedback survey\",
    \"formDefinition\": $(cat survey3.json)
  }"

# 5. Update appsettings.Development.json with template ID
```

## Verify PostgreSQL Installation

```bash
# Check if PostgreSQL is installed
brew list | grep postgres

# Check if PostgreSQL service is running
brew services list | grep postgres

# Start PostgreSQL if needed
brew services start postgresql
# Or for specific version:
brew services start postgresql@15

# Test connection
psql -U $USER -d postgres -c "SELECT version();"
```

## Install PostgreSQL (If Needed)

```bash
# Install PostgreSQL
brew install postgresql@15

# Start PostgreSQL
brew services start postgresql@15

# Add to PATH
echo 'export PATH="/opt/homebrew/opt/postgresql@15/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

## Common Homebrew PostgreSQL Paths

The script now checks these locations automatically:

```
Apple Silicon:
- /opt/homebrew/bin/psql
- /opt/homebrew/opt/postgresql/bin/psql
- /opt/homebrew/opt/postgresql@15/bin/psql
- /opt/homebrew/opt/postgresql@14/bin/psql

Intel Mac:
- /usr/local/bin/psql
- /usr/local/opt/postgresql/bin/psql  
- /usr/local/opt/postgresql@15/bin/psql
- /usr/local/opt/postgresql@14/bin/psql
```

## Still Having Issues?

1. **Check your architecture:**
   ```bash
   uname -m
   # arm64 = Apple Silicon (use /opt/homebrew)
   # x86_64 = Intel (use /usr/local)
   ```

2. **List all PostgreSQL files:**
   ```bash
   brew list postgresql
   # Or: brew list postgresql@15
   ```

3. **Use absolute path in commands:**
   ```bash
   # Find psql
   find /opt/homebrew -name psql 2>/dev/null
   find /usr/local -name psql 2>/dev/null
   
   # Use the full path directly
   /opt/homebrew/bin/psql -U $USER -d postgres
   ```

4. **Check Homebrew itself:**
   ```bash
   # Verify Homebrew is working
   brew doctor
   
   # Check Homebrew location
   which brew
   ```

---

**After fixing PATH, run:**
```bash
./setup-endatix-dev.sh
```

Or follow the manual steps in [MACOS-DEVELOPMENT-SETUP.md](MACOS-DEVELOPMENT-SETUP.md).
