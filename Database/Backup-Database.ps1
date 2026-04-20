<#
.SYNOPSIS
    Backs up the local Techlight_MyDesk SQL Server database to a .bak file
.DESCRIPTION
    Creates a timestamped full backup of the local LocalDB Techlight_MyDesk database.
    Uses System.Data.SqlClient directly (no SqlServer PowerShell module required).
    Backups are stored in Database/Backups/ with format: Techlight_MyDesk_YYYYMMDD_HHMMSS.bak
.PARAMETER BackupPath
    Optional. Override default backup directory.
.PARAMETER Compress
    Optional. Compress the .bak into a .zip afterwards.
.EXAMPLE
    .\Backup-Database.ps1
    .\Backup-Database.ps1 -Compress
#>

param(
    [string]$BackupPath = "$PSScriptRoot\Backups",
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "Techlight_MyDesk",
    [switch]$Compress
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

function Invoke-SqlQuery {
    <#
    .SYNOPSIS
        Execute a SQL query using System.Data.SqlClient (no module required)
    .PARAMETER ConnectionString
        Full SQL Server connection string
    .PARAMETER Query
        SQL query to execute
    .PARAMETER Timeout
        Command timeout in seconds (default 30)
    .PARAMETER ReturnScalar
        If set, returns a single scalar value
    #>
    param(
        [Parameter(Mandatory)][string]$ConnectionString,
        [Parameter(Mandatory)][string]$Query,
        [int]$Timeout = 30,
        [switch]$ReturnScalar
    )
    
    Add-Type -AssemblyName "System.Data" -ErrorAction SilentlyContinue
    
    $conn = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Query
        $cmd.CommandTimeout = $Timeout
        
        if ($ReturnScalar) {
            return $cmd.ExecuteScalar()
        }
        
        # For queries that return rows, use a DataTable
        $reader = $cmd.ExecuteReader()
        $table = New-Object System.Data.DataTable
        $table.Load($reader)
        $reader.Close()
        return $table
    }
    finally {
        if ($conn.State -eq 'Open') { $conn.Close() }
        $conn.Dispose()
    }
}

# Ensure backup directory exists
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Status "Created backup directory: $BackupPath"
}

# Generate timestamped filename
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "$($DatabaseName)_$timestamp.bak"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Techlight_MyDesk Database Backup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Status "Server:   $ServerInstance"
Write-Status "Database: $DatabaseName"
Write-Status "Target:   $backupFile"
Write-Host ""

# Connection string - connect to master for backup
$connStr = "Server=$ServerInstance;Database=master;Integrated Security=True;Connection Timeout=30;"

# Test connection and verify database exists
Write-Status "Testing connection..."
try {
    $testQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DatabaseName'"
    $dbCount = Invoke-SqlQuery -ConnectionString $connStr -Query $testQuery -ReturnScalar
    
    if ($dbCount -eq 0) {
        Write-Status "Database '$DatabaseName' not found on $ServerInstance" "ERROR"
        Write-Status "Available databases:" "WARNING"
        $dbList = Invoke-SqlQuery -ConnectionString $connStr -Query "SELECT name FROM sys.databases ORDER BY name"
        foreach ($row in $dbList) {
            Write-Host "    $($row.name)" -ForegroundColor Gray
        }
        exit 1
    }
    Write-Status "Connection OK - database '$DatabaseName' found" "SUCCESS"
}
catch {
    Write-Status "Cannot connect to $ServerInstance" "ERROR"
    Write-Status "Error: $($_.Exception.Message)" "ERROR"
    Write-Status "Ensure LocalDB is running: sqllocaldb start MSSQLLocalDB" "WARNING"
    exit 1
}

# Run backup (LocalDB doesn't support COMPRESSION, omit it)
Write-Status "Running backup..."
$backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$backupFile'
WITH FORMAT,
     INIT,
     NAME = N'$DatabaseName-Full Backup $timestamp',
     SKIP,
     STATS = 10;
"@

try {
    Invoke-SqlQuery -ConnectionString $connStr -Query $backupQuery -Timeout 600 | Out-Null
    
    if (-not (Test-Path $backupFile)) {
        Write-Status "Backup command succeeded but file not created at $backupFile" "ERROR"
        Write-Status "This may happen when running under different user context" "WARNING"
        exit 1
    }
    
    $backupSize = (Get-Item $backupFile).Length / 1MB
    Write-Status ("Backup complete ({0:N2} MB)" -f $backupSize) "SUCCESS"
}
catch {
    Write-Status "Backup failed: $($_.Exception.Message)" "ERROR"
    exit 1
}

# Optional compression
if ($Compress) {
    Write-Status "Compressing backup..."
    $zipFile = "$backupFile.zip"
    Compress-Archive -Path $backupFile -DestinationPath $zipFile -Force
    Remove-Item $backupFile
    $zipSize = (Get-Item $zipFile).Length / 1MB
    Write-Status ("Compressed to $zipFile ({0:N2} MB)" -f $zipSize) "SUCCESS"
    $backupFile = $zipFile
}

# Cleanup old backups (keep last 10)
Write-Status "Cleaning up old backups (keeping last 10)..."
$oldBackups = Get-ChildItem -Path $BackupPath -Filter "$($DatabaseName)_*.bak*" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 10

if ($oldBackups) {
    $oldBackups | ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Status "  Deleted: $($_.Name)"
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Status "Backup complete: $backupFile" "SUCCESS"
Write-Host "============================================" -ForegroundColor Green
