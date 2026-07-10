<#
.SYNOPSIS
    Automated Telegram webhook setup for MyDesk multi-environment bots.
#>
param(
    [Parameter(Mandatory=$false)][string]$ProdToken = $env:TELEGRAM_PROD_TOKEN,
    [Parameter(Mandatory=$false)][string]$DevToken = $env:TELEGRAM_DEV_TOKEN,
    [string]$ProdUrl = "https://mydesk.digitalresponse.com.au",
    [string]$DevUrl = "https://dev.digitalresponse.com.au",
    [switch]$SkipDev
)
$ErrorActionPreference = "Stop"
function Write-Step($msg) { Write-Host "[TELEGRAM] $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  [OK] $msg"   -ForegroundColor Green }
function Write-Error($msg) { Write-Host "  [ERROR] $msg" -ForegroundColor Red }
function Write-Warn($msg) { Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function Set-Webhook($token, $url, $env) {
    $webhookUrl = "$url/api/telegram/webhook/$env"
    Write-Step "Setting ${env} webhook to: $webhookUrl"
    try {
        $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/setWebhook?url=$webhookUrl" -Method Post -UseBasicParsing
        if ($response.ok) { Write-OK "${env} webhook set successfully"; return $true }
        else { Write-Error "${env} webhook failed: $($response.description)"; return $false }
    } catch { Write-Error "${env} webhook exception: $($_.Exception.Message)"; return $false }
}
function Get-WebhookInfo($token, $env) {
    try {
        $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/getWebhookInfo" -Method Get -UseBasicParsing
        if ($response.ok) {
            $r = $response.result
            Write-Host "  URL: $($r.url)" -ForegroundColor Gray
            Write-Host "  Has Custom Cert: $($r.has_custom_certificate)" -ForegroundColor Gray
            Write-Host "  Pending Updates: $($r.pending_update_count)" -ForegroundColor Gray
            Write-Host "  Last Error: $($r.last_error_message)" -ForegroundColor Gray
        }
    } catch { Write-Warn "Could not get webhook info for ${env}: $($_.Exception.Message)" }
}
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  MyDesk Telegram Webhook Setup" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta
if (-not $ProdToken) { $ProdToken = Read-Host "Enter PRODUCTION bot token"; if (-not $ProdToken) { Write-Error "Token required"; exit 1 } }
if (-not $DevToken -and -not $SkipDev) { $DevToken = Read-Host "Enter DEVELOPMENT bot token [optional]" }
$success = $true
Write-Step "Configuring PRODUCTION bot..."
if (Set-Webhook $ProdToken $ProdUrl "prod") { Get-WebhookInfo $ProdToken "prod" } else { $success = $false }
if (-not $SkipDev -and $DevToken) {
    Write-Step "Configuring DEVELOPMENT bot..."
    if (Set-Webhook $DevToken $DevUrl "dev") { Get-WebhookInfo $DevToken "dev" } else { $success = $false }
} elseif ($SkipDev) { Write-Warn "Skipping development webhook" } else { Write-Warn "No dev token provided, skipping" }
Write-Host "`n========================================" -ForegroundColor Magenta
if ($success) { Write-Host "  All webhooks configured successfully!" -ForegroundColor Green } else { Write-Host "  Some webhooks failed - check errors above" -ForegroundColor Red }
Write-Host "========================================`n" -ForegroundColor Magenta
exit ($success ? 0 : 1)