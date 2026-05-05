[CmdletBinding()]
param(
    [string]$Command = ''
)

$ErrorActionPreference = 'Continue'

# ── Paths ──────────────────────────────────────────────────────────
$Root   = $PSScriptRoot
$Sln    = Join-Path $Root 'MyDesk.slnx'
$Src    = Join-Path $Root 'src'
$Web    = Join-Path $Src 'MyDesk.Web'
$Shared = Join-Path $Src 'MyDesk.Shared'
$Tests  = Join-Path $Root 'tests\MyDesk.PlaywrightTests'
$Deploy = Join-Path $Src 'Deployment'
$Publish = Join-Path $Root 'publish'
$Logs   = Join-Path $Web 'Logs'
$Docs   = Join-Path $Root 'docs'
$Migrations = Join-Path $Deploy 'Migration'
$Port   = 5237
$Url    = "http://localhost:$Port"

# ── Helpers ────────────────────────────────────────────────────────
function Write-Banner {
    Write-Host ''
    Write-Host '  MyDesk' -ForegroundColor Cyan
    Write-Host '  Business Management Platform' -ForegroundColor DarkGray
}

function Write-Section($Label) {
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor DarkGray
    Write-Host "  $Label" -ForegroundColor Yellow
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor DarkGray
}

function Stop-MyDeskInstances {
    Write-Host 'Stopping existing MyDesk instances ...'
    Stop-PortProcess -Port $Port
    $procIds = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'MyDesk\.Web' } |
        Select-Object -ExpandProperty ProcessId -Unique
    foreach ($procId in $procIds) {
        Write-Host "  - Killing MyDesk dotnet PID $procId"
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-PortProcess {
    param([int]$Port)
    Write-Host "Checking for processes on port $Port ..."
    $conns = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $conns) {
        Write-Host "  - Killing PID $procId"
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
}

function Wait-ForServer {
    param([int]$TimeoutSeconds = 60)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt ($TimeoutSeconds * 1000)) {
        try {
            $r = Invoke-WebRequest -Uri "$Url/login" -UseBasicParsing -TimeoutSec 2
            if ($r.StatusCode -eq 200) {
                Write-Host '[+] Server is ready.'
                return $true
            }
        } catch { }
        Start-Sleep -Seconds 2
    }
    Write-Host '[!] Server did not become ready within timeout.'
    return $false
}

# ── Menu options ───────────────────────────────────────────────────

function Opt-Database {
    Write-Section 'Database Migrations'
    Write-Host "Applying SQL migrations from $Migrations..."
    if (-not (Test-Path $Migrations)) {
        Write-Host "[!] Migrations folder not found: $Migrations" -ForegroundColor Red
        return
    }
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        Write-Host '[!] sqlcmd not found in PATH. Install SQL Server command-line tools.' -ForegroundColor Red
        return
    }
    $DbServer = Read-Host 'Server [(localdb)\MSSQLLocalDB]'
    if (-not $DbServer) { $DbServer = '(localdb)\MSSQLLocalDB' }
    $DbName = Read-Host 'Database [Techlight_MyDesk]'
    if (-not $DbName) { $DbName = 'Techlight_MyDesk' }

    Get-ChildItem "$Migrations\*.sql" | Sort-Object Name | ForEach-Object {
        Write-Host "  - Running $($_.Name)"
        sqlcmd -S $DbServer -d $DbName -i $_.FullName -b
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[!] Migration $($_.Name) FAILED" -ForegroundColor Red
            return
        }
    }
    Write-Host '[+] All migrations applied successfully.' -ForegroundColor Green
}

function Opt-IIS {
    Write-Section 'IIS Deployment'
    Stop-MyDeskInstances
    $installer = Join-Path $Deploy 'install.ps1'
    if (-not (Test-Path $installer)) {
        Write-Host "[!] IIS installer script not found: $installer" -ForegroundColor Red
        return
    }
    Write-Host 'Deploying to local IIS on port 80...'
    Write-Host '  Site:     MyDesk'
    Write-Host '  AppPool:  MyDesk'
    Write-Host '  Path:     C:\inetpub\wwwroot\MyDesk'
    Write-Host '  URL:      http://localhost'
    Write-Host ''
    & $installer
    if ($LASTEXITCODE -ne 0) {
        Write-Host '[!] IIS deployment FAILED' -ForegroundColor Red
        return
    }
    Write-Host '[+] IIS deployment completed.' -ForegroundColor Green
}

function Opt-CleanBuild {
    Write-Section 'Clean and Build'
    Write-Host 'Cleaning bin/obj folders...'
    Get-ChildItem -Path $Root -Directory -Recurse -Include bin,obj |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Restoring + Building solution...'
    dotnet build $Sln --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host '[!] Build FAILED' -ForegroundColor Red
        return
    }
    Write-Host '[+] Build succeeded.' -ForegroundColor Green
}

function Opt-Run {
    Write-Section 'Launch Kestrel'
    Stop-MyDeskInstances
    Start-Sleep -Seconds 3
    Write-Host "Starting Kestrel server on $Url ..."
    Write-Host '(Press Ctrl+C in this window to stop)'
    Write-Host ''
    dotnet run --project "$Web\MyDesk.Web.csproj" --no-launch-profile --urls $Url
}

function Opt-Tests {
    Write-Section 'Playwright Tests'
    Stop-PortProcess -Port $Port
    Write-Host 'Starting Kestrel server in background for tests...'
    $job = Start-Job -ScriptBlock {
        param($WebProj, $Url)
        dotnet run --project $WebProj --no-launch-profile --urls $Url
    } -ArgumentList "$Web\MyDesk.Web.csproj", $Url

    if (-not (Wait-ForServer -TimeoutSeconds 60)) {
        Stop-Job $job -ErrorAction SilentlyContinue
        Remove-Job $job -Force -ErrorAction SilentlyContinue
        Stop-PortProcess -Port $Port
        Write-Host '[!] Server startup failed, aborting tests.' -ForegroundColor Red
        return
    }

    Write-Host 'Running Playwright tests...'
    Push-Location $Tests
    dotnet test --nologo --logger 'console;verbosity=normal'
    $testExit = $LASTEXITCODE
    Pop-Location

    Write-Host ''
    Write-Host 'Stopping background test server...'
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -Force -ErrorAction SilentlyContinue
    Stop-PortProcess -Port $Port

    if ($testExit -ne 0) {
        Write-Host "[!] One or more tests FAILED. Exit code $testExit." -ForegroundColor Red
    } else {
        Write-Host '[+] All tests passed.' -ForegroundColor Green
    }
}

function Opt-Status {
    Write-Section 'System Status'
    Write-Host "== Server Port ($Port) =="
    netstat -ano | Select-String ":$Port"
    Write-Host ''
    Write-Host '== .NET SDKs =='
    dotnet --list-sdks
    Write-Host ''
    Write-Host '== Solution health =='
    if (Test-Path $Sln) { Write-Host '  [OK] MyDesk.slnx' -ForegroundColor Green }
    else { Write-Host '  [!!] MyDesk.slnx missing' -ForegroundColor Red }
    if (Test-Path "$Web\MyDesk.Web.csproj") { Write-Host '  [OK] Web project' -ForegroundColor Green }
    else { Write-Host '  [!!] Web project missing' -ForegroundColor Red }
    if (Test-Path "$Tests\MyDesk.PlaywrightTests.csproj") { Write-Host '  [OK] Tests project' -ForegroundColor Green }
    else { Write-Host '  [!!] Tests project missing' -ForegroundColor Red }
    Write-Host ''
}

function Opt-Logs {
    Write-Section 'Open Logs'
    if (-not (Test-Path $Logs)) { New-Item -ItemType Directory -Path $Logs -Force | Out-Null }
    explorer.exe $Logs
}

function Opt-Docs {
    Write-Section 'Open Docs'
    if (-not (Test-Path $Docs)) { New-Item -ItemType Directory -Path $Docs -Force | Out-Null }
    explorer.exe $Docs
}

function Opt-Agents {
    Write-Section 'Open Agent Guide'
    Write-Host 'Opening AGENTS.md (autonomous developer guide)...'
    Start-Process "$Root\AGENTS.md"
}

function Opt-Stop {
    Write-Section 'Stop Kestrel'
    Stop-MyDeskInstances
    Write-Host '[+] Server stopped.' -ForegroundColor Green
}

# ── Dispatch ───────────────────────────────────────────────────────
$dispatch = @{
    '1' = 'Opt-Database'
    '2' = 'Opt-IIS'
    '3' = 'Opt-CleanBuild'
    '4' = 'Opt-Run'
    '5' = 'Opt-Tests'
    '6' = 'Opt-Status'
    '7' = 'Opt-Logs'
    '8' = 'Opt-Docs'
    '9' = 'Opt-Stop'
    '0' = 'Opt-Agents'
}

if ($Command -and $dispatch.ContainsKey($Command)) {
    & $dispatch[$Command]
    if ($Command -eq '4') { return }
    return
}
if ($Command -eq 'Q') { return }

# ── Interactive menu ───────────────────────────────────────────────
while ($true) {
    Clear-Host
    Write-Banner
    Write-Host ''
    Write-Host '  Environment'
    Write-Host '  -----------'
    Write-Host "    Root:   $Root"
    Write-Host "    Web:    $Web"
    Write-Host "    URL:    $Url"
    Write-Host '    SQL:    Dev=(localdb)\MSSQLLocalDB  IIS=(localdb)\.\MyDeskShared'
    Write-Host ''
    Write-Host '  Build & Deploy'
    Write-Host '  --------------'
    Write-Host '    [1]  Database      Apply one-shot SQL migrations from src\Deployment\Migration'
    Write-Host '    [2]  IIS Deploy    Publish and deploy to local IIS (http://localhost, port 80)'
    Write-Host '    [3]  Clean & Build Clean bin/obj and rebuild solution'
    Write-Host "    [4]  Launch        Run Kestrel locally at $Url"
    Write-Host '    [5]  Playwright    Run E2E tests (auto-starts Kestrel)'
    Write-Host ''
    Write-Host '  Tools'
    Write-Host '  -----'
    Write-Host '    [6]  Status        Show ports, SDKs, and solution health'
    Write-Host '    [7]  Logs          Open log folder'
    Write-Host '    [8]  Docs          Open documentation folder'
    Write-Host "    [9]  Stop          Stop server on port $Port"
    Write-Host '    [0]  Agent Guide   Open AGENTS.md'
    Write-Host ''
    Write-Host '  Exit'
    Write-Host '  ----'
    Write-Host '    [Q]  Return to shell (do not close this window)'
    Write-Host ''
    Write-Host '  Tip: You can also run directly, e.g.  Run.bat 4   or   Run.bat 5'
    Write-Host ''

    $choice = Read-Host '   Choose an option'

    if ($dispatch.ContainsKey($choice)) {
        & $dispatch[$choice]
        if ($choice -eq '4') { return }
        $null = Read-Host '`n  Press Enter to continue'
    }
    elseif ($choice -eq 'Q') {
        return
    }
}
