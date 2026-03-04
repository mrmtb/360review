# Endatix and SurveyJS Integration Guide

## Overview

This document provides comprehensive guidance for integrating Endatix (backend survey management) and SurveyJS (frontend survey rendering) into the 360review application.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [Endatix Setup](#endatix-setup)
4. [Configuration](#configuration)
5. [Database Migration](#database-migration)
6. [Testing the Integration](#testing-the-integration)
7. [Deployment Considerations](#deployment-considerations)
8. [Troubleshooting](#troubleshooting)
9. [Phase Implementation Plan](#phase-implementation-plan)

## Architecture Overview

### Components

- **SurveyJS (Frontend)**: Renders surveys in the browser, handles user interaction
- **Endatix (Backend)**: Stores survey templates and submissions via REST API
- **MVC Application**: Orchestrates the flow, manages access control, and maintains review cycle state

### Data Flow

```
Participant → /survey/{password} 
    → SurveyController 
    → EvaluationSurveyRuntimeService 
    → EndatixClient 
    → Endatix API
```

### Key Entities

- **Evaluation**: Review cycle/campaign
- **EvaluationRatee**: Person being reviewed
- **EvaluationParticipant**: Rater assignment (one rater reviewing one ratee)
  - New fields:
    - `EndatixSubmissionId`: Links to Endatix submission
    - `SurveyLastSavedAt`: Tracks last save time
    - `SurveyVersion`: Template version used
    - `SurveyTemplateKey`: Template identifier

## Prerequisites

### Required Software

1. **.NET 6.0 or later**
2. **PostgreSQL 12+** (already in use)
3. **Endatix** (choose one option):
   - Self-hosted via Docker
   - Self-hosted via .NET
   - Endatix SaaS (hosted service)

### NuGet Packages

No additional packages are required for the MVC app. The integration uses HttpClient to communicate with Endatix REST API.

## Endatix Setup (Windows Server - Self-Hosted)

### ✅ SELECTED APPROACH: Self-Hosted .NET Application on Windows Server

Since you're deploying on the same Windows server as your MVC app:

#### Step 1: Clone and Build Endatix

1. **Clone the Endatix repository**:
   ```powershell
   cd C:\inetpub
   git clone https://github.com/endatix/endatix.git
   cd endatix
   ```

2. **Build the Endatix API**:
   ```powershell
   dotnet build src/Endatix.Api/Endatix.Api.csproj --configuration Release
   ```

#### Step 2: Configure Database

**Option A: Shared PostgreSQL Instance (Recommended)**

Create a separate database for Endatix in your existing PostgreSQL:

```sql
-- Connect to PostgreSQL
CREATE DATABASE endatix;
GRANT ALL PRIVILEGES ON DATABASE endatix TO reviews_user;
```

Update `src/Endatix.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=endatix;Username=reviews_user;Password=360Admin!"
  },
  "Urls": "http://localhost:5001"
}
```

**Option B: Separate Endatix Database User**

```sql
CREATE USER endatix_user WITH PASSWORD 'EndatixPass123!';
CREATE DATABASE endatix OWNER endatix_user;
```

#### Step 3: Apply Endatix Migrations

```powershell
cd src/Endatix.Api
dotnet ef database update
```

#### Step 4: Test Run Endatix

```powershell
dotnet run --project src/Endatix.Api/Endatix.Api.csproj --urls "http://localhost:5001"
```

Verify it's running:
```powershell
Invoke-WebRequest -Uri http://localhost:5001/api/health
```

#### Step 5: Setup as Windows Service (Production)

1. **Publish Endatix**:
   ```powershell
   dotnet publish src/Endatix.Api/Endatix.Api.csproj -c Release -o C:\Services\Endatix
   ```

2. **Create Windows Service** using NSSM or sc.exe:
   ```powershell
   # Using sc.exe
   sc.exe create EndatixService binPath= "C:\Program Files\dotnet\dotnet.exe C:\Services\Endatix\Endatix.Api.dll"
   sc.exe start EndatixService
   ```

   Or download NSSM (Non-Sucking Service Manager):
   ```powershell
   nssm install EndatixService "C:\Program Files\dotnet\dotnet.exe" "C:\Services\Endatix\Endatix.Api.dll"
   nssm set EndatixService AppDirectory C:\Services\Endatix
   nssm start EndatixService
   ```

3. **Configure Firewall** (if needed):
   ```powershell
   New-NetFirewallRule -DisplayName "Endatix API" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow
   ```

### Alternative Options (Not Selected)

#### Option: Docker on Windows

If you prefer Docker:
```powershell
docker pull endatix/endatix:latest
docker run -d --name endatix -p 5001:80 `
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=endatix;Username=reviews_user;Password=360Admin!" `
  endatix/endatix:latest
```

#### Option: Endatix SaaS

Visit [https://endatix.com](https://endatix.com) to sign up for hosted Endatix.

## Configuration

### appsettings.json

The following configuration has been added to `appsettings.json`:

```json
{
  "Endatix": {
    "ApiBaseUrl": "https://localhost:5001",
    "ApiKey": ""
  },
  "Survey": {
    "DefaultTemplateId": "leadership-competency-v1",
    "TemplatePath": "survey3.json"
  }
}
```

**Configuration Options**:

- `Endatix:ApiBaseUrl`: Base URL for the Endatix API
- `Endatix:ApiKey`: API key for authentication (leave empty if not using auth)
- `Survey:DefaultTemplateId`: Default template ID to use
- `Survey:TemplatePath`: Path to the SurveyJS JSON template file

### Environment-Specific Configuration

For **production**, update `appsettings.Production.json`:

```json
{
  "Endatix": {
    "ApiBaseUrl": "https://endatix.yourdomain.com",
    "ApiKey": "your-production-api-key"
  }
}
```

## Database Migration

A new migration has been created: `20260303000000_AddEndatixFields.cs`

### Apply the Migration

Run the following command to apply the migration:

```bash
dotnet ef database update
```

Or, if using the built-in auto-migration in Program.cs, simply start the application and migrations will be applied automatically.

### Migration Details

The migration adds the following columns to `EvaluationParticipants`:

- `EndatixSubmissionId` (text, indexed)
- `SurveyLastSavedAt` (timestamptz)
- `SurveyVersion` (text)
- `SurveyTemplateKey` (text)

## Testing the Integration

### Step 1: Create Survey Template in Endatix

**TODO: This requires manual setup in Endatix or via API**

You need to upload the `survey3.json` template to Endatix and note the returned template ID.

Example using curl:

```bash
curl -X POST http://localhost:5001/api/forms \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Leadership Competency Survey",
    "description": "360-degree feedback survey",
    "formDefinition": "'"$(cat survey3.json | jq -c .)"'"
  }'
```

**Important**: Update the `Survey:DefaultTemplateId` in appsettings.json with the returned template ID.

### Step 2: Test Survey Access

1. **Get an existing survey password** from the database:
   ```sql
   SELECT "EvaluationParticipantSurveyPassword" 
   FROM "EvaluationParticipants" 
   LIMIT 1;
   ```

2. **Navigate to the survey URL**:
   ```
   http://localhost:5000/survey/{password}
   ```

3. **Verify the survey renders** with SurveyJS

4. **Test autosave**: Fill out some fields and wait 3 seconds. Check browser console for "Progress saved automatically".

5. **Test final submission**: Complete the survey and click "Complete".

### Step 3: Verify Data in Endatix

Check that the submission was created in Endatix:

```bash
curl http://localhost:5001/api/submissions
```

## Deployment Considerations

### Production Checklist

- [ ] **Endatix is deployed** and accessible from the MVC app
- [ ] **Endatix API URL** is configured in `appsettings.Production.json`
- [ ] **Database migration** has been applied
- [ ] **Survey template** has been uploaded to Endatix
- [ ] **SSL/TLS** is configured for Endatix API
- [ ] **API authentication** is configured (if using Endatix auth)
- [ ] **Firewall rules** allow MVC app to access Endatix
- [ ] **Monitoring** is configured for Endatix API health

### Hosting Options

#### ✅ SELECTED: Same Windows Server (Self-Hosted)
- MVC app and Endatix on the same Windows server
- Both run as .NET applications (or Endatix as Windows Service)
- Can share PostgreSQL instance (different databases) or separate databases
- Simpler ops and lower latency
- Different ports (e.g., MVC on 5000, Endatix on 5001)

#### Alternative: Separate Servers
- MVC app on one server
- Endatix on another server
- Separate databases (better security isolation)
- Higher latency for API calls

#### Alternative: Containerized
- Both apps in Docker containers
- Use Docker Compose
- Would require Docker Desktop on Windows Server

### Security Considerations

1. **Survey Passwords**: 
   - Current implementation uses `EvaluationParticipantSurveyPassword`
   - These should be long, random, unguessable tokens
   - Consider adding expiration dates

2. **API Authentication**:
   - Configure Endatix API key if using authentication
   - Use HTTPS for all API calls

3. **Data Privacy**:
   - Endatix stores submission data
   - Ensure Endatix database is properly secured
   - Consider encryption at rest

4. **CORS** (if SurveyJS posts directly to Endatix):
   - Currently not needed (MVC app is intermediary)
   - If implementing direct browser → Endatix, configure CORS

## Troubleshooting

### Survey Page Returns 404

**Cause**: Survey password not found in database

**Solution**: Verify the password exists:
```sql
SELECT * FROM "EvaluationParticipants" 
WHERE "EvaluationParticipantSurveyPassword" = 'your-password';
```

### "Endatix:ApiBaseUrl not configured" Error

**Cause**: Missing or incorrect appsettings.json configuration

**Solution**: Ensure `Endatix:ApiBaseUrl` is set in appsettings.json

### Survey Template Not Loading

**Cause**: Template file not found or invalid JSON

**Solution**: 
1. Verify `survey3.json` exists in project root
2. Validate JSON syntax
3. Check `Survey:TemplatePath` configuration

### Autosave Failing

**Cause**: Network error or Endatix unreachable

**Solution**: 
1. Check browser console for error messages
2. Verify Endatix is running: `curl http://localhost:5001/api/health`
3. Check MVC app logs for EndatixClient errors

### Submission Not Created in Endatix

**Cause**: API call failing, wrong URL, or authentication issue

**Solution**:
1. Check MVC app logs for exceptions
2. Verify Endatix URL and credentials
3. Test Endatix API directly with curl

## Phase Implementation Plan

### Phase 0: Spike (COMPLETED)
✅ Interfaces and services created
✅ Database migration created
✅ Controller and views created
✅ Configuration added

### Phase 1: Happy Path Runtime (IN PROGRESS)

**Remaining Tasks**:
- [ ] Setup Endatix (Docker or self-hosted)
- [ ] Upload survey template to Endatix
- [ ] Configure Endatix URL in appsettings
- [ ] Test end-to-end flow with a real participant
- [ ] Verify final submission works

**Estimated Time**: 2-4 hours

### Phase 2: Autosave + Resume (READY TO TEST)

**Features Implemented**:
✅ Draft save endpoint
✅ Frontend autosave (3-second debounce)
✅ Save on page change
✅ Resume from existing data

**Testing Tasks**:
- [ ] Test autosave during survey completion
- [ ] Test resume after browser close/reopen
- [ ] Test page navigation saves
- [ ] Verify status transitions (Pending → InProgress)

**Estimated Time**: 1-2 hours testing

### Phase 3: Multi-Template + Versioning (TODO - REQUIRES DESIGN DECISIONS)

**Overview**: Support different survey templates per evaluation or per rater role

**Design Decisions Needed**:
1. **Template Storage**: 
   - Store templates in Endatix only?
   - Or cache locally in MVC app?

2. **Template Selection Logic**:
   - One template per evaluation?
   - Different templates per role (Manager, Peer, Direct Report)?
   - Template versioning strategy?

3. **Database Schema**:
   - Add `SurveyTemplateId` to `Evaluation` table?
   - Create a `SurveyTemplateMapping` table?

**Estimated Tasks**:
- Design template selection logic
- Update `SurveyTemplateResolver` to query database
- Add admin UI for template management
- Testing

**Estimated Time**: 4-6 hours

## API Reference

### Endatix API Endpoints (Used by Integration)

#### Create Submission
```http
POST /api/submissions
Content-Type: application/json

{
  "formId": "template-id",
  "metadata": {
    "evaluationId": "...",
    "participantId": "..."
  },
  "isComplete": false
}
```

#### Get Submission
```http
GET /api/submissions/{submissionId}
```

#### Update Submission (Draft or Final)
```http
PUT /api/submissions/{submissionId}
Content-Type: application/json

{
  "jsonData": "{...survey data...}",
  "isComplete": false
}
```

#### Get Template
```http
GET /api/forms/{templateId}
```

### MVC Application Endpoints

#### Render Survey
```http
GET /survey/{password}
```

#### Get Runtime Data (JSON)
```http
GET /survey/api/{password}/runtime
```

#### Save Draft
```http
POST /survey/api/{password}/draft
Content-Type: application/json

{
  "data": "{...survey data...}",
  "pageNo": 2
}
```

#### Submit Final
```http
POST /survey/api/{password}/submit
Content-Type: application/json

{
  "data": "{...survey data...}"
}
```

## Support and Resources

- **Endatix Documentation**: https://docs.endatix.com
- **SurveyJS Documentation**: https://surveyjs.io/documentation
- **Endatix GitHub**: https://github.com/endatix/endatix
- **SurveyJS GitHub**: https://github.com/surveyjs/survey-library

## Future Enhancements

### Potential Features

1. **Offline Support**: Allow surveys to be completed offline with sync later
2. **Progress Indicators**: Show completion percentage
3. **Skip Logic**: Conditional questions based on answers
4. **Multi-Language**: Support for multiple languages
5. **Custom Themes**: Branded survey appearance
6. **Analytics Dashboard**: Real-time completion tracking
7. **Reminder System**: Integration with existing email reminders
8. **Mobile Optimization**: Enhanced mobile experience

### Integration Points

1. **Reporting**: Connect Endatix submissions to existing reporting pipeline
2. **Admin UI**: Add template management to admin area
3. **Email Templates**: Update invitation emails with new survey URLs
4. **Status Dashboard**: Show real-time survey completion stats

## Appendix A: Service Architecture

### IEndatixClient
- HTTP client wrapper for Endatix API
- Handles submission CRUD operations
- Template retrieval

### ISurveyTemplateResolver
- Resolves which template to use for a given evaluation/role
- Currently returns hardcoded template
- Phase 3: Database-driven template selection

### IEvaluationSurveyRuntimeService
- Main orchestration service
- Coordinates between EF entities, Endatix, and templates
- Manages participant state and submissions

### SurveyController
- Handles HTTP requests
- Routes: `/survey/{password}`, `/survey/api/{password}/draft`, `/survey/api/{password}/submit`

## Appendix B: Status IDs

**Note**: These status IDs are assumptions based on typical workflows. Verify these values in your database:

```sql
SELECT * FROM "EvaluationParticipantStatus" ORDER BY "EvaluationParticipantStatusId";
```

Assumed values:
- `1` = Pending (invitation sent)
- `2` = InProgress (draft exists)
- `3` = Completed (survey submitted)

Update the hardcoded values in `EvaluationSurveyRuntimeService.cs` if different.

## Appendix C: Migration Rollback

If you need to rollback the migration:

```bash
dotnet ef database update 20260123015126_ConsolidateDbContexts
```

Or manually in PostgreSQL:

```sql
DROP INDEX "IX_EvaluationParticipants_EndatixSubmissionId";

ALTER TABLE "EvaluationParticipants"
DROP COLUMN "EndatixSubmissionId",
DROP COLUMN "SurveyLastSavedAt",
DROP COLUMN "SurveyVersion",
DROP COLUMN "SurveyTemplateKey";
```

---

**Last Updated**: March 3, 2026
**Version**: 1.0
**Status**: Phase 1 Implementation
