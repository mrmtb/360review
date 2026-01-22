<#
.SYNOPSIS
    Sets up a domain in Plesk for 360review .NET 10 application
.DESCRIPTION
    Configures Plesk domain for ASP.NET Core 10 hosting
.EXAMPLE
    .\Setup-Plesk.ps1 -Domain "reviews.yourdomain.com"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Domain,
    
    [string]$PleskAdminBin = "${env:plesk_bin}",
    [string]$PleskVhostsPath = "C:\Inetpub\vhosts",
    [switch]$EnableSSL = $false
)

$ErrorActionPreference = "Stop"

# Derive Plesk paths - admin tools are in admin\bin64, domain tools are in bin
$PleskRoot = if ($PleskAdminBin) {
    Split-Path (Split-Path $PleskAdminBin -Parent) -Parent
} else {
    "C:\Program Files (x86)\Plesk"
}
$PleskBin = Join-Path $PleskRoot "bin"
$PleskAdminBinResolved = Join-Path $PleskRoot "admin\bin64"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-ColorOutput "This script must be run as Administrator" "Red"
    exit 1
}

# Verify Plesk is installed
if (-not (Test-Path $PleskBin)) {
    Write-ColorOutput "Plesk not found at: $PleskBin" "Red"
    Write-ColorOutput "Please verify Plesk installation" "Red"
    exit 1
}

Write-ColorOutput "Configuring Plesk domain for ASP.NET Core 10 application" "Cyan"
Write-ColorOutput "Domain: $Domain" "White"
Write-ColorOutput "Plesk Root: $PleskRoot" "Gray"
Write-ColorOutput "Plesk Bin: $PleskBin" "Gray"

# Calculate paths
$DomainPath = Join-Path $PleskVhostsPath $Domain
$HttpDocsPath = Join-Path $DomainPath "httpdocs"
$LogsPath = Join-Path $HttpDocsPath "logs"

# Check if domain exists in Plesk
Write-ColorOutput "`nChecking if domain exists in Plesk..." "Cyan"

$domainExePath = Join-Path $PleskBin "domain.exe"
Write-ColorOutput "Using Plesk binary: $domainExePath" "Gray"
Write-ColorOutput "File exists: $(Test-Path $domainExePath)" "Gray"
Write-ColorOutput "Checking domain: $Domain" "Gray"

if (-not (Test-Path $domainExePath)) {
    Write-ColorOutput "ERROR: domain.exe not found at: $domainExePath" "Red"
    Write-ColorOutput "PleskBin variable: $PleskBin" "Red"
    exit 1
}

$domainExists = $false
try {
    # Use Start-Process to handle paths with spaces more reliably
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $domainExePath
    $processInfo.Arguments = "--info $Domain"
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null
    $output = $process.StandardOutput.ReadToEnd()
    $errorOutput = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    
    Write-ColorOutput "Exit code: $exitCode" "Gray"
    if ($output) { Write-ColorOutput "Output: $output" "Gray" }
    if ($errorOutput) { Write-ColorOutput "Error: $errorOutput" "Gray" }
    
    # Check exit code - Plesk returns 0 on success
    if ($exitCode -eq 0) {
        $domainExists = $true
    }
} catch {
    Write-ColorOutput "Exception caught: $_" "Red"
    Write-ColorOutput "Exception type: $($_.Exception.GetType().FullName)" "Red"
    $domainExists = $false
}

if (-not $domainExists) {
    Write-ColorOutput "Domain not found in Plesk!" "Red"
    Write-ColorOutput "`nPlease create the domain in Plesk first:" "Yellow"
    Write-ColorOutput "1. Login to Plesk (https://yourserver:8443)" "White"
    Write-ColorOutput "2. Go to 'Websites & Domains'" "White"
    Write-ColorOutput "3. Click 'Add Domain'" "White"
    Write-ColorOutput "4. Enter domain name: $Domain" "White"
    Write-ColorOutput "5. Complete the domain creation wizard" "White"
    Write-ColorOutput "`nThen run this script again." "Yellow"
    exit 1
}

Write-ColorOutput "Domain found in Plesk!" "Green"

# Verify domain directory exists
if (-not (Test-Path $DomainPath)) {
    Write-ColorOutput "Domain directory not found: $DomainPath" "Red"
    exit 1
}

# Create necessary directories
Write-ColorOutput "`nCreating application directories..." "Cyan"

$directories = @(
    $HttpDocsPath,
    $LogsPath,
    (Join-Path $HttpDocsPath "App_Data"),
    "C:\Plesk\Backups\360review",
    "C:\Plesk\Backups\DB_Backups"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-ColorOutput "Created: $dir" "Green"
    } else {
        Write-ColorOutput "Exists: $dir" "Gray"
    }
}

# Set folder permissions
Write-ColorOutput "`nSetting folder permissions..." "Cyan"
$acl = Get-Acl $HttpDocsPath

# Add IIS_IUSRS permission
$iisRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($iisRule)

# Try to add PLESK_IUSR permission
try {
    $pleskRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "PLESK_IUSR", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
    )
    $acl.SetAccessRule($pleskRule)
} catch {
    Write-ColorOutput "Note: PLESK_IUSR user not found (may not be needed)" "Yellow"
}

Set-Acl $HttpDocsPath $acl
Write-ColorOutput "Permissions set successfully" "Green"

# Check .NET 10 Hosting Bundle
Write-ColorOutput "`nChecking ASP.NET Core 10 Hosting Bundle..." "Cyan"
$hostingBundle = Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET" -ErrorAction SilentlyContinue | 
    Where-Object { $_.GetValue("DisplayName") -like "*ASP.NET Core*10*Hosting Bundle*" }

if (-not $hostingBundle) {
    Write-ColorOutput "WARNING: ASP.NET Core Hosting Bundle not found!" "Red"
    Write-ColorOutput "Download from: https://dotnet.microsoft.com/download/dotnet" "Yellow"
    Write-ColorOutput "Install the latest 'Hosting Bundle' package" "Yellow"
    Write-ColorOutput "After installation, run: iisreset" "Yellow"
} else {
    $bundleName = $hostingBundle.GetValue("DisplayName")
    Write-ColorOutput "ASP.NET Core Hosting Bundle found: $bundleName" "Green"
}

# Get application pool name
Write-ColorOutput "`nGetting application pool information..." "Cyan"
try {
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    
    # Plesk typically names app pools same as domain
    $appPoolName = $Domain
    
    if (Test-Path "IIS:\AppPools\$appPoolName") {
        Write-ColorOutput "Application Pool: $appPoolName" "White"
        
        # Configure app pool for .NET Core
        $appPool = Get-Item "IIS:\AppPools\$appPoolName"
        
        Write-ColorOutput "Configuring app pool for .NET Core..." "Cyan"
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value ""
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode" -Value "Integrated"
        
        Write-ColorOutput "App pool configured" "Green"
    } else {
        Write-ColorOutput "App pool not found in IIS: $appPoolName" "Yellow"
        Write-ColorOutput "Plesk will manage the app pool automatically" "White"
    }
} catch {
    Write-ColorOutput "Could not configure app pool directly: $_" "Yellow"
    Write-ColorOutput "Plesk will manage the app pool" "White"
}

# Create web.config
$webConfigPath = Join-Path $HttpDocsPath "web.config"
Write-ColorOutput "`nCreating web.config..." "Cyan"

$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\360review.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="DetailedLocalOnly" />
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="104857600" />
        </requestFiltering>
      </security>
    </system.webServer>
  </location>
</configuration>
"@

Set-Content -Path $webConfigPath -Value $webConfigContent
Write-ColorOutput "web.config created" "Green"

# Create sample appsettings.json
$appSettingsPath = Join-Path $HttpDocsPath "appsettings.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-ColorOutput "`nCreating sample appsettings.json..." "Cyan"
    
    $appSettingsContent = @"
{
  "ConnectionStrings": {
    "ApplicationDBConnection": "Host=localhost;Database=360review;Username=appuser;Password=CHANGE_ME"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@
    
    Set-Content -Path $appSettingsPath -Value $appSettingsContent
    Write-ColorOutput "appsettings.json created - REMEMBER TO UPDATE CONNECTION STRING!" "Yellow"
}

# Configure SSL if requested
if ($EnableSSL) {
    Write-ColorOutput "`nSSL Configuration:" "Cyan"
    Write-ColorOutput "Please configure SSL through Plesk interface:" "Yellow"
    Write-ColorOutput "1. Go to Websites & Domains > $Domain" "White"
    Write-ColorOutput "2. Click 'SSL/TLS Certificates'" "White"
    Write-ColorOutput "3. Install/obtain an SSL certificate" "White"
    Write-ColorOutput "4. Enable 'Permanent SEO-safe 301 redirect from HTTP to HTTPS'" "White"
}

# Reconfigure vhost in Plesk
Write-ColorOutput "`nReconfiguring Plesk vhost..." "Cyan"
try {
    $websrvmngPath = Join-Path $PleskAdminBinResolved "websrvmng.exe"
    
    if (-not (Test-Path $websrvmngPath)) {
        Write-ColorOutput "websrvmng.exe not found at: $websrvmngPath" "Yellow"
        Write-ColorOutput "Skipping vhost reconfiguration" "Yellow"
    } else {
        $processInfo = New-Object System.Diagnostics.ProcessStartInfo
        $processInfo.FileName = $websrvmngPath
        $processInfo.Arguments = "--reconfigure-vhost --vhost-name=$Domain"
        $processInfo.RedirectStandardOutput = $true
        $processInfo.RedirectStandardError = $true
        $processInfo.UseShellExecute = $false
        $processInfo.CreateNoWindow = $true
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $processInfo
        $process.Start() | Out-Null
        $process.WaitForExit()
        
        if ($process.ExitCode -eq 0) {
            Write-ColorOutput "Vhost reconfigured successfully" "Green"
        } else {
            Write-ColorOutput "Vhost reconfiguration returned code: $($process.ExitCode)" "Yellow"
        }
    }
} catch {
    Write-ColorOutput "Could not reconfigure vhost: $_" "Yellow"
    Write-ColorOutput "You may need to reconfigure manually in Plesk" "Yellow"
}

Write-ColorOutput "`n========================================" "Green"
Write-ColorOutput "Plesk Setup Complete!" "Green"
Write-ColorOutput "========================================" "Green"
Write-ColorOutput "`nDomain: $Domain" "White"
Write-ColorOutput "Document Root: $HttpDocsPath" "White"
Write-ColorOutput "Logs: $LogsPath" "White"
Write-ColorOutput "`nNext Steps:" "Yellow"
Write-ColorOutput "1. Update appsettings.json with your database connection string" "White"
Write-ColorOutput "2. Set environment variables in Plesk:" "White"
Write-ColorOutput "   - Go to Websites & Domains > $Domain > PHP Settings" "White"
Write-ColorOutput "   - Or use: Websites & Domains > $Domain > Application Settings" "White"
Write-ColorOutput "3. Deploy your application using Deploy-Production-Plesk.ps1" "White"
Write-ColorOutput "4. Test the application at https://$Domain" "White"
Write-ColorOutput "`nEnvironment Variables to set in Plesk:" "Yellow"
Write-ColorOutput "  ASPNETCORE_ENVIRONMENT = Production" "White"
Write-ColorOutput "  DB_HOST = your_database_host" "White"
Write-ColorOutput "  DB_NAME = 360review" "White"
Write-ColorOutput "  DB_USER = your_db_user" "White"
Write-ColorOutput "  DB_PASSWORD = your_db_password" "White"

Write-ColorOutput "`nDeployment command:" "Cyan"
Write-ColorOutput ".\Deploy-Production-Plesk.ps1 -BuildNumber '<commit-sha>' -Domain '$Domain'" "White"
