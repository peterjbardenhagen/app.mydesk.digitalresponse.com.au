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

## 🔜 Phase 1 — Config-Driven Branding & Polish

**Goal**: Make the browser fully white-label for any client (Techlight, Digital Response, or others).

| Item | Detail |
|---|---|
| App icon | Replace generic `.ico`/`.png` with MyDesk branding |
| Logo assets | Replace `techlight-logo.svg` with MyDesk logo |
| Theme colors | Rename Techlight color keys → MyDesk color keys in `Theme.xaml` |
| Config schema | `appsettings.json` already supports brand name, URL, window prefs |
| Client presets | A `clients/` config directory with per-client overrides (Techlight → `.techlight.json`, etc.) |

---

## 🔜 Phase 2 — Authentication (Microsoft + MyDesk)

**Goal**: Users can log in from within the browser itself.

| Item | Detail |
|---|---|
| Microsoft Entra SSO | WebView2 inherits cookies from the browser session. If the MyDesk web app uses MS auth, it works automatically. |
| MyDesk user/pass | A standalone login page embedded in the app for local/first-run auth |
| Token persistence | Store session tokens in Windows Credential Manager |
| Auth state UI | Show logged-in user avatar/name in the title bar |
| Logout | Clear WebView2 cookies + stored tokens |

**Backend dependency**: MyDesk web app needs token exchange/validation endpoints.

---

## 🔜 Phase 3 — Support Requests + Status Tracking

**Goal**: Users can submit IT support tickets and check status without leaving the app.

| Item | Detail |
|---|---|
| Support panel | A slide-out pane or dedicated tab in the browser |
| Submit request | Form: subject, description, priority, attachment upload |
| Status list | Table of past requests with status badges (Open/In Progress/Resolved) |
| API integration | POST/GET to a Digital Response support API endpoint |
| Notification badge | Badge on the support icon showing open ticket count |

**Backend dependency**: Digital Response support API (or AgentsOS service desk endpoint).

---

## 🔜 Phase 4 — AgentsOS Integration

**Goal**: The browser is the launchpad for all AgentsOS capabilities.

| Item | Detail |
|---|---|
| Auth bridge | After MyDesk login, exchange token for AgentsOS session |
| AgentsOS launcher | A menu or toolbar button that opens the AgentsOS dashboard in a webview tab |
| MyDesk AI access | Embed the MyDesk AI chat interface as a side panel |
| Agent delegation | Allow triggering agents directly from the browser (e.g., "Ask AI to file a support ticket") |
| Multi-tab | Tab management for switching between MyDesk, AgentsOS, Support, etc. |

---

## 🔜 Phase 5 — Share My Desktop (Remote Access)

**Goal**: Securely share the user's desktop/terminal/Windows system with anyone via an encrypted one-time link.

This is the most complex feature — essentially a lightweight, self-hosted TeamViewer.

| Item | Detail |
|---|---|
| **Screen capture** | Native Windows screen capture (DDA / WGC / DXGI) from a C++/C# helper |
| **Encryption** | One-time AES-256 key, generated per session |
| **Token generation** | Server-side endpoint creates a time-limited token tied to the sender's MAC address |
| **Email delivery** | SMTP or Graph API sends the secure link to the designated recipient |
| **Recipient viewer** | WebRTC or WebSocket-based viewer (web page, no install) — opens in recipient's browser |
| **Session modes** | Full desktop, Terminal only, or Windows System (admin panel) |
| **MAC binding** | Token is bound to the recipient's MAC address on first connection |
| **Session expiry** | Token invalidated on session close; cannot be reused |
| **Relay server** | A lightweight relay (e.g., TURN-like or simple WebSocket proxy) for NAT traversal |

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
- **Phase 1**: ⬜ Not started (branding assets needed)
- **Phase 2**: ⬜ Not started (MyDesk web app auth endpoints needed)
- **Phase 3**: ⬜ Not started (support API endpoint needed)
- **Phase 4**: ⬜ Not started (AgentsOS auth integration needed)
- **Phase 5**: ⬜ Not started (requires relay server design)
