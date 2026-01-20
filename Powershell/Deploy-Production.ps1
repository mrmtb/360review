<#
.SYNOPSIS
    Production deployment script for 360review application
.DESCRIPTION
    Downloads build artifact, backs up current version, deploys new version,
    runs database migrations, and restarts IIS application pool
.EXAMPLE
    .\Deploy-Production.ps1 -BuildNumber "1234567890"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BuildNumber,
    
    [string]$ArtifactUrl = "https://api.github.com/repos/mrmtb/360review/actions/artifacts",
    [string]$GitHubToken = $env:GITHUB_TOKEN,
    [string]$SitePath = "C:\inetpub\wwwroot\360review",
    [string]$BackupPath = "C:\Deployments\Backups\360review",
    [string]$AppPoolName = "360ReviewAppPool",
    [string]$SiteName = "360Review",
    [switch]$SkipMigrations = $false,
    [switch]$SkipBackup = $false,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$deploymentLog = "C:\Deployments\Logs\deployment-$timestamp.log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') [$Level] $Message"
    Write-Host $logMessage
    Add-Content -Path $deploymentLog -Value $logMessage
}

function Stop-AppPool {
    Write-Log "Stopping IIS Application Pool: $AppPoolName" "INFO"
    if (-not $DryRun) {
        Import-Module WebAdministration
        Stop-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 5
    }
}

function Start-AppPool {
    Write-Log "Starting IIS Application Pool: $AppPoolName" "INFO"
    if (-not $DryRun) {
        Import-Module WebAdministration
        Start-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 5
    }
}

function Test-AppPoolHealth {
    Write-Log "Checking Application Pool health..." "INFO"
    Import-Module WebAdministration
    $appPool = Get-WebAppPoolState -Name $AppPoolName
    
    if ($appPool.Value -eq "Started") {
        Write-Log "Application Pool is running" "INFO"
        return $true
    } else {
        Write-Log "Application Pool is not running: $($appPool.Value)" "ERROR"
        return $false
    }
}

function Invoke-HealthCheck {
    param([string]$Url = "https://localhost/health")
    
    Write-Log "Performing health check at $Url" "INFO"
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30
        if ($response.StatusCode -eq 200) {
            Write-Log "Health check passed" "INFO"
            return $true
        }
    } catch {
        Write-Log "Health check failed: $_" "ERROR"
        return $false
    }
    return $false
}

function Backup-CurrentDeployment {
    if ($SkipBackup) {
        Write-Log "Skipping backup as requested" "WARN"
        return $null
    }
    
    $backupDir = Join-Path $BackupPath $timestamp
    Write-Log "Creating backup at: $backupDir" "INFO"
    
    if (-not $DryRun) {
        if (-not (Test-Path $BackupPath)) {
            New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
        }
        
        if (Test-Path $SitePath) {
            Copy-Item -Path $SitePath -Destination $backupDir -Recurse -Force
            Write-Log "Backup completed successfully" "INFO"
        } else {
            Write-Log "No existing deployment to backup" "WARN"
        }
    }
    
    return $backupDir
}

function Restore-FromBackup {
    param([string]$BackupDir)
    
    if (-not $BackupDir -or -not (Test-Path $BackupDir)) {
        Write-Log "No backup available for rollback" "ERROR"
        return $false
    }
    
    Write-Log "Rolling back to backup: $BackupDir" "WARN"
    if (-not $DryRun) {
        Remove-Item "$SitePath\*" -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item "$BackupDir\*" -Destination $SitePath -Recurse -Force
        Write-Log "Rollback completed" "INFO"
    }
    return $true
}

function Download-Artifact {
    param([string]$BuildNumber)
    
    $tempZip = "C:\temp\360review-$BuildNumber.zip"
    Write-Log "Downloading artifact for build: $BuildNumber" "INFO"
    
    if (-not $DryRun) {
        # Ensure temp directory exists
        if (-not (Test-Path "C:\temp")) {
            New-Item -ItemType Directory -Path "C:\temp" -Force | Out-Null
        }
        
        # Download artifact from GitHub Actions
        $headers = @{
            "Authorization" = "Bearer $GitHubToken"
            "Accept" = "application/vnd.github.v3+json"
        }
        
        try {
            # List artifacts and find the one matching our build
            $artifacts = Invoke-RestMethod -Uri $ArtifactUrl -Headers $headers
            $artifact = $artifacts.artifacts | Where-Object { $_.name -like "*$BuildNumber*" } | Select-Object -First 1
            
            if (-not $artifact) {
                throw "Artifact not found for build: $BuildNumber"
            }
            
            Write-Log "Downloading artifact: $($artifact.name)" "INFO"
            $downloadUrl = $artifact.archive_download_url
            Invoke-WebRequest -Uri $downloadUrl -Headers $headers -OutFile $tempZip
            Write-Log "Download completed" "INFO"
        } catch {
            Write-Log "Failed to download artifact: $_" "ERROR"
            throw
        }
    }
    
    return $tempZip
}

function Deploy-Application {
    param([string]$ZipFile)
    
    Write-Log "Deploying application from: $ZipFile" "INFO"
    
    if (-not $DryRun) {
        # Clear existing files (except logs and app_data)
        Write-Log "Clearing existing deployment files..." "INFO"
        Get-ChildItem -Path $SitePath -Exclude @("logs", "App_Data") | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        
        # Extract new deployment
        Write-Log "Extracting new deployment..." "INFO"
        Expand-Archive -Path $ZipFile -DestinationPath $SitePath -Force
        
        # Set permissions
        Write-Log "Setting folder permissions..." "INFO"
        $acl = Get-Acl $SitePath
        $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $acl.SetAccessRule($rule)
        Set-Acl $SitePath $acl
        
        Write-Log "Deployment completed" "INFO"
    }
}

function Invoke-DatabaseMigration {
    if ($SkipMigrations) {
        Write-Log "Skipping database migrations as requested" "WARN"
        return $true
    }
    
    $migrationScript = Join-Path $SitePath "migrations.sql"
    if (-not (Test-Path $migrationScript)) {
        Write-Log "No migration script found at: $migrationScript" "WARN"
        return $true
    }
    
    Write-Log "Running database migrations..." "INFO"
    
    if (-not $DryRun) {
        try {
            # Call the migration PowerShell script
            $migrationScriptPath = Join-Path $PSScriptRoot "Run-Migration.ps1"
            
            if (Test-Path $migrationScriptPath) {
                & $migrationScriptPath `
                    -ProjectPath (Join-Path $SitePath "360review.csproj") `
                    -DbType "Postgres" `
                    -DbHost $env:DB_HOST `
                    -DbName $env:DB_NAME `
                    -DbUser $env:DB_USER `
                    -DbPassword $env:DB_PASSWORD `
                    -BackupPath "C:\Deployments\DB_Backups"
                
                Write-Log "Database migrations completed" "INFO"
            } else {
                Write-Log "Migration script not found, applying SQL directly..." "WARN"
                # Direct SQL execution as fallback
                $env:PGPASSWORD = $env:DB_PASSWORD
                & psql -h $env:DB_HOST -U $env:DB_USER -d $env:DB_NAME -f $migrationScript
            }
            
            return $true
        } catch {
            Write-Log "Database migration failed: $_" "ERROR"
            return $false
        }
    }
    
    return $true
}

# Main deployment workflow
try {
    Write-Log "========================================" "INFO"
    Write-Log "Starting Deployment for Build: $BuildNumber" "INFO"
    Write-Log "========================================" "INFO"
    
    if ($DryRun) {
        Write-Log "DRY RUN MODE - No actual changes will be made" "WARN"
    }
    
    # Step 1: Stop Application Pool
    Stop-AppPool
    
    # Step 2: Backup current deployment
    $backupDir = Backup-CurrentDeployment
    
    # Step 3: Download new artifact
    $zipFile = Download-Artifact -BuildNumber $BuildNumber
    
    # Step 4: Deploy application
    Deploy-Application -ZipFile $zipFile
    
    # Step 5: Run database migrations
    $migrationSuccess = Invoke-DatabaseMigration
    
    if (-not $migrationSuccess) {
        Write-Log "Migration failed, rolling back deployment" "ERROR"
        Restore-FromBackup -BackupDir $backupDir
        Start-AppPool
        exit 1
    }
    
    # Step 6: Start Application Pool
    Start-AppPool
    
    # Step 7: Health check
    Start-Sleep -Seconds 10
    if (-not (Test-AppPoolHealth)) {
        Write-Log "Application Pool health check failed, rolling back" "ERROR"
        Stop-AppPool
        Restore-FromBackup -BackupDir $backupDir
        Start-AppPool
        exit 1
    }
    
    # Step 8: Application health check
    if (-not (Invoke-HealthCheck -Url "https://localhost/$SiteName/health")) {
        Write-Log "Application health check failed, rolling back" "ERROR"
        Stop-AppPool
        Restore-FromBackup -BackupDir $backupDir
        Start-AppPool
        exit 1
    }
    
    # Step 9: Cleanup old backups (keep last 10)
    if (-not $DryRun -and (Test-Path $BackupPath)) {
        Write-Log "Cleaning up old backups..." "INFO"
        Get-ChildItem $BackupPath | Sort-Object CreationTime -Descending | Select-Object -Skip 10 | Remove-Item -Recurse -Force
    }
    
    Write-Log "========================================" "INFO"
    Write-Log "Deployment completed successfully!" "INFO"
    Write-Log "========================================" "INFO"
    
    exit 0
    
} catch {
    Write-Log "Deployment failed with error: $_" "ERROR"
    Write-Log "Stack Trace: $($_.ScriptStackTrace)" "ERROR"
    
    # Attempt rollback
    if ($backupDir) {
        Write-Log "Attempting automatic rollback..." "ERROR"
        Stop-AppPool
        Restore-FromBackup -BackupDir $backupDir
        Start-AppPool
    }
    
    exit 1
}
