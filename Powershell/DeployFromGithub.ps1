param(
    [string]$ArtifactUrl = "https://nightly-builds.example.com/mvc-app-publish.zip",
    [string]$SitePath = "C:\inetpub\wwwroot\MyApp",
    [string]$BackupPath = "C:\backups\MyApp"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $BackupPath $timestamp
$zipFile = "C:\temp\deploy.zip"

Write-Host "📦 Downloading latest build..."
Invoke-WebRequest -Uri $ArtifactUrl -OutFile $zipFile

Write-Host "📂 Backing up current site..."
Copy-Item $SitePath $backupDir -Recurse

Write-Host "🗑 Clearing old files..."
Remove-Item "$SitePath\*" -Recurse -Force

Write-Host "📂 Extracting new build..."
Expand-Archive $zipFile -DestinationPath $SitePath -Force

Write-Host "🔄 Running DB migrations..."
& "C:\scripts\Run-ProdMigration.ps1" `
    -ProjectPath "$SitePath\MyApp.csproj" `
    -DbType "Postgres" `
    -DbHost "prod-db.example.com" `
    -DbName "MyAppProd" `
    -DbUser "myuser" `
    -DbPassword "mypassword"

Write-Host "♻ Restarting IIS..."
iisreset
