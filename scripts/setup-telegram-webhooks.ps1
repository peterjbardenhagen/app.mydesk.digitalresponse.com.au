<#
.SYNOPSIS
    Automated Telegram webhook setup for MyDesk multi-environment bots.

.DESCRIPTION
    Configures webhooks for production and development Telegram bots.
    Reads bot tokens from environment variables or prompts interactively.
    Supports both production (mydesk.digitalresponse.com.au) and development (dev.digitalresponse.com.au) environments.

.PARAMETER ProdToken
    Production bot token from @BotFather

.PARAMETER DevToken
    Development bot token from @BotFather

.PARAMETER ProdUrl
    Production base URL (default: https://mydesk.digitalresponse.com.au)

.PARAMETER DevUrl
    Development base URL (default: https://dev.digitalresponse.com.au)

.PARAMETER SkipDev
    Skip development webhook setup

.EXAMPLE
    .\scripts\setup-telegram-webhooks.ps1 -ProdToken "123:ABC" -DevToken "456:XYZ"

.EXAMPLE
    .\scripts\setup-telegram-webhooks.ps1 -ProdToken "123:ABC"

.EXAMPLE
    $env:TELEGRAM_PROD_TOKEN="123:ABC"; $env:TELEGRAM_DEV_TOKEN="456:XYZ"
    .\scripts\setup-telegram-webhooks.ps1
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
    Write-Step "Setting $env webhook to: $webhookUrl"
    
    try {
        $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/setWebhook?url=$webhookUrl" -Method Post -UseBasicParsing
        if ($response.ok) {
            Write-OK "$env webhook set successfully"
            return $true
        } else {
            Write-Error "$env webhook failed: $($response.description)"
            return $false
        }
    } catch {
        Write-Error "$env webhook exception: $($_.Exception.Message)"
        return $false
    }
}

function Get-WebhookInfo($token, $env) {
    try {
        $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/getWebhookInfo" -Method Get -UseBasicParsing
        if ($response.ok) {
            $result = $response.result
            Write-Host "  URL: $($result.url)" -ForegroundColor Gray
            Write-Host "  Has Custom Certificate: $($result.has_custom_certificate)" -ForegroundColor Gray
            Write-Host "  Pending Updates: $($result.pending_update_count)" -ForegroundColor Gray
            Write-Host "  Last Error: $($result.last_error_message)" -ForegroundColor Gray
            Write-Host "  Last Error Date: $([DateTime]::FromFileTime($result.last_error_date * 10000000 + 116444736000000000))" -ForegroundColor Gray
        }
    } catch {
        Write-Warn "Could not get webhook info for $env: $($_.Exception.Message)"
    }
}

function Delete-Webhook($token, $env) {
    Write-Step "Deleting $env webhook"
    try {
        $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/deleteWebhook" -Method Post -UseBasicParsing
        if ($response.ok) {
            Write-OK "$env webhook deleted"
        } else {
            Write-Error "$env webhook delete failed: $($response.description)"
        }
    } catch {
        Write-Error "$env webhook delete exception: $($_.Exception.Message)"
    }
}

# ── Main ────────────────────────────────────────────────────────────────────────

Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  MyDesk Telegram Webhook Setup" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

# Prompt for tokens if not provided
if (-not $ProdToken) {
    $ProdToken = Read-Host "Enter PRODUCTION bot token (from @BotFather)"
    if (-not $ProdToken) {
        Write-Error "Production token is required"
        exit 1
    }
}

if (-not $DevToken -and -not $SkipDev) {
    $DevToken = Read-Host "Enter DEVELOPMENT bot token (from @BotFather) [optional]"
}

$success = $true

# Production webhook
Write-Step "Configuring PRODUCTION bot..."
if (Set-Webhook $ProdToken $ProdUrl "prod") {
    Get-WebhookInfo $ProdToken "prod"
} else {
    $success = $false
}

# Development webhook
if (-not $SkipDev -and $DevToken) {
    Write-Step "Configuring DEVELOPMENT bot..."
    if (Set-Webhook $DevToken $DevUrl "dev") {
        Get-WebhookInfo $DevToken "dev"
    } else {
        $success = $false
    }
} elseif ($SkipDev) {
    Write-Warn "Skipping development webhook (--SkipDev specified)"
} else {
    Write-Warn "No development token provided, skipping dev webhook"
}

Write-Host "`n========================================" -ForegroundColor Magenta
if ($success) {
    Write-Host "  All webhooks configured successfully!" -ForegroundColor Green
} else {
    Write-Host "  Some webhooks failed - check errors above" -ForegroundColor Red
}
Write-Host "============================================`n" -ForegroundColor Magenta

exit ($success ? 0 : 1)