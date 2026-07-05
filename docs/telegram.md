# Telegram Bot Integration for MyDesk

This document describes how to configure and use the Telegram bot integration in MyDesk. The system supports **multiple bot environments** (prod, dev, staging, etc.) with per-tenant configuration.

---

## 🎯 Overview

MyDesk's Telegram bot provides real-time AI-powered business assistance directly in Telegram. Users can:

- Ask natural language questions about business data
- Send voice messages for transcription and processing
- Get morning business briefings
- Run MYOB reconciliations
- Check outstanding invoices/debtors
- Receive automated alerts/notifications

The bot uses the same AI backend as the web UI (Azure AI, OpenAI, Ollama, etc.) with full tenant isolation.

---

## 🤖 Bot Architecture

### Multi-Environment Support

| Environment | Bot Username Example | Webhook URL |
|-------------|---------------------|-------------|
| Production | `mydeskdr_bot` | `https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod` |
| Development | `mydeskdev_bot` | `https://dev.digitalresponse.com.au/api/telegram/webhook/dev` |
| Staging | `mydeskstaging_bot` | `https://staging.mydesk.digitalresponse.com.au/api/telegram/webhook/staging` |

### Configuration Priority (highest to lowest)

1. **`PlatformSettings.TelegramBots`** (per-tenant, stored in DB) — Most flexible
2. **`PlatformSettings.Telegram`** (simple Prod/Dev tokens) — Backward compatible
3. **`appsettings.json` → `Telegram:Bots`** (global, not per-tenant)
4. **Legacy `Telegram:BotToken`** (single bot, backward compatibility)

---

## ⚙️ Configuration

### Option 1: Flexible Multi-Environment (Recommended)

Configure in **Platform Settings → Telegram Bots** (stored in `PlatformSettingsEntities` table):

```json
{
  "TelegramBots": {
    "prod": {
      "BotToken": "123456789:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
      "BotUsername": "mydeskdr_bot",
      "AllowedUsers": ["peterb", "admin_user", "finance_team"],
      "AllowedChatIds": [123456789, 987654321],
      "WebhookUrl": "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod",
      "Environment": "prod",
      "Enabled": true,
      "CustomCommands": {
        "/cash": "Show cash position summary",
        "/pipeline": "Show sales pipeline"
      }
    },
    "dev": {
      "BotToken": "987654321:XYZ-ABC9876ghIkl-zyx57W2v1u123ew11",
      "BotUsername": "mydeskdev_bot",
      "AllowedUsers": ["peterb", "dev_user"],
      "AllowedChatIds": [123456789],
      "WebhookUrl": "https://dev.digitalresponse.com.au/api/telegram/webhook/dev",
      "Environment": "dev",
      "Enabled": true
    }
  }
}
```

### Option 2: Simple Prod/Dev (Legacy Format)

Configure in **Platform Settings → Telegram**:

```json
{
  "Telegram": {
    "ProdBotToken": "123456789:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
    "DevBotToken": "987654321:XYZ-ABC9876ghIkl-zyx57W2v1u123ew11",
    "AllowedUsers": ["peterb", "admin_user"],
    "AllowedChatIds": [123456789, 987654321],
    "DefaultEnvironment": "prod",
    "WebhookBaseUrl": "https://mydesk.digitalresponse.com.au",
    "ProdWebhookUrl": "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod",
    "DevWebhookUrl": "https://dev.digitalresponse.com.au/api/telegram/webhook/dev",
    "EnableVoiceTranscription": true,
    "EnableMarkdown": true
  }
}
```

### Option 3: Global appsettings.json (Not Recommended for Multi-Tenant)

```json
{
  "Telegram": {
    "Bots": {
      "prod": {
        "BotToken": "123456789:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
        "BotUsername": "mydesk_bot",
        "AllowedUsers": ["peterb", "admin"],
        "AllowedChatIds": [123456789],
        "WebhookUrl": "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod",
        "Environment": "prod",
        "Enabled": true
      }
    }
  }
}
```

---

## 🔧 Setup Steps

### 1. Create Bot(s) with @BotFather

For each environment (prod, dev, staging):

1. Open Telegram → Search `@BotFather`
2. Send `/newbot`
3. Name: `MyDesk Production` / `MyDesk Dev`
4. Username: `mydeskdr_bot` / `mydeskdev_bot` (must end in `_bot`)
5. Save the **Bot Token** (e.g., `123456789:ABC-DEF1234ghIkl-zyx57W2v1u123ew11`)

Optional: Set bot commands via `/setcommands`:
```
start - Welcome message
help - Show help
briefing - Morning business briefing
reconcile - Run MYOB reconciliation
debtors - Show top debtors
```

### 2. Configure Webhook URLs

Set webhook for each bot:

```bash
# Production
curl -X POST "https://api.telegram.org/bot<PROD_TOKEN>/setWebhook?url=https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod"

# Development
curl -X POST "https://api.telegram.org/bot<DEV_TOKEN>/setWebhook?url=https://dev.digitalresponse.com.au/api/telegram/webhook/dev"
```

Or use the admin API:
```bash
# Via admin endpoint (requires Admin/Director role)
curl -X POST "https://mydesk.digitalresponse.com.au/api/admin/telegram/webhook/set/prod?url=https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod" \
  -H "Authorization: Bearer <jwt_token>"

curl -X POST "https://dev.digitalresponse.com.au/api/admin/telegram/webhook/set/dev?url=https://dev.digitalresponse.com.au/api/telegram/webhook/dev" \
  -H "Authorization: Bearer <jwt_token>"
```

### 3. Verify Webhook

```bash
# Check webhook status
curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo"

# Or via admin API
curl "https://mydesk.digitalresponse.com.au/api/admin/telegram/webhook/info/prod" \
  -H "Authorization: Bearer <jwt_token>"
```

### 4. Add Allowed Users

In Platform Settings, add Telegram usernames (without `@`) or chat IDs:
- **Usernames**: `peterb`, `admin_user`
- **Chat IDs**: More secure, get via `@userinfobot` or from webhook payload

---

## 🧪 Local Development & Testing

### Using ngrok for Local Testing

1. **Start MyDesk locally:**
   ```bash
   cd C:\development\DR\app.mydesk.digitalresponse.com.au\src\MyDesk.Web
   dotnet run --environment Development
   ```

2. **Expose with ngrok:**
   ```bash
   ngrok http 5000 --subdomain=mydesk-dev
   # or: ngrok http 5000
   # Get URL: https://abc123.ngrok-free.app
   ```

3. **Set webhook to ngrok URL:**
   ```bash
   curl -X POST "https://api.telegram.org/bot<DEV_TOKEN>/setWebhook?url=https://abc123.ngrok-free.app/api/telegram/webhook/dev"
   ```

4. **Test in Telegram** - Send messages to your dev bot

### Test Commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message |
| `/help` | Show all commands |
| `/briefing` | Morning business briefing |
| `/reconcile` | Run MYOB reconciliation |
| `/debtors` | Show top debtors |
| Natural language | Ask any business question |
| Voice message | Auto-transcribed and processed |

---

## 📡 API Endpoints

### Webhook Endpoints (Receive from Telegram)

| Method | Endpoint | Environment |
|--------|----------|-------------|
| `POST` | `/api/telegram/webhook/prod` | Production |
| `POST` | `/api/telegram/webhook/dev` | Development |
| `POST` | `/api/telegram/webhook` | Default (prod) |

### Admin Management Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/admin/telegram/bots` | Admin/Director | List all configured bots |
| `POST` | `/api/admin/telegram/webhook/set/{env}` | Admin/Director | Set webhook URL |
| `POST` | `/api/admin/telegram/webhook/delete/{env}` | Admin/Director | Delete webhook |
| `GET` | `/api/admin/telegram/webhook/info/{env}` | Admin/Director | Get webhook info |
| `POST` | `/api/admin/telegram/broadcast/{env}` | Admin/Director | Broadcast to allowed users |

---

## 🔐 Security

### Access Control
- **AllowedUsers**: Telegram usernames (without `@`)
- **AllowedChatIds**: Numeric chat IDs (more secure)
- Both checked on every message
- Empty arrays = allow all (not recommended)

### Best Practices
1. Always use **chat IDs** over usernames (usernames can change)
2. Rotate bot tokens periodically via @BotFather → `/revoke`
3. Use HTTPS webhooks only
4. Monitor `/api/admin/telegram/webhook/info/{env}` for failed deliveries
5. Set `AllowedChatIds` for automated alerts

---

## 🏷️ Client-Specific Configuration

For multi-tenant deployments (e.g., Digital Response clients):

### Per-Client Bot Setup

Each client/tenant gets their own bot configuration in their `PlatformSettings`:

```json
// Tenant: "Digital Response"
{
  "TelegramBots": {
    "prod": {
      "BotToken": "<DR_prod_token>",
      "BotUsername": "mydeskdr_bot",
      "AllowedUsers": ["peterb", "caley", "adamw"],
      "WebhookUrl": "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod"
    },
    "dev": {
      "BotToken": "<DR_dev_token>",  
      "BotUsername": "mydeskdrdev_bot",
      "AllowedUsers": ["peterb"],
      "WebhookUrl": "https://dev.digitalresponse.com.au/api/telegram/webhook/dev"
    }
  }
}
```

### Automated Provisioning

For new client onboarding, use the admin API:

```bash
# 1. Create tenant
# 2. Configure their PlatformSettings with TelegramBots
# 3. Set webhooks automatically
curl -X POST "https://mydesk.digitalresponse.com.au/api/admin/telegram/webhook/set/prod?url=https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod"
curl -X POST "https://dev.digitalresponse.com.au/api/admin/telegram/webhook/set/dev?url=https://dev.digitalresponse.com.au/api/telegram/webhook/dev"
```

---

## 🔄 Automated Webhook Setup Script

Save as `scripts/setup-telegram-webhooks.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)][string]$ProdToken,
    [Parameter(Mandatory=$true)][string]$DevToken,
    [string]$ProdUrl = "https://mydesk.digitalresponse.com.au",
    [string]$DevUrl = "https://dev.digitalresponse.com.au"
)

function Set-Webhook($token, $url, $env) {
    $webhookUrl = "$url/api/telegram/webhook/$env"
    $response = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/setWebhook?url=$webhookUrl" -Method Post
    Write-Host "[$env] Webhook set: $response.ok - $response.description"
    return $response.ok
}

# Set production webhook
Set-Webhook $ProdToken $ProdUrl "prod"

# Set development webhook  
Set-Webhook $DevToken $DevUrl "dev"

# Verify
Write-Host "`nVerifying webhooks..."
Invoke-RestMethod "https://api.telegram.org/bot$ProdToken/getWebhookInfo" | Select-Object ok, result.url, result.last_error_date, result.last_error_message
Invoke-RestMethod "https://api.telegram.org/bot$DevToken/getWebhookInfo" | Select-Object ok, result.url, result.last_error_date, result.last_error_message
```

Usage:
```powershell
.\scripts\setup-telegram-webhooks.ps1 -ProdToken "123:ABC" -DevToken "456:XYZ"
```

---

## 📊 Monitoring & Debugging

### Check Bot Status
```bash
# List configured bots
curl "https://mydesk.digitalresponse.com.au/api/admin/telegram/bots" -H "Authorization: Bearer <token>"

# Get webhook info
curl "https://mydesk.digitalresponse.com.au/api/admin/telegram/webhook/info/prod" -H "Authorization: Bearer <token>"
```

### Logs
- Webhook requests logged at `Information` level
- Errors logged at `Error` level with full exception
- Check `Logs/app-YYYYMMDD.log` and `Logs/errors-YYYYMMDD.log`

### Common Issues

| Issue | Solution |
|-------|----------|
| "Bot not configured" | Check PlatformSettings for valid BotToken |
| "Access denied" | Add user to AllowedUsers/AllowedChatIds |
| Webhook not receiving | Verify URL accessible, check Telegram getWebhookInfo |
| Voice not working | Ensure Azure AI Speech configured, check logs |

---

## 📝 Configuration Reference

### TelegramBotConfig Properties

| Property | Required | Description |
|----------|----------|-------------|
| `BotToken` | ✅ | From @BotFather |
| `BotUsername` | ✅ | Bot username (ends in `_bot`) |
| `AllowedUsers` | ❌ | Array of Telegram usernames |
| `AllowedChatIds` | ❌ | Array of numeric chat IDs |
| `WebhookUrl` | ✅ | Full HTTPS webhook URL |
| `Environment` | ✅ | `prod`, `dev`, `staging`, etc. |
| `Enabled` | ❌ | Default `true` |
| `CustomCommands` | ❌ | Dict of custom command → description |

### TelegramSettings Properties (Legacy)

| Property | Description |
|----------|-------------|
| `ProdBotToken` / `DevBotToken` | Bot tokens |
| `AllowedUsers` / `AllowedChatIds` | Access control |
| `DefaultEnvironment` | `prod` or `dev` |
| `WebhookBaseUrl` | Base URL for auto-generating webhooks |
| `ProdWebhookUrl` / `DevWebhookUrl` | Explicit webhook URLs |
| `EnableVoiceTranscription` | Default `true` |
| `EnableMarkdown` | Default `true` |

---

## 🔗 Related Files

| File | Purpose |
|------|---------|
| `src/MyDesk.Web/Services/TelegramBotService.cs` | Core bot logic |
| `src/MyDesk.Shared/Models/PlatformSettings.cs` | Data models |
| `src/MyDesk.Web/Program.cs` | Webhook & admin endpoints |
| `src/MyDesk.Teams/manifest-dev.json` | Dev Teams app manifest |

---

## 📋 Quick Checklist for New Deployment

- [ ] Create prod bot with @BotFather
- [ ] Create dev bot with @BotFather  
- [ ] Add tokens to PlatformSettings (per tenant)
- [ ] Set AllowedUsers/AllowedChatIds
- [ ] Configure webhook URLs
- [ ] Run webhook setup script
- [ ] Verify via getWebhookInfo
- [ ] Test `/start` in Telegram
- [ ] Test voice message
- [ ] Test natural language query
- [ ] Configure admin alerts if needed

---

*Last updated: 2026-07-04 | MyDesk v3.1+ | Proposal #272*