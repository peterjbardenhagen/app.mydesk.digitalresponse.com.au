# Demo Lighting — MyDesk Demo Platform

[![MyDesk Platform](https://img.shields.io/badge/Platform-MyDesk-004E89)](https://app.mydesk.digitalresponse.com.au)
[![Version](https://img.shields.io/badge/Version-1.0.0-FF6B35)](https://app.mydesk.digitalresponse.com.au)

**Demo Lighting** is a fully functional demo tenant on the **MyDesk Enterprise AI Brain** platform, powered by **AgentsOS**. This instance is pre-loaded with realistic synthetic data across all core modules, ready for client walkthroughs, product demonstrations, and evaluation.

---

## Table of Contents

1. [Getting Started — Web Login](#1-getting-started--web-login)
2. [APK Download & Mobile App](#2-apk-download--mobile-app)
3. [Credentials](#3-credentials)
4. [About Demo Lighting](#4-about-demo-lighting)
5. [Modules Overview](#5-modules-overview)
6. [Synthetic Data Summary](#6-synthetic-data-summary)
7. [Platform Architecture](#7-platform-architecture)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Getting Started — Web Login

### Step 1: Open the URL

Navigate to one of the following:

| Environment | URL | Notes |
|-------------|-----|-------|
| **Production** | [https://app.mydesk.digitalresponse.com.au](https://app.mydesk.digitalresponse.com.au) | Live demo instance |
| **Local Dev** | [http://localhost:5237](http://localhost:5237) | Run `dotnet run` from `src/MyDesk.Web` |

### Step 2: Enter Credentials

On the login page:

| Field | Value |
|-------|-------|
| **Username / Code** | `DEMO` (case-insensitive) |
| **Password** | `demo123` |

### Step 3: Click "Sign In"

After successful login, you'll land on the **Dashboard** — your starting point for all modules.

> **First-time experience:** The app runs database migrations and seeds demo data automatically on startup (5–15 seconds). If you see a loading screen, wait briefly and refresh.

### Step 4: Logout

Click **Logout** in the user menu (top-right avatar/menu icon). You'll be returned to the login page.

---

## 2. APK Download & Mobile App

### Download the Latest APK

The Android APK is available for direct download:

- **File:** `DigitalResponseMyDesk-v1.0.0.apk` (2.9 MB)
- **Web Download:** Log into the web dashboard → click your **Profile** → **Download Mobile App** button
- **Direct URL:** `https://app.mydesk.digitalresponse.com.au/api/download/apk` (requires auth)
- **Build Artifact:** `artifacts/DigitalResponseMyDesk-v1.0.0.apk` in the repository

### Installation Instructions

**Android:**

1. Open Settings → **Security** → Enable **"Install from unknown apps"** (temporary)
2. Download the APK from the web dashboard or direct link
3. Open the downloaded file from the notification tray or Downloads folder
4. Tap **Install**
5. After install, you can disable "Install from unknown apps" for security

**iOS (Coming Soon):**

The iOS app is in development. For demo purposes, the web app is fully responsive and works well on mobile Safari.

### Mobile Features

| Feature | Status |
|---------|--------|
| Login with DEMO/demo123 | ✅ |
| Dashboard & KPI tiles | ✅ |
| CRM (Companies, Contacts) | ✅ |
| Quotes & Sales | ✅ |
| Expenses with receipt capture | ✅ |
| Timesheets | ✅ |
| Tasks & Notifications | ✅ |
| File Upload | ✅ |
| Push Notifications | ✅ |

---

## 3. Credentials

### Demo User

| Field | Value |
|-------|-------|
| **Username / Code** | `DEMO` |
| **Password** | `demo123` |
| **Email** | `demo@demolighting.com.au` |
| **Role** | Administrator |
| **Tenant** | Demo Lighting |

> Please use this shared demo account for client walkthroughs. Do not change the password — it is shared across the team.

### Tenant Info

| Field | Value |
|-------|-------|
| **Tenant ID** | `55555555-5555-5555-5555-555555555555` |
| **Slug** | `demo-lighting` |
| **Branding** | Orange (#FF6B35) / Navy (#004E89) / Gold (#F7B801) |
| **Logo** | `/images/demo-lighting-logo.svg` |

---

## 4. About Demo Lighting

**Demo Lighting** is a fictional Lighting & Electrical company used to showcase the MyDesk platform's capabilities. It represents a mid-size Australian B2B service business with:

- **~25 employees** across Sales, Projects, Installations, and Accounts
- **Operations** in Melbourne, Sydney, and Brisbane
- **$2.5M annual revenue** across commercial and residential projects
- **~50 active projects** at any time

### Demo Scenario

Demo Lighting recently won three major contracts:

1. **Solar Systems — Chatfield Project** ($187K) — Commercial solar + battery installation for a shopping centre
2. **Bright LED Solutions** ($49.5K) — Smart lighting retrofit for a CBD office tower
3. **Smart Lighting Co** ($24.75K PO) — LED downlight and dimmer switch supply

These deals are at various stages (approved quote, issued invoice, pending purchase order) to demonstrate the full quote-to-cash lifecycle.

---

## 5. Modules Overview

### Core Business Modules

| Module | Demo Data | Key Actions |
|--------|-----------|-------------|
| **Dashboard** | KPI tiles, charts, upcoming tasks, recent activity | Welcome back greeting, quick actions |
| **Companies (CRM)** | 8 demo companies (Acme Engineering, Solar Systems, Bright LED, etc.) | Create, edit, view contacts, quotes |
| **Contacts** | 11 demo contacts across companies | Create, edit, email, phone lookup |
| **Quotes** | 4 demo quotes (Office Refit, Hotel Lobby, Warehouse Compliance, Solar) | Create, approve, convert to invoice, print PDF |
| **Invoices** | 4 demo invoices with line items | Create, issue, mark paid, email |
| **Purchase Orders** | 3 demo POs with line items | Create, approve, receive goods |
| **Job Orders** | 2 demo job orders (Site Survey, Equipment Delivery) | Create, assign, track status |
| **Expenses** | 3 demo expenses (Conference Travel, Client Lunch, Training Mat.) | Submit, approve, reject, receipt upload |
| **Timesheets** | 2 demo timesheets (40h and 38.5h) | Create, submit, approve |
| **Banking** | Demo bank statements + transactions | Reconcile, match, review |
| **Tasks** | 5 demo project tasks (Site Survey, Timeline, Ordering, etc.) | Create, assign, update status |

### AI & Intelligence Modules

| Module | Demo Data | Key Actions |
|--------|-----------|-------------|
| **Ask AI** | Connected to AgentsOS AI Brain | Ask natural language questions about your data |
| **AgentsOS DAG** | DAG workflow visualization | View Plans, Goals, Ledger, approve/reject gated tasks |
| **Reports** | Dynamic reports from demo data | Filter, export, schedule |
| **Noticeboard** | 2 demo notices | Create, pin, dismiss |
| **File Library** | Demo files | Upload, download, organize |
| **Scheduled Tasks** | 2 demo automation tasks | View, run, monitor |

### Administration Modules

| Module | Description |
|--------|-------------|
| **Profile** | User profile, photo upload, password change, mobile app download |
| **Notifications** | Bell icon with real-time notification list |
| **Delegation Manager** | Delegate approvals to other users by module |
| **Settings** | Platform settings, branding, user management |

---

## 6. Synthetic Data Summary

All demo data is created by two mechanisms that run on app startup:

### SQL Migrations (004 + 011)

Runs once as part of database initialization:

| Entity | Count | Details |
|--------|-------|---------|
| Tenant | 1 | Demo Lighting (ID: `55555...`) |
| User | 1 | DEMO / demo123 |
| Companies | 3 | Solar Systems, Bright LED, Smart Lighting Co |
| Divisions | 2 | Sales & Commercial, Projects & Installation |
| Quotes | 1 | DL-QUOTE-001 (Solar Panel Install, $18,750) |
| Invoices | 2 | DL-INV-001 ($49,500), DL-INV-002 ($35,750) |
| Purchase Orders | 1 | DL-PO-001 ($24,750) |
| Expenses | 3 | Conference Travel, Client Lunch, Training Materials |
| Timesheets | 2 | 40h and 38.5h weeks |
| Tasks | 5 | Site survey, timeline, ordering, approvals, safety |

### DemoDataSeeder (C# Hosted Service)

Runs on app startup, idempotent (checks sentinel `[DEMO]` prefix):

| Entity | Count | Prefix |
|--------|-------|--------|
| Companies | 5 | `[DEMO]` |
| Contacts | 8 | `[DEMO]` prefix on names |
| Products | 6 | `[DEMO]` |
| Quotes | 3 | `[DEMO]` |
| Invoices | 2 | `[DEMO]` |
| Purchase Orders | 2 | `[DEMO]` |
| Job Orders | 2 | `[DEMO]` |
| Expenses | Via SQL migration | — |
| Banking Statements | 2 | `[DEMO]` |
| Notices | 2 | `[DEMO]` |
| Files | 3 | `[DEMO]` |
| Scheduled Tasks | 2 | `[DEMO]` Weekly pipeline summary |

All data is **idempotent** — it only seeds once. On subsequent startups, the `[DEMO]` sentinel check skips re-seeding.

---

## 7. Platform Architecture

```
┌─────────────────────────────────────────────┐
│              Web App (Blazor Server)          │
│          app.mydesk.digitalresponse.com.au    │
├─────────────────────────────────────────────┤
│           Mobile App (Android APK)            │
│      artifacts/DigitalResponseMyDesk-*.apk   │
├─────────────────────────────────────────────┤
│            AgentsOS AI Brain                  │
│      REST API → localhost:8080/health         │
├─────────────────────────────────────────────┤
│           Database (SQL Server)               │
│      Connection string in appsettings.json    │
├─────────────────────────────────────────────┤
│         CI/CD (GitHub Actions)                │
│  playwright-tests.yml → smoke tests           │
│  playwright-full.yml → full suite             │
│  android-build.yml → APK build                │
└─────────────────────────────────────────────┘
```

### Tech Stack

| Component | Technology |
|-----------|-----------|
| **Frontend** | Blazor Server + MudBlazor 7 |
| **Backend** | .NET 10 Minimal APIs |
| **Database** | SQL Server (LocalDB dev / Azure SQL prod) |
| **AI Brain** | AgentsOS (Python) on localhost:8080 |
| **Auth** | Session-based with BCrypt password hashing |
| **CI/CD** | GitHub Actions (windows-latest) |
| **Tests** | Playwright .NET (NUnit) |
| **Mobile** | Android Native (Kotlin) |

---

## 8. Troubleshooting

### Login Issues

| Symptom | Cause | Fix |
|---------|-------|-----|
| "Invalid credentials" | Wrong password or user not found | Use **DEMO** / **demo123** exactly |
| Page doesn't load | App still starting up | Wait 15s and refresh |
| White screen / 500 error | Database not migrated | App auto-migrates on first start; check logs |
| "No tenant found" | Tenant slug mismatch | Use `demo-lighting` subdomain or the main URL |

### Known Limitations (Demo Only)

- All demo emails are redirected to `demo@bardenhagen.xyz` — no real emails are sent
- The APK is a debug build; production signing is separate
- Demo data resets only if the database is recreated (migration scripts re-run)
- AgentsOS API must be running on `:8080` for DAG/Ledger features to work
- Some external integrations (Xero, MYOB, banking feeds) show demo data only

---

*MyDesk Platform — Built by Digital Response. Powered by AgentsOS.*
*For internal demo use only. Not for production deployment.*
*Last updated: July 2026*
