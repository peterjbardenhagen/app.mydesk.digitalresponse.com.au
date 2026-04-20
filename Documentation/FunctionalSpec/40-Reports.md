# 40 — Reports

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Reports/`.

Business intelligence and analytics module providing sales reports, purchase order analysis, and data visualization. The Reports module uses a modern card-based hub interface linking to specific report generators.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Modern reports hub with card-based navigation. Director-only access. |
| `SalesReportGen.asp` | Sales report generator interface. |
| `SalesReport.asp` | Main sales report (45 KB) with extensive filtering. |
| `SalesReport_All.asp` | Comprehensive sales report with drill-down (27 KB). |
| `SalesReport_Data1-7.asp` | Data partials for AJAX loading. |
| `SalesReportEnterData_Proc.asp` | Data entry processor for report inputs. |
| `SalesReportError.asp` | Error display for report generation failures. |
| `PurchaseOrders_ByMonth_ByDivision.asp` | PO spend analysis by time and division. |
| `Chart.asp` | Chart generation utility. |
| `Upload.asp` | Data upload for report inputs. |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Reports/` | Reports hub |
| `…/Reports/SalesReportGen.asp` | Sales report generator |
| `…/Reports/SalesReport.asp` | Detailed sales report |
| `…/Reports/SalesReport_All.asp` | Full sales analysis |
| `…/Reports/PurchaseOrders_ByMonth_ByDivision.asp` | PO analytics |
| `…/Reports/Chart.asp` | Chart generation |

---

## 3. Access Control

**Director-Only Gate** (`Default.asp:18`):
```asp
If Not Request.Cookies("UserSettings")("UserTypeId") => 6 Then
    Response.Redirect("../Portal/AccessDenied.asp")
End If
```

Reports are restricted to UserTypeId ≥ 6 (Director level).

---

## 4. Reports Hub (Default.asp)

Modern card-based interface organized into sections:

### 4.1 Sales Reports

| Report | File | Description |
|---|---|---|
| Sales Report | `SalesReportGen.asp` | Date range, division, and sales rep filtering |

### 4.2 Purchase Orders

| Report | File | Description |
|---|---|---|
| PO by Month & Division | `PurchaseOrders_ByMonth_ByDivision.asp` | Spend analysis by time period |

### 4.3 Additional Reports

Section marked "Coming Soon" with placeholder for future reports:
- Customer analysis
- Product performance
- User activity
- Trend analysis

---

## 5. Sales Report

### 5.1 SalesReportGen.asp

Report generator interface allowing selection of:
- Date range (From/To)
- Division(s)
- Sales representative(s)
- Report type (Summary/Detailed)

### 5.2 SalesReport.asp

Comprehensive 45 KB report processor featuring:

**Input Parameters**:
- `DateFrom`, `DateTo` — Date range
- `DivisionId` — Single or multiple divisions
- `Code` — Sales rep filter
- `ReportType` — Format selector

**Output Sections**:
- Summary header with filter echo
- Division breakdown
- Sales rep performance
- Product category analysis
- Trend charts
- Running totals and averages

### 5.3 SalesReport_All.asp

Full analytics report (27 KB) with:
- Multi-division comparison
- Historical trend analysis
- Year-over-year calculations
- Interactive drill-down
- Export preparation

### 5.4 Data Partials (SalesReport_Data1-7.asp)

AJAX-loaded data segments for:
1. Header summary
2. Division data
3. Sales rep data
4. Product data
5. Customer data
6. Trend data
7. Chart data

---

## 6. Purchase Order Reports

### 6.1 PurchaseOrders_ByMonth_ByDivision.asp

Spend analysis report showing:

**Dimensions**:
- Month (horizontal axis)
- Division (vertical axis)
- PO count and total value

**Features**:
- Color-coded heat map
- Running totals
- Budget comparison (if configured)
- Export to Excel

---

## 7. Chart Generation

### 7.1 Chart.asp

Dynamic chart generator supporting:
- Bar charts
- Line trends
- Pie charts
- Multi-series comparison

**Parameters**:
- `Type` — Chart type
- `Data` — JSON or query reference
- `Width`, `Height` — Dimensions
- `Title` — Chart header

---

## 8. Data Model

Reports query the transactional tables directly:

### Primary Sources

| Report | Primary Tables |
|---|---|
| Sales Report | `Quotes`, `QuoteContents`, `Invoices`, `InvoiceContents` |
| PO Report | `PurchaseOrders`, `PurchaseOrderContents` |
| Customer Analysis | `Companies`, `Contacts`, `Quotes` |

### Computed Fields

Reports calculate:
- Revenue totals (ex/inc GST)
- Margin percentages
- Growth rates
- Moving averages
- Year-to-date accumulation

---

## 9. Export Options

Reports support export to:
- **PDF** — Via ASP.NET interop (`/MyDeskASPNet/`)
- **Excel** — CSV or XLS generation
- **Print** — Optimized print stylesheet

---

## 10. Integration Points

| Module | Connection |
|---|---|
| **10-Quotes.md** | Sales data source |
| **11-Invoices.md** | Revenue data source |
| **12-PurchaseOrders.md** | Spend data source |
| **50-ASPNet-Interop.md** | PDF generation |
| **03-Navigation-Header.md** | Reports link in header |

---

## 11. Known Baseline Issues

1. **Director-Only Access**: Very restrictive — managers cannot view their own performance.

2. **No Scheduled Reports**: No automated email delivery of daily/weekly reports.

3. **No Dashboard Widgets**: Report data not exposed on main dashboard.

4. **Hardcoded Report Types**: Adding new reports requires code changes.

5. **AJAX Loading**: Data partials may fail if session expires during load.

6. **Limited Chart Types**: Only basic chart formats supported.

7. **No Real-Time Data**: Reports query database directly — may impact performance during heavy use.

---

## 12. Related Modules

- **03-Navigation-Header.md** — Reports link in main navigation
- **50-ASPNet-Interop.md** — PDF generation endpoint
