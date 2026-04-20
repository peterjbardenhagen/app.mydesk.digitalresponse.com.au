#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Install SQL Server Express 2022 with Windows and SQL Authentication
.DESCRIPTION
    Downloads and installs SQL Server Express with:
    - Instance name: localhost/sqlserver
    - Windows Authentication: Enabled
    - SQL Server Authentication: Enabled (sa/password)
    - TCP/IP: Enabled for remote connections
.NOTES
    Run as Administrator
#>

param(
    [string]$DownloadPath = "$env:TEMP\SQLEXPR_x64_ENU.exe",
    [string]$InstanceName = "sqlserver",
    [string]$SAPassword = "password",
    [string]$SQLPort = "1433"
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $color = switch ($Status) {
        "SUCCESS" { "Green" }
        "ERROR"   { "Red" }
        "WARNING" { "Yellow" }
        default   { "Cyan" }
    }
    Write-Host "[$Status] $Message" -ForegroundColor $color
}

function Test-IsAdmin {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Download-SQLExpress {
    $url = "https://go.microsoft.com/fwlink/?linkid=2215158"  # SQL Server 2022 Express x64
    
    Write-Status "Downloading SQL Server Express 2022..." "INFO"
    
    if (Test-Path $DownloadPath) {
        Write-Status "Installer already exists at $DownloadPath" "WARNING"
        $response = Read-Host "Download again? (y/n)"
        if ($response -ne 'y') { return }
    }
    
    try {
        $ProgressPreference = 'Continue'
        Invoke-WebRequest -Uri $url -OutFile $DownloadPath -UseBasicParsing
        Write-Status "Download completed: $DownloadPath" "SUCCESS"
    }
    catch {
        Write-Status "Download failed: $_" "ERROR"
        Write-Status "Please manually download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads" "WARNING"
        exit 1
    }
}

function Install-SQLExpress {
    Write-Status "Installing SQL Server Express..." "INFO"
    
    # Create extraction directory
    $extractPath = "$env:TEMP\SQLEXPR_Extract"
    if (Test-Path $extractPath) {
        Remove-Item $extractPath -Recurse -Force
    }
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
    
    # Extract installer
    Write-Status "Extracting installer..." "INFO"
    $process = Start-Process -FilePath $DownloadPath -ArgumentList "/q", "/x:`"$extractPath`"" -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        Write-Status "Extraction failed with exit code $($process.ExitCode)" "ERROR"
        exit 1
    }
    
    # Find setup.exe
    $setupExe = Get-ChildItem -Path $extractPath -Recurse -Filter "setup.exe" | Select-Object -First 1
    if (-not $setupExe) {
        Write-Status "setup.exe not found in extracted files" "ERROR"
        exit 1
    }
    
    # Installation configuration
    $installParams = @{
        FilePath = $setupExe.FullName
        ArgumentList = @(
            "/ACTION=Install"
            "/FEATURES=SQLEngine"
            "/INSTANCENAME=$InstanceName"
            "/SQLSVCACCOUNT=`"NT AUTHORITY\SYSTEM`""
            "/SQLSYSADMINACCOUNTS=`"BUILTIN\ADMINISTRATORS`""
            "/SECURITYMODE=SQL"
            "/SAPWD=`"$SAPassword`""
            "/TCPENABLED=1"
            "/NPENABLED=1"
            "/IACCEPTSQLSERVERLICENSETERMS"
            "/QUIET"  # Silent install, change to /QUIETSIMPLE for progress bar
        )
        Wait = $true
        PassThru = $true
    }
    
    Write-Status "Starting installation (this may take 10-20 minutes)..." "INFO"
    Write-Status "Instance: $InstanceName" "INFO"
    Write-Status "SQL Auth: sa / $SAPassword" "WARNING"
    
    $installProcess = Start-Process @installParams
    
    if ($installProcess.ExitCode -eq 0) {
        Write-Status "SQL Server Express installed successfully!" "SUCCESS"
    }
    else {
        Write-Status "Installation failed with exit code $($installProcess.ExitCode)" "ERROR"
        Write-Status "Check the installation logs at: $env:ProgramFiles\Microsoft SQL Server\*\Setup Bootstrap\Log\" "WARNING"
        exit 1
    }
}

function Configure-SQLServer {
    Write-Status "Configuring SQL Server..." "INFO"
    
    # Enable TCP/IP
    $smo = "Microsoft.SqlServer.Management.Smo"
    try {
        Add-Type -AssemblyName $smo
    }
    catch {
        Write-Status "SMO not available, using SQLCMD for configuration" "WARNING"
        Configure-WithSQLCMD
        return
    }
    
    try {
        $server = New-Object Microsoft.SqlServer.Management.Smo.Server("localhost\$InstanceName")
        
        # Enable SQL Server Authentication
        $server.Settings.LoginMode = [Microsoft.SqlServer.Management.Smo.ServerLoginMode]::Mixed
        $server.Alter()
        
        Write-Status "Mixed authentication mode enabled" "SUCCESS"
    }
    catch {
        Write-Status "Could not configure via SMO: $_" "WARNING"
    }
    
    # Configure Windows Firewall
    Configure-Firewall
}

function Configure-WithSQLCMD {
    Write-Status "Configuring using SQLCMD..." "INFO"
    
    # SQL script to enable mixed mode and configure SA
    $sqlScript = @"
ALTER LOGIN sa ENABLE;
ALTER LOGIN sa WITH PASSWORD = '$SAPassword';
GO
EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'LoginMode', 
    REG_DWORD, 
    2;
GO
"@
    
    $sqlFile = "$env:TEMP\configure_sql.sql"
    $sqlScript | Out-File -FilePath $sqlFile -Encoding ASCII
    
    Write-Status "SQL configuration script created: $sqlFile" "INFO"
    Write-Status "You may need to run this script manually after restart" "WARNING"
}

function Configure-Firewall {
    Write-Status "Configuring Windows Firewall for SQL Server..." "INFO"
    
    # SQL Server port
    $ruleName = "SQL Server Express - $InstanceName"
    
    # Remove existing rule if present
    Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    
    # Create new rule
    try {
        New-NetFirewallRule `
            -DisplayName $ruleName `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $SQLPort `
            -Action Allow `
            -Profile Any `
            -ErrorAction Stop
        
        Write-Status "Firewall rule created for port $SQLPort" "SUCCESS"
    }
    catch {
        Write-Status "Could not create firewall rule: $_" "WARNING"
    }
}

function Restart-SQLService {
    Write-Status "Restarting SQL Server service..." "INFO"
    
    $serviceName = "MSSQL`$$InstanceName"
    
    try {
        Restart-Service -Name $serviceName -Force
        Write-Status "SQL Server service restarted" "SUCCESS"
    }
    catch {
        Write-Status "Could not restart service: $_" "WARNING"
        Write-Status "Please restart the SQL Server service manually" "WARNING"
    }
}

function Test-Connection {
    Write-Status "Testing SQL Server connection..." "INFO"
    
    # Test Windows Auth
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = "Server=localhost\$InstanceName;Integrated Security=true;Connection Timeout=10;"
        $conn.Open()
        $conn.Close()
        Write-Status "Windows Authentication: SUCCESS" "SUCCESS"
    }
    catch {
        Write-Status "Windows Authentication: FAILED ($($_.Exception.Message))" "WARNING"
    }
    
    # Test SQL Auth
    Start-Sleep -Seconds 2  # Give service time to fully start
    
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection
        $conn.ConnectionString = "Server=localhost\$InstanceName;User Id=sa;Password=$SAPassword;Connection Timeout=10;"
        $conn.Open()
        $conn.Close()
        Write-Status "SQL Server Authentication (sa): SUCCESS" "SUCCESS"
    }
    catch {
        Write-Status "SQL Server Authentication: FAILED ($($_.Exception.Message))" "ERROR"
        Write-Status "You may need to restart SQL Server service and try again" "WARNING"
    }
}

function Show-Summary {
    Write-Host "`n" + ("=" * 60) -ForegroundColor Green
    Write-Host "  SQL Server Express Installation Complete" -ForegroundColor Green
    Write-Host ("=" * 60) -ForegroundColor Green
    Write-Host ""
    Write-Host "  Server Name:     localhost\$InstanceName" -ForegroundColor Cyan
    Write-Host "  TCP Port:        $SQLPort" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Connection Strings:" -ForegroundColor White
    Write-Host ""
    Write-Host "  Windows Auth:" -ForegroundColor Yellow
    Write-Host "    Server=localhost\$InstanceName;Database=YourDB;Trusted_Connection=True;" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  SQL Server Auth:" -ForegroundColor Yellow
    Write-Host "    Server=localhost\$InstanceName;Database=YourDB;User Id=sa;Password=$SAPassword;" -ForegroundColor Gray
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Green
    Write-Host ""
    Write-Host "  IMPORTANT: Change the SA password in production!" -ForegroundColor Red -BackgroundColor Black
    Write-Host "    Current: $SAPassword" -ForegroundColor Red
    Write-Host ""
    Write-Host "  To change SA password, run:" -ForegroundColor Cyan
    Write-Host "    ALTER LOGIN sa WITH PASSWORD = 'NewStrongPassword!';" -ForegroundColor Gray
    Write-Host ""
}

# ==================== MAIN ====================

# Check for admin rights
if (-not (Test-IsAdmin)) {
    Write-Status "This script requires Administrator privileges. Please run as Administrator." "ERROR"
    exit 1
}

# Check if SQL Server already installed
$existingInstance = Get-Service | Where-Object { $_.Name -like "*MSSQL*$InstanceName*" }
if ($existingInstance) {
    Write-Status "SQL Server instance '$InstanceName' already exists!" "WARNING"
    $response = Read-Host "Continue anyway? This may overwrite the instance. (y/n)"
    if ($response -ne 'y') { exit 0 }
}

# Show configuration
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "  SQL Server Express 2022 Installation" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""
Write-Host "  Configuration:" -ForegroundColor White
Write-Host "    Instance Name:    $InstanceName"
Write-Host "    Download Path:    $DownloadPath"
Write-Host "    SA Password:      $SAPassword"
Write-Host "    TCP Port:         $SQLPort"
Write-Host ""
Write-Host "  Authentication Modes:" -ForegroundColor White
Write-Host "    [x] Windows Authentication"
Write-Host "    [x] SQL Server Authentication (sa)"
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Proceed with installation? (y/n)"
if ($confirm -ne 'y') {
    Write-Status "Installation cancelled by user" "WARNING"
    exit 0
}

# Execute installation steps
try {
    Download-SQLExpress
    Install-SQLExpress
    Configure-SQLServer
    Restart-SQLService
    Start-Sleep -Seconds 5  # Wait for service to be ready
    Test-Connection
    Show-Summary
}
catch {
    Write-Status "Installation failed: $_" "ERROR"
    exit 1
}

Write-Status "Installation script completed!" "SUCCESS"
