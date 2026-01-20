<#
.SYNOPSIS
    Sets up IIS for 360review application on Windows Server
.DESCRIPTION
    Creates application pool, website, configures settings, and SSL
.EXAMPLE
    .\Setup-IIS.ps1 -SiteName "360Review" -HostName "reviews.yourdomain.com"
#>

param(
    [string]$SiteName = "360Review",
    [string]$AppPoolName = "360ReviewAppPool",
    [string]$SitePath = "C:\inetpub\wwwroot\360review",
    [string]$HostName = "reviews.yourdomain.com",
    [int]$HttpPort = 80,
    [int]$HttpsPort = 443,
    [string]$CertificateThumbprint = "",
    [switch]$CreateSelfSignedCert = $false
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# Check if running as Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-ColorOutput "This script must be run as Administrator" "Red"
    exit 1
}

# Install IIS if not already installed
Write-ColorOutput "Checking IIS installation..." "Cyan"
$iisFeature = Get-WindowsFeature -Name Web-Server
if (-not $iisFeature.Installed) {
    Write-ColorOutput "Installing IIS..." "Yellow"
    Install-WindowsFeature -Name Web-Server -IncludeManagementTools
    Install-WindowsFeature -Name Web-Asp-Net45
}

# Import WebAdministration module
Import-Module WebAdministration

# Create site directory
Write-ColorOutput "Creating site directory: $SitePath" "Cyan"
if (-not (Test-Path $SitePath)) {
    New-Item -ItemType Directory -Path $SitePath -Force | Out-Null
}

# Create logs directory
$logsPath = Join-Path $SitePath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
}

# Set folder permissions
Write-ColorOutput "Setting folder permissions..." "Cyan"
$acl = Get-Acl $SitePath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $SitePath $acl

# Create Application Pool
Write-ColorOutput "Creating Application Pool: $AppPoolName" "Cyan"
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-ColorOutput "Application Pool already exists, removing..." "Yellow"
    Remove-WebAppPool -Name $AppPoolName
}

$appPool = New-WebAppPool -Name $AppPoolName
$appPool | Set-ItemProperty -Name "managedRuntimeVersion" -Value ""
$appPool | Set-ItemProperty -Name "managedPipelineMode" -Value "Integrated"
$appPool | Set-ItemProperty -Name "startMode" -Value "AlwaysRunning"
$appPool | Set-ItemProperty -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))
$appPool | Set-ItemProperty -Name "recycling.periodicRestart.time" -Value ([TimeSpan]::FromMinutes(0))

Write-ColorOutput "Application Pool created with .NET Core settings" "Green"

# Remove existing site if it exists
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-ColorOutput "Site already exists, removing..." "Yellow"
    Remove-Website -Name $SiteName
}

# Create Website
Write-ColorOutput "Creating Website: $SiteName" "Cyan"
$site = New-Website -Name $SiteName `
    -PhysicalPath $SitePath `
    -ApplicationPool $AppPoolName `
    -Port $HttpPort `
    -HostHeader $HostName

# Add HTTPS binding if certificate is provided
if ($CertificateThumbprint) {
    Write-ColorOutput "Adding HTTPS binding with certificate..." "Cyan"
    New-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -HostHeader $HostName
    
    $binding = Get-WebBinding -Name $SiteName -Protocol "https"
    $binding.AddSslCertificate($CertificateThumbprint, "My")
    
    Write-ColorOutput "HTTPS binding configured" "Green"
} elseif ($CreateSelfSignedCert) {
    Write-ColorOutput "Creating self-signed certificate..." "Yellow"
    $cert = New-SelfSignedCertificate -DnsName $HostName -CertStoreLocation "Cert:\LocalMachine\My"
    
    New-WebBinding -Name $SiteName -Protocol "https" -Port $HttpsPort -HostHeader $HostName
    $binding = Get-WebBinding -Name $SiteName -Protocol "https"
    $binding.AddSslCertificate($cert.Thumbprint, "My")
    
    Write-ColorOutput "Self-signed certificate created and bound" "Green"
    Write-ColorOutput "Certificate Thumbprint: $($cert.Thumbprint)" "Yellow"
}

# Configure Application Settings
Write-ColorOutput "Configuring application settings..." "Cyan"

# Enable detailed errors for troubleshooting (disable in production)
Set-WebConfigurationProperty -Filter "system.webServer/httpErrors" `
    -PSPath "IIS:\Sites\$SiteName" `
    -Name "errorMode" `
    -Value "Detailed"

# Configure request limits
Set-WebConfigurationProperty -Filter "system.webServer/security/requestFiltering/requestLimits" `
    -PSPath "IIS:\Sites\$SiteName" `
    -Name "maxAllowedContentLength" `
    -Value 104857600  # 100MB

# Enable compression
Set-WebConfigurationProperty -Filter "system.webServer/urlCompression" `
    -PSPath "IIS:\Sites\$SiteName" `
    -Name "doStaticCompression" `
    -Value "True"

Set-WebConfigurationProperty -Filter "system.webServer/urlCompression" `
    -PSPath "IIS:\Sites\$SiteName" `
    -Name "doDynamicCompression" `
    -Value "True"

# Install ASP.NET Core Hosting Bundle if not present
Write-ColorOutput "Checking ASP.NET Core Hosting Bundle..." "Cyan"
$hostingBundle = Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET" -ErrorAction SilentlyContinue | 
    Where-Object { $_.GetValue("DisplayName") -like "*ASP.NET Core*Hosting Bundle*" }

if (-not $hostingBundle) {
    Write-ColorOutput "ASP.NET Core Hosting Bundle not found!" "Red"
    Write-ColorOutput "Please download and install from: https://dotnet.microsoft.com/download/dotnet/10.0" "Yellow"
    Write-ColorOutput "Then run 'iisreset' and restart this script" "Yellow"
} else {
    Write-ColorOutput "ASP.NET Core Hosting Bundle is installed" "Green"
}

# Create a sample web.config if it doesn't exist
$webConfigPath = Join-Path $SitePath "web.config"
if (-not (Test-Path $webConfigPath)) {
    Write-ColorOutput "Creating sample web.config..." "Cyan"
    
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
    </system.webServer>
  </location>
</configuration>
"@
    
    Set-Content -Path $webConfigPath -Value $webConfigContent
    Write-ColorOutput "web.config created" "Green"
}

# Start the site
Write-ColorOutput "Starting website..." "Cyan"
Start-Website -Name $SiteName
Start-WebAppPool -Name $AppPoolName

Write-ColorOutput "`n========================================" "Green"
Write-ColorOutput "IIS Setup Complete!" "Green"
Write-ColorOutput "========================================" "Green"
Write-ColorOutput "Site Name: $SiteName" "White"
Write-ColorOutput "App Pool: $AppPoolName" "White"
Write-ColorOutput "Physical Path: $SitePath" "White"
Write-ColorOutput "HTTP Binding: http://$HostName`:$HttpPort" "White"
if ($CertificateThumbprint -or $CreateSelfSignedCert) {
    Write-ColorOutput "HTTPS Binding: https://$HostName`:$HttpsPort" "White"
}
Write-ColorOutput "`nNext Steps:" "Yellow"
Write-ColorOutput "1. Deploy your application files to: $SitePath" "White"
Write-ColorOutput "2. Configure your appsettings.json with database connection strings" "White"
Write-ColorOutput "3. Ensure your hosts file or DNS points $HostName to this server" "White"
Write-ColorOutput "4. Run database migrations" "White"
Write-ColorOutput "5. Test the application at http://$HostName" "White"
