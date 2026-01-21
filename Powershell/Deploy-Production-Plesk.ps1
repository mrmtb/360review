<#
.SYNOPSIS
    Production deployment script for 360review application on Plesk
.DESCRIPTION
    Downloads build artifact, backs up current version, deploys new version,
    runs database migrations, and recycles application pool via Plesk
.EXAMPLE
    .\Deploy-Production-Plesk.ps1 -BuildNumber "1234567890" -Domain "reviews.yourdomain.com"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BuildNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$Domain,
    
    [string]$ArtifactUrl = "https://api.github.com/repos/mrmtb/360review/actions/artifacts",
    [string]$GitHubToken = $env:GH_PAT,
    [string]$PleskVhostsPath = "C:\Inetpub\vhosts",
    [string]$BackupPath = "C:\Plesk\Backups\360review",
    [string]$PleskBin = "${env:plesk_bin}",
    [switch]$SkipMigrations = $false,
    [switch]$SkipBackup = $false,
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$deploymentLog = Join-Path $BackupPath "deployment-$timestamp.log"

# Calculate paths
$SitePath = Join-Path $PleskVhostsPath "$Domain\httpdocs"
$PleskDomainPath = Join-Path $PleskVhostsPath $Domain

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $logMessage = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') [$Level] $Message"
    Write-Host $logMessage
    if (-not (Test-Path (Split-Path $deploymentLog))) {
        New-Item -ItemType Directory -Path (Split-Path $deploymentLog) -Force | Out-Null
    }
    Add-Content -Path $deploymentLog -Value $logMessage
}

function Get-PleskAppPool {
    Write-Log "Getting Application Pool name for domain: $Domain" "INFO"
    
    # Plesk naming convention: IIS APPPOOL\<domain>
    $appPoolName = $Domain
    
    # Try to get the app pool from Plesk
    if (Test-Path $PleskBin) {
        try {
            $pleskInfo = & "$PleskBin\websrvmng.exe" --reconfigure-vhost --vhost-name=$Domain --verbose
            Write-Log "Plesk vhost info retrieved" "INFO"
        } catch {
            Write-Log "Could not query Plesk, using default app pool name" "WARN"
        }
    }
    
    return $appPoolName
}

function Stop-PleskAppPool {
    param([string]$AppPoolName)
    
    Write-Log "Stopping Application Pool: $AppPoolName" "INFO"
    if (-not $DryRun) {
        try {
            Import-Module WebAdministration -ErrorAction SilentlyContinue
            
            if (Test-Path "IIS:\AppPools\$AppPoolName") {
                Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 5
                Write-Log "App pool stopped successfully" "INFO"
            } else {
                Write-Log "App pool not found in IIS, may be managed by Plesk" "WARN"
            }
        } catch {
            Write-Log "Error stopping app pool: $_" "WARN"
            # Continue anyway as Plesk may manage this differently
        }
    }
}

function Start-PleskAppPool {
    param([string]$AppPoolName)
    
    Write-Log "Starting Application Pool: $AppPoolName" "INFO"
    if (-not $DryRun) {
        try {
            Import-Module WebAdministration -ErrorAction SilentlyContinue
            
            if (Test-Path "IIS:\AppPools\$AppPoolName") {
                Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 5
                Write-Log "App pool started successfully" "INFO"
            } else {
                # Use Plesk's reconfigure to restart
                & "$PleskBin\websrvmng.exe" --reconfigure-vhost --vhost-name=$Domain
                Write-Log "Plesk vhost reconfigured" "INFO"
            }
        } catch {
            Write-Log "Error starting app pool: $_" "WARN"
        }
    }
}

function Recycle-PleskAppPool {
    param([string]$AppPoolName)
    
    Write-Log "Recycling Application Pool via Plesk" "INFO"
    if (-not $DryRun) {
        try {
            if (Test-Path $PleskBin) {
                # Use Plesk command to recycle
                & "$PleskBin\websrvmng.exe" --reconfigure-vhost --vhost-name=$Domain
                Write-Log "Application pool recycled via Plesk" "INFO"
            } else {
                # Fallback to direct IIS management
                Import-Module WebAdministration
                if (Test-Path "IIS:\AppPools\$AppPoolName") {
                    Restart-WebAppPool -Name $AppPoolName
                }
            }
            Start-Sleep -Seconds 5
        } catch {
            Write-Log "Error recycling app pool: $_" "ERROR"
            throw
        }
    }
}

function Test-AppPoolHealth {
    param([string]$AppPoolName)
    
    Write-Log "Checking Application Pool health..." "INFO"
    try {
        Import-Module WebAdministration -ErrorAction SilentlyContinue
        
        if (Test-Path "IIS:\AppPools\$AppPoolName") {
            $appPool = Get-WebAppPoolState -Name $AppPoolName
            
            if ($appPool.Value -eq "Started") {
                Write-Log "Application Pool is running" "INFO"
                return $true
            } else {
                Write-Log "Application Pool is not running: $($appPool.Value)" "ERROR"
                return $false
            }
        } else {
            Write-Log "App pool not directly accessible, assuming Plesk management" "WARN"
            return $true
        }
    } catch {
        Write-Log "Could not check app pool status: $_" "WARN"
        return $true  # Assume healthy if we can't check
    }
}

function Invoke-HealthCheck {
    param([string]$Url = "https://$Domain/health")
    
    Write-Log "Performing health check at $Url" "INFO"
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Log "Health check passed" "INFO"
            return $true
        }
    } catch {
        Write-Log "Health check failed: $_" "ERROR"
        # Try HTTP if HTTPS fails
        try {
            $httpUrl = "http://$Domain/health"
            $response = Invoke-WebRequest -Uri $httpUrl -UseBasicParsing -TimeoutSec 30
            if ($response.StatusCode -eq 200) {
                Write-Log "Health check passed (HTTP)" "INFO"
                return $true
            }
        } catch {
            Write-Log "Health check failed on both HTTPS and HTTP" "ERROR"
        }
        return $false
    }
    return $false
}

function Backup-CurrentDeployment {
    if ($SkipBackup) {
        Write-Log "Skipping backup as requested" "WARN"
        return $null
    }
    
    $backupDir = Join-Path $BackupPath "$Domain-$timestamp"
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
        # Create site path if it doesn't exist
        if (-not (Test-Path $SitePath)) {
            New-Item -ItemType Directory -Path $SitePath -Force | Out-Null
        }
        
        # Clear existing files (except logs, App_Data, plesk-stat, and protected directories)
        Write-Log "Clearing existing deployment files..." "INFO"
        $excludeDirs = @("logs", "App_Data", "plesk-stat", "statistics", "error_docs", "_private")
        Get-ChildItem -Path $SitePath | Where-Object { 
            $_.Name -notin $excludeDirs 
        } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        
        # Extract new deployment
        Write-Log "Extracting new deployment..." "INFO"
        Expand-Archive -Path $ZipFile -DestinationPath $SitePath -Force
        
        # Set permissions for Plesk
        Write-Log "Setting folder permissions for Plesk..." "INFO"
        $pleskSystemUser = "PLESK_IUSR"
        $acl = Get-Acl $SitePath
        
        # Add IIS_IUSRS permission
        $iisRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $acl.SetAccessRule($iisRule)
        
        # Add PLESK_IUSR permission if exists
        try {
            $pleskRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $pleskSystemUser, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
            )
            $acl.SetAccessRule($pleskRule)
        } catch {
            Write-Log "Could not set PLESK_IUSR permissions (may not exist)" "WARN"
        }
        
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
                    -BackupPath "C:\Plesk\Backups\DB_Backups"
                
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
    Write-Log "Starting Plesk Deployment for Build: $BuildNumber" "INFO"
    Write-Log "Domain: $Domain" "INFO"
    Write-Log "Site Path: $SitePath" "INFO"
    Write-Log "========================================" "INFO"
    
    if ($DryRun) {
        Write-Log "DRY RUN MODE - No actual changes will be made" "WARN"
    }
    
    # Verify Plesk paths exist
    if (-not (Test-Path $PleskVhostsPath)) {
        throw "Plesk vhosts path not found: $PleskVhostsPath"
    }
    
    if (-not (Test-Path $PleskDomainPath)) {
        throw "Domain path not found: $PleskDomainPath. Has the domain been created in Plesk?"
    }
    
    # Get app pool name
    $appPoolName = Get-PleskAppPool
    
    # Step 1: Stop Application Pool
    Stop-PleskAppPool -AppPoolName $appPoolName
    
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
        Start-PleskAppPool -AppPoolName $appPoolName
        exit 1
    }
    
    # Step 6: Start Application Pool / Recycle
    Start-PleskAppPool -AppPoolName $appPoolName
    
    # Step 7: Health check
    Start-Sleep -Seconds 10
    if (-not (Test-AppPoolHealth -AppPoolName $appPoolName)) {
        Write-Log "Application Pool health check failed, rolling back" "ERROR"
        Stop-PleskAppPool -AppPoolName $appPoolName
        Restore-FromBackup -BackupDir $backupDir
        Start-PleskAppPool -AppPoolName $appPoolName
        exit 1
    }
    
    # Step 8: Application health check
    if (-not (Invoke-HealthCheck)) {
        Write-Log "Application health check failed, rolling back" "ERROR"
        Stop-PleskAppPool -AppPoolName $appPoolName
        Restore-FromBackup -BackupDir $backupDir
        Start-PleskAppPool -AppPoolName $appPoolName
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
        Stop-PleskAppPool -AppPoolName $appPoolName
        Restore-FromBackup -BackupDir $backupDir
        Start-PleskAppPool -AppPoolName $appPoolName
    }
    
    exit 1
}
