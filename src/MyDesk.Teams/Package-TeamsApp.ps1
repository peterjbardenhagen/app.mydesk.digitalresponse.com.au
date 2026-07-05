<#
.SYNOPSIS
    Packages the MyDesk Teams app into distributable ZIP files using Python (RFC-compliant ZIPs).

.DESCRIPTION
    Uses Python's zipfile module to create fully compliant ZIP files that Teams portal accepts.
    PowerShell's Compress-Archive is known to produce incompatible ZIPs that Teams rejects
    with "We can't read the manifest file".

    Creates packages:
      mydesk-teams-copilot.zip       — Production with M365 Copilot agent
      mydesk-teams-basic.zip          — Production tabs + bot only
      mydesk-teams-dev-copilot.zip    — Development with M365 Copilot agent
      mydesk-teams-dev-basic.zip      — Development tabs + bot only

.EXAMPLE
    .\Package-TeamsApp.ps1
#>

$ErrorActionPreference = "Stop"
$HERE = $PSScriptRoot

function Write-Step($msg) { Write-Host "[TEAMS] $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  [OK] $msg"   -ForegroundColor Green }

# Detect Python
$python = if (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" }
          elseif (Get-Command python -ErrorAction SilentlyContinue) { "python" }
          else { throw "Python not found. Required for RFC-compliant ZIP creation." }

$script = @'
import json, zipfile, os, sys

HERE = sys.argv[1]
OUT = sys.argv[2]

def make_zip(name, files):
    path = os.path.join(OUT, name)
    with zipfile.ZipFile(path, 'w', zipfile.ZIP_DEFLATED) as zf:
        for src, arcname in files:
            zf.write(os.path.join(HERE, src), arcname)
    z = zipfile.ZipFile(path)
    z.testzip()
    manifest = json.loads(z.read('manifest.json'))
    print(f'  [OK] {name} → {manifest["name"]["short"]} v{manifest["version"]}')
    z.close()

# Prod Copilot
make_zip('mydesk-teams-copilot.zip', [
    ('manifest.json','manifest.json'), ('declarativeAgent.json','declarativeAgent.json'),
    ('ai-plugin.json','ai-plugin.json'), ('color.png','color.png'), ('outline.png','outline.png')])

# Prod Basic
make_zip('mydesk-teams-basic.zip', [
    ('manifest.json','manifest.json'), ('color.png','color.png'), ('outline.png','outline.png')])

# Dev Copilot
make_zip('mydesk-teams-dev-copilot.zip', [
    ('manifest-dev.json','manifest.json'), ('declarativeAgent-dev.json','declarativeAgent.json'),
    ('ai-plugin-dev.json','ai-plugin.json'), ('color.png','color.png'), ('outline.png','outline.png')])

# Dev Basic
make_zip('mydesk-teams-dev-basic.zip', [
    ('manifest-dev.json','manifest.json'), ('color.png','color.png'), ('outline.png','outline.png')])
'@

Write-Step "Building 4 Teams app packages using Python..."
$result = & $python -c $script $HERE (Resolve-Path (Join-Path $HERE ".."))
if ($LASTEXITCODE -eq 0) {
    Write-Host $result
    Write-Host ""
    Write-Host "Done! Upload to Teams Admin Centre or Developer Portal:" -ForegroundColor Yellow
    Write-Host "  Production with M365 Copilot  → mydesk-teams-copilot.zip" -ForegroundColor White
    Write-Host "  Production without M365 Copilot → mydesk-teams-basic.zip"  -ForegroundColor White
    Write-Host "  Development with M365 Copilot  → mydesk-teams-dev-copilot.zip" -ForegroundColor White
    Write-Host "  Development without M365 Copilot → mydesk-teams-dev-basic.zip"  -ForegroundColor White
} else {
    Write-Host "  [ERROR] Packaging failed: $result" -ForegroundColor Red
    exit 1
}