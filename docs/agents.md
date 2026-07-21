# MyDesk Project — CI/CD & Agent Context

## 🚀 CI/CD Pipelines

### GitHub Actions (`.github/workflows/`)

| Workflow | Trigger | Purpose | 
|----------|---------|---------|
| `playwright-tests.yml` | Push/PR to `main`, `master`, `develop` | Builds .NET, runs Playwright **Smoke** tests |
| `playwright-full.yml` | Manual dispatch only | Builds .NET, runs **full** Playwright suite, logs to `deployment-log/` |
| `android-build.yml` | Push/PR to `main`, `master`, `develop` (path: `Mobile/Android/**`) | Builds debug + release APKs |

### .NET Deployments

| Environment | Type | Server | Path | Method |
|-------------|------|--------|------|--------|
| **Development** (dev.digitalresponse.com.au) | Local IIS | `pb-legion` | `C:\inetpub\wwwroot\MyDesk` | Manual `dotnet publish` (see below) |
| **Production** (svr1.digitalresponse.com.au) | Unknown | Remote | Unknown | Not automated in this repo yet |

**No GitHub Actions deploy workflow exists for .NET.** This should be added if CI/CD to production is desired.

### Mobile (Android)

| Environment | Method | Output |
|-------------|--------|--------|
| Debug APK | GitHub Actions (`android-build.yml`) | `app-debug.apk` (artifact, 30 day retention) |
| Release APK | GitHub Actions (`android-build.yml`) | `app-release.apk` (artifact, 90 day retention), requires keystore secrets |

### Mobile (iOS)

**No CI/CD pipeline configured.** Only scaffolding exists at `Mobile/iOS/`. Manual Xcode build required.

---

## 🚀 Deploying to Development IIS

1. Open **PowerShell as Administrator** (right-click → Run as Administrator)
2. Navigate to the project:
   ```powershell
   cd C:\Development\DR\app.mydesk.digitalresponse.com.au\src\MyDesk.Web
   ```
3. **Stop the IIS site** to unlock DLLs:
   ```powershell
   net stop W3SVC /y
   # OR just the app pool:
   & "C:\Windows\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:"MyDesk"
   ```
4. **Publish and deploy:**
   ```powershell
   dotnet publish --configuration Release --output "C:\inetpub\wwwroot\MyDesk"
   ```
5. **Restart IIS:**
   ```powershell
   net start W3SVC
   # OR just the app pool:
   & "C:\Windows\System32\inetsrv\appcmd.exe" start apppool /apppool.name:"MyDesk"
   ```
6. **Verify:**
   ```bash
   curl -X POST "https://dev.digitalresponse.com.au/api/telegram/webhook/dev"
   # Should return 200 (not 404 anymore)
   ```

### Quick Deploy (if you already published to Desktop):
```powershell
# Run as Administrator:
net stop W3SVC /y
xcopy /E /Y "C:\Users\PeterBardenhagen\Desktop\MyDesk-publish-temp\*" "C:\inetpub\wwwroot\MyDesk\"
net start W3SVC
```

---

## 🧪 Running Tests Locally

```bash
# Build everything
dotnet build MyDesk.slnx

# Run .NET unit tests
dotnet test tests/MyDesk.UnitTests --verbosity quiet

# Run Playwright tests (requires LocalDB)
dotnet test tests/MyDesk.PlaywrightTests --filter "TestCategory=Smoke"
```

## 🔧 Key Repo Structure

```
├── .github/workflows/     # CI/CD pipelines
│   ├── android-build.yml   # Android APK builds
│   ├── playwright-tests.yml# .NET smoke tests
│   └── playwright-full.yml # Full test suite
├── src/
│   ├── MyDesk.Web/         # .NET 10 Blazor Server app
│   ├── MyDesk.Shared/      # Shared library (models, services)
│   └── MyDesk.Teams/       # Teams app manifests + packaging
├── tests/
│   ├── MyDesk.UnitTests/   # xUnit unit tests
│   └── MyDesk.PlaywrightTests/  # E2E Playwright tests
├── Mobile/
│   ├── Android/            # Android APK (Gradle)
│   └── iOS/                # iOS app scaffolding
└── scripts/                # Deployment utilities
    └── setup-telegram-webhooks.ps1  # Telegram bot webhook setup
```

## 🤖 Telegram Bot Endpoints

| Bot | Token (last 4) | Webhook URL | Status |
|-----|----------------|-------------|--------|
| `mydesk_bot` (prod) | `XIs` | `https://svr1.digitalresponse.com.au/api/telegram/webhook/prod` | ✅ Set, needs deployment |
| `mydeskdev_bot` (dev) | `tEA` | `https://dev.digitalresponse.com.au/api/telegram/webhook/dev` | ✅ Set, needs deployment |

## 📦 Teams App Manifests

```
src/MyDesk.Teams/
├── manifest.json               # Production (with Copilot)
├── manifest-basic.json         # Production (basic, no Copilot)
├── manifest-dev.json           # Development (points to dev.digitalresponse.com.au)
├── declarativeAgent-dev.json   # Dev Copilot agent
├── ai-plugin-dev.json          # Dev AI plugin
└── Package-TeamsApp.ps1        # Creates 4 ZIPs (Python-based)
```

Run: `pwsh src/MyDesk.Teams/Package-TeamsApp.ps1` to rebuild ZIPs.

## 🔐 Secrets & Configuration

| Secret | Used By | Location |
|--------|---------|----------|
| GEMINI_API_KEY | Gemini CLI, Agents Handoff | Env var |
| Telegram bot tokens | Telegram webhook | `scripts/setup-telegram-webhooks.ps1` |
| KEYSTORE_BASE64, KEYSTORE_PASSWORD, KEY_ALIAS, KEY_PASSWORD | GitHub Actions (`android-build.yml`) | GitHub secrets |
| AzureAD:ClientId, AzureAD:ClientSecret | .NET app | `appsettings.json` |
| ConnectionStrings:TechlightDb | .NET app | `appsettings.json` |
