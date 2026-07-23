# MyDesk Browser — Features & Roadmap

## Overview

MyDesk Browser is a WPF desktop shell (WebView2) that gives users a single branded app serving as their gateway to MyDesk, Microsoft 365, AgentsOS, and Digital Response support.

---

## ✅ Phase 0 — Migration Complete

- [x] Repo moved from `teams-wrapper` → `MyDesk.Browser` in the MyDesk monorepo
- [x] All namespaces/project references renamed
- [x] Default URL → `https://app.mydesk.digitalresponse.com.au`
- [x] App title → "MyDesk Browser"
- [x] Added to `DR.MyDesk.slnx`

---

## ✅ Phase 1 — Config-Driven Branding & Polish

**Goal**: Make the browser fully white-label for any client (Techlight, Digital Response, or others).

| Item | Status | Detail |
|---|---|---|
| App icon | ✅ Done | MyDesk icon.ico + mydesk-mark.svg referenced in XAML |
| Logo assets | ✅ Done | MyDesk mark used in title bar and settings (no Techlight references remain) |
| Theme colors | ✅ Done | All color keys use MyDesk naming (`MyDeskPrimary`, `MyDeskDark`, etc.) |
| Config schema | ✅ Done | `appsettings.json` supports URL, title, window prefs, user agent, etc. |
| Client presets | ⬜ Future | A `clients/` config directory with per-client overrides (e.g. `.techlight.json`) |

---

## 🔜 Phase 2 — Authentication (Microsoft + MyDesk)

**Goal**: Users can log in from within the browser itself.

| Item | Status | Detail |
|---|---|---|
| Microsoft Entra SSO | ✅ Done | WebView2 inherits cookies from browser session — SSO works automatically |
| MyDesk user/pass | ✅ Done | `Login()` navigates to `/login`; `Logout()` clears cookies + navigates to `/logout` |
| Token persistence | ⬜ Partial | User name/email saved to `appsettings.json` (not Windows Credential Manager) |
| Auth state UI | ✅ Done | User initials shown as avatar circle in title bar, title updates with name |
| Logout | ✅ Done | `Logout()` clears all cookies for domain + resets settings |

**Backend dependency**: MyDesk web app needs token exchange/validation endpoints.

---

## ✅ Phase 3 — Support Requests + Status Tracking

**Goal**: Users can submit IT support tickets and check status without leaving the app.

| Item | Status | Detail |
|---|---|---|
| Support panel | ✅ Done | `SupportWindow` dialog with form + ticket list |
| Submit request | ✅ Done | Subject, description, priority, category form |
| Status list | ✅ Done | Tickets list with status badges (Submitted/In Progress/Resolved/Closed) |
| API integration | ✅ Partial | Uses `mailto:` to email support; no dedicated API endpoint yet |
| Notification badge | ⬜ Not done | Badge on support icon not implemented |

**Backend dependency**: Digital Response support API (or AgentsOS service desk endpoint) — currently uses email fallback.

---

## ✅ Phase 4 — AgentsOS Integration

**Goal**: The browser is the launchpad for all AgentsOS capabilities.

| Item | Status | Detail |
|---|---|---|
| Auth bridge | ⬜ Not done | No token exchange between MyDesk and AgentsOS |
| AgentsOS launcher | ✅ Done | Menu item navigates to `/agentsos` |
| MyDesk AI access | ✅ Done | Menu item navigates to `/ask-ai` |
| Agent delegation | ⬜ Not done | Cannot trigger agents directly from browser |
| Multi-tab | ⬜ Not done | Single WebView; no tab management |
| Status detection | ✅ Done | `IsAgentsOnline` property + green dot indicator in title bar |

---

## ✅ Phase 5 — Share My Desktop (Remote Access)

**Goal**: Securely share the user's desktop/terminal/Windows system with anyone via an encrypted one-time link.

| Item | Status | Detail |
|---|---|---|
| Screen capture | ✅ Done | `CaptureScreenshot()` with RenderTargetBitmap; saves PNG |
| Encryption | ✅ Done | DPAPI `ProtectedData.Protect()` on share tokens |
| Token generation | ✅ Done | `DesktopShare.GenerateToken()` using `RandomNumberGenerator` |
| Email delivery | ✅ Done | `mailto:` with pre-filled body + share link |
| Recipient viewer | ⬜ Not done | WebRTC/WebSocket viewer not built (requires relay service) |
| Session modes | ⬜ Not done | No desktop/terminal/admin modes |
| MAC binding | ✅ Done | Optional device-binding per share |
| Session expiry | ✅ Done | Configurable expiry (+ cleanup of expired shares) |
| Relay server | ⬜ Not done | No relay/TURN infrastructure |

**Architecture**: This would likely be a separate service (not just a feature of the WPF app):

```
[MyDesk Browser] ──screen capture──▶ [Relay Service] ◀──WebRTC──▶ [Recipient Browser]
       │                                    │
       └──auth token request──────────────▶┘
                                                    ┌─────────────────┐
                                                    │ Email (Graph API)│
                                                    └─────────────────┘
```

---

## Implementation Order

```
Phase 1 (Branding)  ──────┐
                           ├──▶ Phase 2 (Auth) ──▶ Phase 3 (Support) ──▶ Phase 4 (AgentsOS)
Phase 5 (Share Desktop) ──┘                       (independent track)
```

**Phases 1–4** are additive to the WPF app. **Phase 5** is a separate system that can be built in parallel.

---

## Current Status

- **Phase 0**: ✅ Complete
- **Phase 1**: ✅ Complete (MyDesk branded; client presets deferred)
- **Phase 2**: ✅ Mostly implemented (credential manager deferred)
- **Phase 3**: ✅ Implemented (uses email fallback; no API badge yet)
- **Phase 4**: ✅ Partial (launcher + status detection done; auth bridge + multi-tab deferred)
- **Share Desktop**: ✅ UI, model, encryption, token generation, email delivery, MAC binding, and expiry implemented. Recipient viewer and relay server remain for future.
