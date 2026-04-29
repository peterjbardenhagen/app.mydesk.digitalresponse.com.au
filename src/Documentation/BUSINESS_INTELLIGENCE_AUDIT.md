# MyDesk — Business Intelligence Audit & Enhancements

**Date:** April 2026
**Scope:** Executive-grade analytics, target tracking, customer intelligence,
individual performance profiles.

---

## 1. What Was Added

### 1.1 Performance Targets (NEW)
A complete **Monthly / Quarterly / Yearly** target system at three levels:

| Level      | Source                                                      |
|------------|-------------------------------------------------------------|
| Company    | `Config/targets.json` → `CompanyMonthlyTarget` etc.         |
| Sales Team | Sum of individual targets for active users                  |
| Individual | `Config/targets.json` → `UserTargets[<code>]` or defaults   |

**Smart features:**
- **Forecast**: Linear projection of current run-rate to end of period
- **Expected vs Actual progress bar**: Shows if you're ahead or behind pace
- **Traffic-light banding**: `On Track / At Risk / Behind` against time-adjusted pace
- **Per-user leaderboard** with real-time ranking and target progress

**Visibility:** Director / Admin only (surfaced in Dashboard carousel)

**Config file:** `src/MyDesk.Web/Config/targets.json` (hot-reloaded every 30s)

---

### 1.2 Customer Intelligence (NEW)
Four categorisations answer: _"Who is making us money, and who is wasting our time?"_

| Category      | Logic                                                       |
|---------------|-------------------------------------------------------------|
| **Best**      | Top YTD revenue                                             |
| **Growing**   | YTD revenue ≥ $5K AND YoY growth ≥ 25%                      |
| **At Risk**   | Prior year revenue ≥ $10K AND current decline ≥ 25%         |
| **Wasting Time** | 3+ quotes AND quote-value : revenue ratio ≥ 5x           |

**Ratings system (Diamond → Watch):**
- Diamond: ≥ $500K YTD
- Gold: ≥ $100K
- Silver: ≥ $25K
- Bronze: ≥ $5K
- Watch: quoted but no revenue
- New: no history

**Company metrics surfaced:**
- Active customers (invoiced in 90 days)
- Dormant customers (no activity in 180+ days)
- Top 10 revenue concentration %

**Visibility:** Director / Admin only

---

### 1.3 User Performance Profile (NEW)
Route: `/admin/users/{id}/profile`

Shows for every individual:
- Current team rank with performance band
- Month / Quarter / Year target vs actual (3 progress bars)
- 4 KPI cards: quotes raised, quotes won, invoices closed, open quotes
- **Team comparison** section: revenue, win rate, quotes raised, avg quote value —
  user's bar vs team-average bar, with ± percentage delta

**Visibility:** Director / Admin only.  
**Entry point:** New "Performance Profile" icon on the Users list (📈 Insights icon).

---

## 2. Fixed / Enhanced

| Issue                                                 | Resolution                                |
|-------------------------------------------------------|-------------------------------------------|
| `CCompany` SQL error in PurchaseOrder list            | Joined `Contacts → Companies` properly    |
| Dashboard "0.0% gross profit" with no data            | Shows `-.-%` when no activity             |
| Revenue Trends chart had no tooltips                  | Enabled `ShowTooltips = true`             |
| Alert cards looked plain                              | New card component with warning/critical/rec variants |
| White/invisible dashboard icons                       | CSS `fill: currentColor` for SVG KPI icons |
| `Techlight.MyDesk.*` namespace drift                  | Standardised to `MyDesk.*` with `MyDesk.*` assembly names |

---

## 3. Architecture

### New Files
```
src/MyDesk.Shared/Services/
  ├── ITargetsProvider.cs          — Interface for config-driven targets
  └── IntelligenceService.cs       — Targets, leaderboard, customer analytics

src/MyDesk.Web/Services/
  └── TargetsProvider.cs           — Reads Config/targets.json with hot-reload

src/MyDesk.Web/Config/
  └── targets.json                 — Tune all targets without code changes

src/MyDesk.Web/Components/Shared/
  ├── TargetsView.razor            — Dashboard carousel view
  └── CustomerIntelligenceView.razor

src/MyDesk.Web/Components/Pages/Admin/
  └── UserProfile.razor            — Individual KPI & team comparison page
```

### Service registration (Program.cs)
```csharp
builder.Services.AddSingleton<ITargetsProvider, TargetsProvider>();
builder.Services.AddScoped<IntelligenceService>();
```

---

## 4. Tuning Targets

Edit `src/MyDesk.Web/Config/targets.json`:

```json
{
  "CompanyMonthlyTarget": 800000,
  "CompanyQuarterlyTarget": 2400000,
  "CompanyYearlyTarget": 9600000,

  "DefaultUserMonthlyTarget": 80000,
  "DefaultUserQuarterlyTarget": 240000,
  "DefaultUserYearlyTarget": 960000,

  "UserTargets": {
    "MD0229": { "Monthly": 120000, "Quarterly": 360000, "Yearly": 1440000 }
  }
}
```

Changes auto-apply within 30 seconds — no rebuild required.

---

## 5. Future High-Value Ideas

1. **Quote velocity** — time from quote created → won/lost (faster = better win rate)
2. **Sales cycle analytics** — average days a customer takes to buy after quote
3. **Commission forecaster** — if commission rules added, show user what they'll earn
4. **Predictive churn** — flag customers matching historical churn signals
5. **Product profitability** — cross-reference Products × Invoices to see margin-per-SKU
6. **Supplier risk** — concentrate Purchase Order spend to see dependency risk
7. **Geographic heatmap** — if customer addresses are available, by-region revenue map
8. **Compensation bands** — tie each user's target band to suggested bonus bands

---

## 6. Permissions Model (existing)

| Role          | UserTypeId | Dashboard Targets | Customer Intel | User Profiles |
|---------------|-----------:|:-----------------:|:--------------:|:-------------:|
| Admin         |          1 |        ✅         |       ✅        |       ✅       |
| Director      |          2 |        ✅         |       ✅        |       ✅       |
| Manager/User  |      3, 4+ |        ❌         |       ❌        |   Own only*   |

_*Future enhancement: let managers view profiles of users in their division._
