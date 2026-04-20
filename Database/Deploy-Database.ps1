<#
.SYNOPSIS
    Deploys local Techlight_MyDesk database to production SQL Server 2016
.DESCRIPTION
    Process:
    1. Backs up local DB (Techlight_MyDesk on LocalDB)
    2. Copies .bak file to production server via SMB/UNC path
    3. Restores on production (localhost\SQL2016 on techlight.digitalresponse.com.au)
    4. Verifies restore succeeded

    NOTE: SQL Server 2016 compatible only (no SQL 2017+ features used).
.PARAMETER ProductionServer
    DNS name or IP of production server. Default: techlight.digitalresponse.com.au
.PARAMETER DryRun
    Show what would happen without executing destructive operations
.EXAMPLE
    .\Deploy-Database.ps1
    .\Deploy-Database.ps1 -DryRun
    .\Deploy-Database.ps1 -ProductionServer "techlight.digitalresponse.com.au"
#>

param(
    [string]$ProductionServer = "techlight.digitalresponse.com.au",
    [string]$ProductionInstance = "SQL2016",
    [string]$DatabaseName = "Techlight_MyDesk",
    [string]$SqlUser = "Techlight_MyDesk",
    [string]$SqlPassword = "DigitalResponse2595!",
    [string]$LocalServer = "(localdb)\MSSQLLocalDB",
    [string]$RemoteBackupPath = "C:\SQLBackups",  # Path on production server where .bak will be placed
    [switch]$DryRun,
    [switch]$SkipBackup
)

$ErrorActionPreference = "Stop"

function Write-Status {
    param([string]$Message, [string]$Status = "INFO")
    $color = switch ($Status) {
        "SUCCESS" { "Green" }
        "ERROR"   { "Red" }
        "WARNING" { "Yellow" }
        "STEP"    { "Magenta" }
        default   { "Cyan" }
    }
    Write-Host "[$Status] $Message" -ForegroundColor $color
}

function Confirm-Action {
    param([string]$Message)
    Write-Host ""
    Write-Host $Message -ForegroundColor Yellow
    $response = Read-Host "Type 'YES' to continue"
    return ($response -ceq "YES")
}

# ============================================================================
# BANNER
# ============================================================================
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Red
Write-Host "║   PRODUCTION DATABASE DEPLOYMENT                        ║" -ForegroundColor Red
Write-Host "║   Target: $ProductionServer".PadRight(59) + "║" -ForegroundColor Red
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Red
Write-Host ""
Write-Status "Source:      $LocalServer / $DatabaseName"
Write-Status "Destination: $ProductionServer\$ProductionInstance / $DatabaseName"
Write-Status "Dry Run:     $DryRun"
Write-Host ""

if (-not $DryRun) {
    if (-not (Confirm-Action "⚠️  WARNING: This will OVERWRITE the production database!")) {
        Write-Status "Deployment cancelled by user" "WARNING"
        exit 0
    }
}

# ============================================================================
# STEP 1: Backup local database
# ============================================================================
Write-Status "=== STEP 1: Backup Local Database ===" "STEP"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$localBackupDir = Join-Path $PSScriptRoot "Backups"
$backupFileName = "$($DatabaseName)_DEPLOY_$timestamp.bak"
$localBackupPath = Join-Path $localBackupDir $backupFileName

if (-not (Test-Path $localBackupDir)) {
    New-Item -ItemType Directory -Path $localBackupDir -Force | Out-Null
}

if ($SkipBackup) {
    Write-Status "Skipping backup (--SkipBackup set)" "WARNING"
}
else {
    if ($DryRun) {
        Write-Status "[DRY RUN] Would backup $DatabaseName to $localBackupPath"
    }
    else {
        Write-Status "Backing up local $DatabaseName..."
        $backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$localBackupPath'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
"@
        try {
            Invoke-Sqlcmd -ServerInstance $LocalServer -Query $backupQuery -QueryTimeout 600 -ErrorAction Stop
            $size = (Get-Item $localBackupPath).Length / 1MB
            Write-Status ("Backup created ({0:N2} MB)" -f $size) "SUCCESS"
        }
        catch {
            Write-Status "Local backup failed: $_" "ERROR"
            exit 1
        }
    }
}

# ============================================================================
# STEP 2: Test connection to production
# ============================================================================
Write-Status "=== STEP 2: Test Production Connection ===" "STEP"

$prodConnStr = "Server=$ProductionServer\$ProductionInstance;Database=master;User Id=$SqlUser;Password=$SqlPassword;Encrypt=yes;TrustServerCertificate=yes;Connection Timeout=30;"

if ($DryRun) {
    Write-Status "[DRY RUN] Would test connection to $ProductionServer\$ProductionInstance"
}
else {
    Write-Status "Testing connection to $ProductionServer\$ProductionInstance..."
    try {
        $versionQuery = "SELECT @@VERSION AS Version, @@SERVERNAME AS ServerName"
        $result = Invoke-Sqlcmd -ConnectionString $prodConnStr -Query $versionQuery -ErrorAction Stop
        Write-Status "Connected to: $($result.ServerName)" "SUCCESS"
        
        # Verify SQL Server 2016+
        $versionCheck = Invoke-Sqlcmd -ConnectionString $prodConnStr -Query "SELECT SERVERPROPERTY('ProductMajorVersion') AS Major"
        if ($versionCheck.Major -lt 13) {
            Write-Status "Production is older than SQL 2016 - may have compatibility issues" "WARNING"
        }
        else {
            Write-Status "SQL Server version check passed (Major: $($versionCheck.Major))" "SUCCESS"
        }
    }
    catch {
        Write-Status "Cannot connect to production: $_" "ERROR"
        Write-Status "Verify:" "WARNING"
        Write-Status "  - Production server is reachable" "WARNING"
        Write-Status "  - SQL Server instance name is correct ($ProductionInstance)" "WARNING"
        Write-Status "  - Credentials are correct ($SqlUser)" "WARNING"
        Write-Status "  - Firewall allows SQL Server (port 1433)" "WARNING"
        exit 1
    }
}

# ============================================================================
# STEP 3: Manual file transfer to production
# ============================================================================
Write-Status "=== STEP 3: Transfer Backup to Production (MANUAL) ===" "STEP"

$restorePath = Join-Path $RemoteBackupPath $backupFileName

if ($DryRun) {
    Write-Status "[DRY RUN] Would prompt for manual file copy to $ProductionServer"
}
else {
    Write-Host ""
    Write-Host "┌────────────────────────────────────────────────────────────┐" -ForegroundColor Yellow
    Write-Host "│  MANUAL FILE TRANSFER REQUIRED                             │" -ForegroundColor Yellow
    Write-Host "└────────────────────────────────────────────────────────────┘" -ForegroundColor Yellow
    Write-Host ""
    Write-Status "Copy the following backup file to the production server:" "WARNING"
    Write-Host ""
    Write-Host "  SOURCE (your local machine):" -ForegroundColor White
    Write-Host "    $localBackupPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "  TARGET (on $ProductionServer):" -ForegroundColor White
    Write-Host "    $restorePath" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Suggested methods:" -ForegroundColor Cyan
    Write-Host "    - RDP: Open RDP to $ProductionServer, drag file in clipboard" -ForegroundColor Gray
    Write-Host "    - SCP/SFTP: Use WinSCP or pscp" -ForegroundColor Gray
    Write-Host "    - Cloud: Upload to OneDrive/Dropbox, download on server" -ForegroundColor Gray
    Write-Host ""
    
    # Try to open file explorer at the backup location for convenience
    try {
        Start-Process "explorer.exe" "/select,`"$localBackupPath`"" -ErrorAction SilentlyContinue
    } catch { }
    
    if (-not (Confirm-Action "Have you copied '$backupFileName' to '$RemoteBackupPath' on production?")) {
        Write-Status "Deployment cancelled - awaiting file transfer" "WARNING"
        exit 0
    }
    
    # Verify the file exists on production via SQL Server's xp_fileexist
    Write-Status "Verifying backup file exists on production..."
    try {
        $verifyQuery = "EXEC xp_fileexist '$restorePath'"
        $fileCheck = Invoke-Sqlcmd -ConnectionString $prodConnStr -Query $verifyQuery -ErrorAction Stop
        # xp_fileexist returns "File Exists" as bit in first column
        $fileExists = $fileCheck.PSObject.Properties.Value[0]
        if ($fileExists -eq 1) {
            Write-Status "File verified on production: $restorePath" "SUCCESS"
        }
        else {
            Write-Status "File NOT found at: $restorePath" "ERROR"
            Write-Status "Please verify the path and re-run the script" "WARNING"
            exit 1
        }
    }
    catch {
        Write-Status "Could not verify file (requires sysadmin on production): $_" "WARNING"
        if (-not (Confirm-Action "Continue assuming file is in place?")) {
            exit 1
        }
    }
}

# ============================================================================
# STEP 4: Take production backup (safety net)
# ============================================================================
Write-Status "=== STEP 4: Backup Production Database (Safety Net) ===" "STEP"

if ($DryRun) {
    Write-Status "[DRY RUN] Would backup existing production database"
}
else {
    # Check if DB exists on production
    $checkDbQuery = "SELECT COUNT(*) AS Exists FROM sys.databases WHERE name = '$DatabaseName'"
    $dbExists = (Invoke-Sqlcmd -ConnectionString $prodConnStr -Query $checkDbQuery).Exists -gt 0
    
    if ($dbExists) {
        $safetyBackup = "$RemoteBackupPath\$($DatabaseName)_PREDEPLOY_$timestamp.bak"
        Write-Status "Backing up existing production DB to $safetyBackup..."
        $safetyBackupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$safetyBackup'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
"@
        try {
            Invoke-Sqlcmd -ConnectionString $prodConnStr -Query $safetyBackupQuery -QueryTimeout 600 -ErrorAction Stop
            Write-Status "Production safety backup created" "SUCCESS"
        }
        catch {
            Write-Status "Safety backup failed: $_" "WARNING"
            if (-not (Confirm-Action "Continue without safety backup?")) {
                exit 1
            }
        }
    }
    else {
        Write-Status "Database doesn't exist on production - skipping safety backup"
    }
}

# ============================================================================
# STEP 5: Restore on production
# ============================================================================
Write-Status "=== STEP 5: Restore Database on Production ===" "STEP"

if ($DryRun) {
    Write-Status "[DRY RUN] Would restore from $restorePath"
}
else {
    Write-Status "Setting database to SINGLE_USER mode and restoring..."
    
    $restoreQuery = @"
-- Kill all connections and restore
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '$DatabaseName')
BEGIN
    ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END

RESTORE DATABASE [$DatabaseName]
FROM DISK = N'$restorePath'
WITH REPLACE, RECOVERY, STATS = 10;

ALTER DATABASE [$DatabaseName] SET MULTI_USER;

-- Grant permissions to the SQL user
USE [$DatabaseName];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$SqlUser')
BEGIN
    CREATE USER [$SqlUser] FOR LOGIN [$SqlUser];
END
ALTER ROLE db_owner ADD MEMBER [$SqlUser];
"@
    
    try {
        Invoke-Sqlcmd -ConnectionString $prodConnStr -Query $restoreQuery -QueryTimeout 1800 -ErrorAction Stop
        Write-Status "Restore complete" "SUCCESS"
    }
    catch {
        Write-Status "Restore failed: $_" "ERROR"
        Write-Status "Production DB may be in inconsistent state - check immediately!" "ERROR"
        exit 1
    }
}

# ============================================================================
# STEP 6: Verify deployment
# ============================================================================
Write-Status "=== STEP 6: Verify Deployment ===" "STEP"

if ($DryRun) {
    Write-Status "[DRY RUN] Would verify deployment"
}
else {
    $verifyConnStr = "Server=$ProductionServer\$ProductionInstance;Database=$DatabaseName;User Id=$SqlUser;Password=$SqlPassword;Encrypt=yes;TrustServerCertificate=yes;"
    
    try {
        $tableCount = Invoke-Sqlcmd -ConnectionString $verifyConnStr -Query "SELECT COUNT(*) AS Count FROM sys.tables"
        Write-Status "Tables in production: $($tableCount.Count)" "SUCCESS"
        
        # Show top 5 tables with row counts
        Write-Status "Top 5 tables by row count:"
        $topTables = Invoke-Sqlcmd -ConnectionString $verifyConnStr -Query @"
SELECT TOP 5 
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY RowCount DESC;
"@
        foreach ($row in $topTables) {
            Write-Status "  $($row.TableName): $($row.RowCount) rows"
        }
    }
    catch {
        Write-Status "Verification failed: $_" "WARNING"
    }
}

# ============================================================================
# DONE
# ============================================================================
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   DEPLOYMENT COMPLETE                                    ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Status "Production: $ProductionServer\$ProductionInstance"
Write-Status "Database:   $DatabaseName"
Write-Status "Backup:     $localBackupPath"
Write-Host ""
