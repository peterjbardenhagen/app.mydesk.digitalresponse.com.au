<#
.SYNOPSIS
    Packages the MyDesk Teams app into distributable ZIP files.

.DESCRIPTION
    Creates packages for different environments:
      mydesk-teams-copilot.zip  — Production with M365 Copilot agent
      mydesk-teams-basic.zip    — Production tabs + bot only (no Copilot licence needed)
      mydesk-teams-dev-copilot.zip  — Development with M365 Copilot agent
      mydesk-teams-dev-basic.zip    — Development tabs + bot only (no Copilot licence needed)

.EXAMPLE
    .\Package-TeamsApp.ps1
#>

$ErrorActionPreference = "Stop"
$HERE = $PSScriptRoot

function Write-Step($msg) { Write-Host "[TEAMS] $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  [OK] $msg"   -ForegroundColor Green }

$files = @(
    Join-Path $HERE "color.png"
    Join-Path $HERE "outline.png"
)

# ── Package 1: Production with Copilot ───────────────────────────────────────────────────
Write-Step "Building mydesk-teams-copilot.zip (Production, includes M365 Copilot agent)..."
$copilotZip = Join-Path $HERE "..\mydesk-teams-copilot.zip"
if (Test-Path $copilotZip) { Remove-Item $copilotZip -Force }

$tmp = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "mydesk-teams-copilot") -Force
Copy-Item (Join-Path $HERE "manifest.json") $tmp
Copy-Item (Join-Path $HERE "declarativeAgent.json") $tmp
Copy-Item (Join-Path $HERE "ai-plugin.json") $tmp
Copy-Item (Join-Path $HERE "color.png") $tmp
Copy-Item (Join-Path $HERE "outline.png") $tmp

Compress-Archive -Path "$tmp\*" -DestinationPath $copilotZip -Force
Remove-Item $tmp -Recurse -Force
Write-OK "Created: $(Resolve-Path $copilotZip)"

# ── Package 2: Production Basic (no Copilot) ────────────────────────────────────────────
Write-Step "Building mydesk-teams-basic.zip (Production, tabs + bot, no Copilot required)..."
$basicZip = Join-Path $HERE "..\mydesk-teams-basic.zip"
if (Test-Path $basicZip) { Remove-Item $basicZip -Force }

$tmp2 = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "mydesk-teams-basic") -Force
Copy-Item (Join-Path $HERE "manifest-basic.json") (Join-Path $tmp2 "manifest.json")
Copy-Item (Join-Path $HERE "color.png") $tmp2
Copy-Item (Join-Path $HERE "outline.png") $tmp2

Compress-Archive -Path "$tmp2\*" -DestinationPath $basicZip -Force
Remove-Item $tmp2 -Recurse -Force
Write-OK "Created: $(Resolve-Path $basicZip)"

# ── Package 3: Development with Copilot ────────────────────────────────────────────────
Write-Step "Building mydesk-teams-dev-copilot.zip (Development, includes M365 Copilot agent)..."
$devCopilotZip = Join-Path $HERE "..\mydesk-teams-dev-copilot.zip"
if (Test-Path $devCopilotZip) { Remove-Item $devCopilotZip -Force }

$tmp3 = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "mydesk-teams-dev-copilot") -Force
Copy-Item (Join-Path $HERE "manifest-dev.json") (Join-Path $tmp3 "manifest.json")
Copy-Item (Join-Path $HERE "declarativeAgent-dev.json") $tmp3
Copy-Item (Join-Path $HERE "ai-plugin-dev.json") $tmp3
Copy-Item (Join-Path $HERE "color.png") $tmp3
Copy-Item (Join-Path $HERE "outline.png") $tmp3

Compress-Archive -Path "$tmp3\*" -DestinationPath $devCopilotZip -Force
Remove-Item $tmp3 -Recurse -Force
Write-OK "Created: $(Resolve-Path $devCopilotZip)"

# ── Package 4: Development Basic (no Copilot) ──────────────────────────────────────────
Write-Step "Building mydesk-teams-dev-basic.zip (Development, tabs + bot, no Copilot required)..."
$devBasicZip = Join-Path $HERE "..\mydesk-teams-dev-basic.zip"
if (Test-Path $devBasicZip) { Remove-Item $devBasicZip -Force }

$tmp4 = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "mydesk-teams-dev-basic") -Force
Copy-Item (Join-Path $HERE "manifest-dev.json") (Join-Path $tmp4 "manifest.json")
Copy-Item (Join-Path $HERE "color.png") $tmp4
Copy-Item (Join-Path $HERE "outline.png") $tmp4

Compress-Archive -Path "$tmp4\*" -DestinationPath $devBasicZip -Force
Remove-Item $tmp4 -Recurse -Force
Write-OK "Created: $(Resolve-Path $devBasicZip)"

Write-Host ""
Write-Host "Done! Upload to Teams Admin Centre or Developer Portal:" -ForegroundColor Yellow
Write-Host "  Production with M365 Copilot  → mydesk-teams-copilot.zip" -ForegroundColor White
Write-Host "  Production without M365 Copilot → mydesk-teams-basic.zip"  -ForegroundColor White
Write-Host "  Development with M365 Copilot  → mydesk-teams-dev-copilot.zip" -ForegroundColor White
Write-Host "  Development without M365 Copilot → mydesk-teams-dev-basic.zip"  -ForegroundColor White