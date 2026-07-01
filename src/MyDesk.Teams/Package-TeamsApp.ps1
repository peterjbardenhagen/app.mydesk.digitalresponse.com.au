<#
.SYNOPSIS
    Packages the MyDesk Teams app into distributable ZIP files.

.DESCRIPTION
    Creates two packages:
      mydesk-teams-copilot.zip  — includes copilotAgents (M365 Copilot required)
      mydesk-teams-basic.zip    — tabs + bot only (no Copilot licence needed)

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
    Join-Path $HERE "declarativeAgent.json"
    Join-Path $HERE "ai-plugin.json"
)

# ── Package 1: With Copilot ───────────────────────────────────────────────────
Write-Step "Building mydesk-teams-copilot.zip (includes M365 Copilot agent)..."
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

# ── Package 2: Basic (no Copilot) ────────────────────────────────────────────
Write-Step "Building mydesk-teams-basic.zip (tabs + bot, no Copilot required)..."
$basicZip = Join-Path $HERE "..\mydesk-teams-basic.zip"
if (Test-Path $basicZip) { Remove-Item $basicZip -Force }

$tmp2 = New-Item -ItemType Directory -Path (Join-Path $env:TEMP "mydesk-teams-basic") -Force
Copy-Item (Join-Path $HERE "manifest-basic.json") (Join-Path $tmp2 "manifest.json")
Copy-Item (Join-Path $HERE "color.png") $tmp2
Copy-Item (Join-Path $HERE "outline.png") $tmp2

Compress-Archive -Path "$tmp2\*" -DestinationPath $basicZip -Force
Remove-Item $tmp2 -Recurse -Force
Write-OK "Created: $(Resolve-Path $basicZip)"

Write-Host ""
Write-Host "Done! Upload to Teams Admin Centre or Developer Portal:" -ForegroundColor Yellow
Write-Host "  With M365 Copilot  → mydesk-teams-copilot.zip" -ForegroundColor White
Write-Host "  Without M365 Copilot → mydesk-teams-basic.zip"  -ForegroundColor White
