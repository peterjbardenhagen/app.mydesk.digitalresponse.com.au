# MyDesk Mobile App

## Overview
MyDesk provides a native Android mobile application for consultants and business operators to access key business data on the go. The app offers offline support, real-time data synchronization, and module-level access control based on tenant configuration.

**Location**: `Mobile/Android/DigitalResponseMyDesk/`

## Architecture

### Authentication
- **PAT (Personal Access Token)** authentication: Issued at login, valid for 1 year
- Tokens are scoped to the default tenant and stored in localStorage
- Supports both username/password and Azure AD SSO login
- Mobile endpoints: `/api/auth/mobile/login` and `/api/auth/mobile/azure-callback`

### Data Sync
- **REST API**: All data fetched via PAT-authenticated `/api/mobile/*` endpoints
- **Caching**: localStorage with 1-hour TTL for offline support
- **Pagination**: List endpoints support limit/offset pagination (50 items per page)
- **Tenant Isolation**: All API calls enforce tenant context via ICurrentTenantAccessor

### Module Gating
The app requests available modules from `/api/mobile/modules` which returns:
- **Demo Lighting**: All Phase 1 & 2 modules + core modules
- **Digital Response**: All modules
- **Techlight**: All modules
- **Carter Capner Law**: Law-specific modules (Tasks, Timesheets, Contacts, no POs/Despatch)

## Implemented Modules

### Phase 0 (Core)
1. **Invoices** - List, detail view with line items, status filtering
2. **Quotes** - List with expiry tracking, detail with margin %, line items
3. **Purchase Orders** - List with supplier, detail with required-by dates
4. **Files** - Folder/file browser with real metadata

### Phase 1 (Operations)
5. **Expenses** - Expense tracking with receipts, category breakdown
6. **Timesheets** - Weekly timesheet entry with daily tracking
7. **Tasks** - Task management with priority, assignment, due dates
8. **Despatch** - Delivery tracking with proof of delivery (signatures)

### Phase 2 (Strategy)
9. **Contacts** - Contact management with company association
10. **Cash Flow** - 12-week cash position forecast
11. **Business Goals** - KPI tracking with progress indicators
12. **Projects** - Project tracking with health status and milestones

## API Endpoints

### Authentication
```
POST /api/auth/mobile/login
  Request: { username, password }
  Response: { token, user, tenants, activeTenant }

POST /api/auth/mobile/azure-callback
  Response: { token, user, tenants, activeTenant }

POST /api/auth/mobile/token?t={token}
  Response: Validates token, returns user info
```

### Module Management
```
GET /api/mobile/modules
  Response: { modules: [list of available modules for tenant] }
```

### Data Endpoints (all require Bearer PAT auth)
```
GET /api/mobile/invoices?limit=50&offset=0
GET /api/mobile/invoices/{id}

GET /api/mobile/quotes?limit=50&offset=0
GET /api/mobile/quotes/{id}

GET /api/mobile/purchase-orders?limit=50&offset=0
GET /api/mobile/purchase-orders/{id}

GET /api/mobile/expenses?limit=50&offset=0
GET /api/mobile/expenses/{id}

GET /api/mobile/timesheets?limit=50&offset=0
GET /api/mobile/timesheets/{id}

GET /api/mobile/tasks?limit=50&offset=0
GET /api/mobile/tasks/{id}

GET /api/mobile/despatch?limit=50&offset=0
GET /api/mobile/despatch/{id}

GET /api/mobile/contacts?limit=50&offset=0
GET /api/mobile/contacts/{id}

GET /api/mobile/cashflow
  Response: 12-week cash position forecast

GET /api/mobile/goals
  Response: Active business goals with progress

GET /api/mobile/projects
  Response: Projects with health status

GET /api/mobile/files?folderId={uuid}
  Response: Files and folders

POST /api/chat/mobile
  Request: { message, conversationId? }
  Response: AI reply with tool results
```

## UI/UX Features

### Home Screen
- Revenue card with trend chart
- Key metrics (quotes, invoices, receivables)
- Desky AI chat integration
- Quick-access module buttons for all 12 modules
- Inline chat for quick questions

### Module Screens
- **List View**: Paginated list with item summaries, status indicators, quick metadata
- **Detail View**: Full item information with line items (where applicable)
- **Progressive Loading**: Graceful fallback when modules unavailable for tenant

### Theme & Branding
- Dark mode by default, with light mode support
- Tenant-specific brand colors (Techlight, Digital Response, Carter Capner Law)
- Responsive design for phones and tablets
- AUD currency formatting, Australian date locale

### AI Integration
- **Desky Chat**: Inline on home screen + dedicated chat screen
- Full tool loop with 7 AI tools (quotes summary, invoices, pipeline, cash flow, etc.)
- Real-time data feeding to AI for analysis

## Build & Deployment

### Prerequisites
```bash
# Android Studio 2024+
# Android SDK 24+ (API level)
# Java 17+
```

### Building APK
```bash
cd Mobile/Android/DigitalResponseMyDesk
./gradlew assembleRelease
# Output: app/build/outputs/apk/release/app-release.apk
```

### GitHub Actions APK Build
The repository includes `.github/workflows/android-build.yml` which:
1. Builds the APK on each push to `claude/deploy-*` branches
2. Stores APK as artifact for 30 days
3. Can be downloaded from Actions tab

## Development

### File Structure
```
Mobile/Android/DigitalResponseMyDesk/
├── app/src/main/
│   ├── assets/
│   │   └── app.html          # Main UI (single-page app)
│   ├── java/
│   │   └── MainActivity.java  # Android activity wrapper
│   └── res/
│       ├── layout/activity_main.xml
│       ├── drawable/ (app icons)
│       ├── values/ (colors, strings)
│       └── mipmap-* (launchers)
├── build.gradle              # App-level config
├── proguard-rules.pro        # Obfuscation rules
└── AndroidManifest.xml       # App permissions
```

### Key JavaScript Files (in app.html)
- **State management**: `const state = { ... }`
- **Navigation**: `navigate()` function switches screens
- **API calls**: `apiCall()` adds Bearer auth headers
- **Data loading**: `loadData()` fetches all modules
- **Theme/Brand**: `applyTheme()`, `applyBrand()`
- **Chat**: `sendChat()`, `sendDeskyChatMessage()`

### Adding New Modules
1. Add module button to home screen grid (line ~541)
2. Add state properties and pagination (state object)
3. Add screen definitions (HTML div with id="screen-{name}")
4. Add loading logic in `loadData()` function
5. Add `build{Module}List()` and `show{Module}Detail()` functions
6. Update `updateModuleButtons()` to show/hide based on module gating
7. Create corresponding API endpoint in `Program.cs`

## Testing

### Local Testing
1. Run MyDesk web app: `dotnet run`
2. Open Chrome DevTools on the web app
3. Use Device Toolbar to simulate mobile (375x812)
4. Login and test module screens

### APK Testing
1. Build APK: `./gradlew assembleDebug`
2. Install on emulator/device: `adb install app/build/outputs/apk/debug/app-debug.apk`
3. Open app, authenticate with test credentials
4. Verify module data loads correctly

### Module Gating Testing
1. Test with different tenant accounts (Demo Lighting, Carter Capner Law, etc.)
2. Verify only available modules appear on home screen
3. Verify API returns 403 or empty for unavailable modules

## Performance Considerations
- **Pagination**: Limit 50 items per page, load more button for additional pages
- **Caching**: 1-hour TTL prevents excessive API calls
- **Offline**: LocalStorage allows viewing cached data without network
- **Image Optimization**: File library thumbnails are generated server-side
- **Bundle Size**: Proguard obfuscation reduces APK size (~8-12MB)

## Security
- **PAT Auth**: Long-lived tokens scoped to default tenant only
- **HTTPS Only**: All API calls over HTTPS (enforced in production)
- **XSS Protection**: All user data HTML-escaped in app.html templates
- **CSRF**: App-to-API calls use PAT bearer auth (no cookies)
- **Token Revocation**: User can revoke tokens from web app

## Known Limitations
- Single tenant per PAT (no workspace switching in current build)
- No offline write capability (read-only when offline)
- No push notifications (polling-based refresh)
- No biometric authentication (username/password only)

## Future Enhancements
- Offline write queue with sync on reconnect
- Push notifications for overdue items
- Barcode/QR code scanning for despatch
- Photo capture for expense receipts
- Biometric login (fingerprint/face)
- Dark theme customization
- Multiple language support
