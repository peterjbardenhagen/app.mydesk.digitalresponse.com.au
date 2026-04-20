<#
.SYNOPSIS
    Backs up the local Techlight_MyDesk SQL Server database to a .bak file
.DESCRIPTION
    Creates a timestamped full backup of the local LocalDB Techlight_MyDesk database.
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

# Test connection
Write-Status "Testing connection..."
try {
    $testQuery = "SELECT name FROM sys.databases WHERE name = '$DatabaseName'"
    $result = Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $testQuery -ErrorAction Stop
    if (-not $result) {
        Write-Status "Database '$DatabaseName' not found on $ServerInstance" "ERROR"
        exit 1
    }
    Write-Status "Connection OK" "SUCCESS"
}
catch {
    Write-Status "Cannot connect to $ServerInstance : $_" "ERROR"
    Write-Status "Ensure LocalDB is running: sqllocaldb start MSSQLLocalDB" "WARNING"
    exit 1
}

# Run backup
Write-Status "Running backup..."
$backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$backupFile'
WITH FORMAT,
     INIT,
     NAME = N'$DatabaseName-Full Backup $timestamp',
     SKIP,
     COMPRESSION,
     STATS = 10;
"@

try {
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $backupQuery -QueryTimeout 600 -ErrorAction Stop
    $backupSize = (Get-Item $backupFile).Length / 1MB
    Write-Status ("Backup complete ({0:N2} MB)" -f $backupSize) "SUCCESS"
}
catch {
    Write-Status "Backup failed: $_" "ERROR"
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
