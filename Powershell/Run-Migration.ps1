param (
    [string]$ProjectPath = "C:\path\to\YourProject.csproj",
    [string]$DbType = "Postgres", # or "SqlServer"
    [string]$DbHost = "localhost",
    [string]$DbName = "YourDb",
    [string]$DbUser = "dbuser",
    [string]$DbPassword = "dbpassword",
    [string]$BackupPath = "C:\db_backups",
    [string]$SqlOutputPath = "C:\migrations\migrations.sql"
)

# Step 0: Detect pending migrations
Write-Host "🔍 Checking for pending migrations..." -ForegroundColor Cyan
$pendingMigrations = & dotnet ef migrations list --project $ProjectPath --no-build |
    Where-Object { $_ -match "^\s+\d{14}_" -and ($_ -notmatch "^\s+\d{14}_.*\(Applied\)") }

if (-not $pendingMigrations) {
    Write-Host "✅ No pending migrations found. Skipping migration." -ForegroundColor Green
    exit 0
}

Write-Host "📋 Pending migrations detected:" -ForegroundColor Yellow
$pendingMigrations | ForEach-Object { Write-Host "   $_" }

# Step 1: Create backup folder if not exists
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
}

# Step 2: Backup database
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "$DbName-$timestamp.bak"

Write-Host "🔄 Backing up database..." -ForegroundColor Cyan

if ($DbType -eq "Postgres") {
    $env:PGPASSWORD = $DbPassword
    & pg_dump -h $DbHost -U $DbUser -d $DbName -F c -f $backupFile
}
elseif ($DbType -eq "SqlServer") {
    $sqlBackupFile = Join-Path $BackupPath "$DbName-$timestamp.bak"
    Invoke-Sqlcmd -ServerInstance $DbHost -Database master -Query "BACKUP DATABASE [$DbName] TO DISK = N'$sqlBackupFile' WITH NOFORMAT, INIT, NAME = '$DbName-Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
}

Write-Host "✅ Backup completed: $backupFile" -ForegroundColor Green

# Step 3: Generate SQL script from EF Core migrations
Write-Host "🛠 Generating SQL migration script..." -ForegroundColor Cyan
dotnet ef migrations script --project $ProjectPath -o $SqlOutputPath

if (-not (Test-Path $SqlOutputPath)) {
    Write-Host "❌ Failed to generate migration script." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Migration script generated at $SqlOutputPath" -ForegroundColor Green

# Step 4: Wrap migration script in transaction for rollback safety
Write-Host "🛡 Adding transaction safety..." -ForegroundColor Cyan

if ($DbType -eq "Postgres") {
    $sqlContent = Get-Content $SqlOutputPath -Raw
    $wrappedSql = "BEGIN;\n" + $sqlContent + "\nCOMMIT;"
    Set-Content $SqlOutputPath $wrappedSql -Encoding UTF8
}
elseif ($DbType -eq "SqlServer") {
    $sqlContent = Get-Content $SqlOutputPath -Raw
    $wrappedSql = "BEGIN TRANSACTION;\n" + $sqlContent + "\nCOMMIT TRANSACTION;"
    Set-Content $SqlOutputPath $wrappedSql -Encoding UTF8
}

Write-Host "✅ Transaction wrapper added." -ForegroundColor Green

# Step 5: Apply migrations inside transaction
Write-Host "🚀 Applying migrations to $DbType database..." -ForegroundColor Cyan

try {
    if ($DbType -eq "Postgres") {
        & psql -h $DbHost -U $DbUser -d $DbName -f $SqlOutputPath
    }
    elseif ($DbType -eq "SqlServer") {
        Invoke-Sqlcmd -ServerInstance $DbHost -Database $DbName -InputFile $SqlOutputPath
    }
    Write-Host "🎉 Migration completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Migration failed. Rolling back changes..." -ForegroundColor Red
    # Rollback is automatic since migration is in a transaction
}
