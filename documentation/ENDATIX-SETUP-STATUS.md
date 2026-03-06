# Endatix Integration - Setup Status

## Date: March 3, 2026

## ✅ COMPLETED

### 1. 360Review Application Code (100%)
All integration code for 360Review application is complete and ready:

- **Models** (✓)
  - Added 4 new fields to `EvaluationParticipant.cs`:
    - `EndatixSubmissionId`
    - `SurveyLastSavedAt`
    - `SurveyVersion`
    - `SurveyTemplateKey`
  - Created EF migration: `20260303000000_AddEndatixFields.cs`

- **Services** (✓)
  - `IEndatixClient` + `EndatixClient.cs` - HTTP client for Endatix API
  - `ISurveyTemplateResolver` + `SurveyTemplateResolver.cs` - Loads survey3.json
  - `IEvaluationSurveyRuntimeService` + `EvaluationSurveyRuntimeService.cs` - Main orchestration

- **Controllers** (✓)
  - `SurveyController.cs` with 4 endpoints:
    - `GET /survey/{password}` - Display survey page
    - `POST /survey/api/{password}/draft` - Autosave draft  
    - `POST /survey/api/{password}/submit` - Submit final
    - `GET /survey/api/{password}/runtime` - Get runtime data

- **Views** (✓)
  - `Views/Survey/Respond.cshtml` - SurveyJS integration with 3-second autosave

- **Configuration** (✓)
  - `Program.cs` - All services registered in DI
  - `appsettings.json` - Endatix and Survey sections added
  - `appsettings.Development.json` - MacOS configuration

- **Documentation** (✓)
  - 13 comprehensive markdown files covering setup, deployment, troubleshooting
  - Windows Server deployment guide
  - MacOS development setup guide

### 2. Environment Setup (Partial)
- ✅ .NET SDK 10.0.103 installed via Homebrew
- ✅ PostgreSQL 16.12 installed via Homebrew
- ✅ Endatix source code cloned to `~/endatix-dev`
- ✅ Database `endatix` created
- ✅ Database schemas created (`endatix`, `identity`)

## ⚠️ BLOCKERS

### Endatix Database Migrations
**Issue**: Automatic migrations not working despite configuration

**Details**:
- Endatix configured with `EnableAutoMigrations: true` in appsettings
- Application starts successfully on port 5001
- However, database tables are not created automatically
- Manual EF migrations attempts failed due to:
  - Missing design-time DbContext factory
  - Complex dependency injection requirements for PostgreSQL configuration
  - Service resolution errors when configuring PostgreSQL manually

**What Was Attempted**:
1. Enabled auto-migrations in appsettings.json ❌
2. Called `.EnableAutoMigrations(true)` in Program.cs ❌
3. Tried to manually apply migrations with `dotnet ef database update` ❌
4. Attempted to generate SQL scripts from migrations ❌
5. Tried calling `app.Services.ApplyDbMigrationsAsync()` explicitly ❌

**Root Cause**:
- Endatix's PostgreSQL persistence configuration requires additional infrastructure services
- Simply calling `UsePostgreSql<>()` without `UseDefaults()` on other builders causes missing service registrations
- The default `ConfigureEndatix()` uses SQL Server, not PostgreSQL
- MacOS doesn't easily run SQL Server (requires Docker)

## 📋 NEXT STEPS

### Option 1: PostgreSQL with Docker (Recommended for MacOS Dev)
Instead of building Endatix from source, use their official Docker setup:

```bash
# Check if Docker image exists
docker pull endatix/endatix:latest

# Or check Endatix documentation for Docker Compose setup
```

### Option 2: Windows Development Machine
-Move Endatix development to a Windows machine where SQL Server LocalDB or full SQL Server is available
- The default Endatix configuration works out-of-the-box on Windows

### Option 3: Community Support
Post an issue on Endatix GitHub repo:
```
Repository: https://github.com/endatix/endatix
Issue Title: "PostgreSQL auto-migrations not working on MacOS"
Include: 
- Configuration from Program.cs and appsettings.Development.json
- Logs showing "Automatic database migrations are disabled"
- Steps to reproduce
```

### Option 4: Manual Schema Creation
1. Find working Endatix PostgreSQL deployment
2. Export its database schema with `pg_dump`
3. Apply schema to local `endatix` database
4. Proceed with testing

## 🔄 TO RESUME WORK

Once Endatix database is properly set up:

1. **Upload Survey Template**:
```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc

# Get auth token first
RESPONSE=$(curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"YOUR_STRONG_PASSWORD","confirmPassword":"YOUR_STRONG_PASSWORD"}')

TOKEN=$(echo $RESPONSE | jq -r '.accessToken')

# Upload survey3.json
curl -X POST http://localhost:5001/api/forms \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @survey3.json
```

2. **Update Configuration**:
Replace `REPLACE_AFTER_UPLOADING_TEMPLATE` in `appsettings.Development.json` with the actual template ID from step 1

3. **Apply 360Review Migration**:
```bash
cd /Users/mblackmon/Documents/vscode/360review_mvc
dotnet ef database update
```

4. **Test the Integration**:
```bash
# Get a test password from your database
export PATH="/opt/homebrew/opt/postgresql@16/bin:$PATH"
psql -U mblackmon -d reviews_mvc -c "SELECT password FROM \"EvaluationParticipants\" LIMIT 1;"

# Navigate to survey page
http://localhost:5000/survey/{password}
```

5. **Verify Autosave**:
- Open browser console
- Fill out survey questions
- Wait 3 seconds
- Check for "Progress saved automatically" message

## 📁 KEY FILES

### In 360Review (`/Users/mblackmon/Documents/vscode/360review_mvc/`)
- `Controllers/SurveyController.cs` - Survey endpoints
- `Services/EndatixClient.cs` - Endatix API integration
- `Services/EvaluationSurveyRuntimeService.cs` - Main service
- `Views/Survey/Respond.cshtml` - Survey page
- `survey3.json` - Survey template to upload
- `appsettings.Development.json` - Configuration

### In Endatix (`~/endatix-dev/`)
- `samples/Endatix.Samples.SelfHosted/Program.cs` - Entry point
- `samples/Endatix.Samples.SelfHosted/appsettings.Development.json` - Config
- `src/Endatix.Persistence.PostgreSql/Migrations/` - Migration files

### Database Details
- Database: `endatix`
- User: `mblackmon`
- Password: `YOUR_DB_PASSWORD` (store in environment variables or a secret store; do not commit real credentials)
- Port: 5432
- Schemas: `endatix`, `identity`

## 📚 DOCUMENTATION CREATED

1. `MACOS-ENDATIX-SETUP.md` - MacOS setup instructions
2. `WINDOWS-SERVER-SETUP.md` - Windows deployment guide
3. `ENDATIX-INTEGRATION-OVERVIEW.md` - Integration architecture
4. `TROUBLESHOOTING-PSQL.md` - PATH configuration for psql
5. `YOUR-SETUP-INSTRUCTIONS.md` - Step-by-step commands
6. Multiple other guides...

## 💡 RECOMMENDATIONS

1. **For Development**: Consider using Endatix's Docker image or moving to Windows
2. **For Production**: Windows Server deployment is well-documented and ready
3. **Testing**: All 360Review code is ready - just needs working Endatix instance
4. **Alternative**: If Endatix proves too complex, consider direct SurveyJS integration without backend

## ✉️ SUPPORT RESOURCES

- Endatix Documentation: https://docs.endatix.com
- Endatix GitHub: https://github.com/endatix/endatix
- Endatix Community: Check repo for Discord/Slack links
- SurveyJS Documentation: https://surveyjs.io/form-library/documentation/overview

---

**Status**: Ready for testing once Endatix database is initialized  
**Estimated Time to Complete**: 1-2 hours (depending on chosen approach)  
**Risk Level**: Medium (requires Endatix expertise or community help)
