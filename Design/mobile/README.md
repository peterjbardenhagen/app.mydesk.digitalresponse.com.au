# MyDesk Mobile — Android Wireframe Prototype

**Platform:** Android Native (MAUI / .NET Android)  
**Version:** 1.0 | July 2026

## Overview

The MyDesk mobile app is a companion to the web platform, designed for on-the-go approvals, expense capture, timesheet submission, and monitoring agent workflows.

## Screens Covered

| Screen | Description |
|--------|-------------|
| **Login / Splash** | Branded login with username/password, ⬡ MyDesk logo |
| **Dashboard** | KPI snapshot, quick actions, recent activity |
| **Notifications** | Bell icon badge, notification feed with actions |
| **Companies (CRM)** | Searchable company list, tap for detail |
| **Quotes** | Quote list with amounts, statuses, approval actions |
| **Invoices** | Invoice list with paid/pending/overdue indicators |
| **Expenses** | Tab-filtered list, camera receipt capture, submit for approval |
| **Timesheets** | Week view, daily hours entry, submit button |
| **AgentsOS DAG** | Simplified workflow status view |
| **Approvals** | Swipeable queue: approve/reject with one tap |
| **Banking** | Payment summary, bank account details (read-only) |
| **Profile & Settings** | Account info, notification prefs, server config |

## Navigation

- **Bottom tab bar** with 5 tabs: Home, CRM, Expenses, AgentsOS, Settings
- **Top app bar** with ⬡ MyDesk logo, notification bell (with badge), and profile avatar
- **Swipe gestures** for approve/reject on approval cards
- **Pull-to-refresh** on all list screens

## Design Decisions

1. **Bottom tabs** — Standard Android navigation pattern, familiar to users
2. **Single-column lists** — Optimised for mobile viewport, uses cards not tables
3. **Camera integration** — Direct receipt capture from expense form (no file picker detour)
4. **Offline support** — Cache recent data for viewing without connection
5. **Biometric auth** — Fingerprint / face unlock as secondary auth after initial login

## Viewing

Open `Design/mobile/index.html` in any browser. Phone-shaped frames show each screen.

## Next Steps

1. Implement bottom navigation in MAUI Shell
2. Build login screen with biometric support
3. Implement camera capture for expenses
4. Add push notification handling
5. Offline data caching with SQLite
