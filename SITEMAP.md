# MyDesk Application Sitemap

> **Complete navigation hierarchy and page structure**
> Generated: 2026-05-03

---

## Overview

MyDesk is a comprehensive business management platform with three access tiers:
- **Main Portal**: Full-featured staff/admin interface
- **Customer Portal**: Client self-service access
- **Supplier Portal**: Vendor/supplier access

---

## 1. Public Pages (No Authentication Required)

```
/
├── /login                      - Staff login
├── /logout                     - Logout handler
├── /forgot-password            - Password reset
├── /customer-login             - Customer portal login
├── /supplier-login             - Supplier portal login
├── /privacy-policy             - Privacy policy
├── /terms-and-conditions       - Terms of service
└── /Error                      - Error display page
```

---

## 2. Main Portal (Authenticated)

### 2.1 Dashboard & Quick Access
```
├── /                           - Main Dashboard
├── /activity                   - Latest Activity Feed
├── /favourites                 - User Favourites
├── /files                      - Files Library
│   └── /files/folder/{id}      - Folder view
├── /ask-ai                     - AI Assistant (Ask Techlight AI)
└── /search-results             - Global search results
```

### 2.2 CRM Module
```
├── /contacts                   - Contacts List
│   ├── /contacts/create        - Create new contact
│   ├── /contacts/{id}          - View contact
│   └── /contacts/{id}/edit     - Edit contact
│
└── /companies                  - Companies List
    ├── /companies/new          - Create new company
    └── /companies/{id}/edit    - Edit company
```

### 2.3 Sales Module
```
├── /quotes                     - Quotes List
│   ├── /quotes/create          - Create new quote
│   ├── /quotes/{id}            - View quote
│   ├── /quotes/{id}/edit       - Edit quote
│   ├── /quotes/{id}/history    - Quote history
│   └── /quotes/copy-supplier   - Copy from supplier quote
│
├── /invoices                   - Invoices List
│   ├── /invoices/create        - Create new invoice
│   ├── /invoices/{id}          - View invoice
│   ├── /invoices/{id}/edit     - Edit invoice
│   ├── /invoices/{id}/history  - Invoice history
│   ├── /invoices/{id}/email    - Email invoice
│   ├── /invoices/{id}/despatch - Despatch invoice
│   ├── /invoices/{id}/delivery-note  - Delivery note
│   └── /invoices/myob-export   - MYOB export
│
├── /rfq                        - Request For Quote (RFQ) list
│   ├── /rfq/edit               - Create new RFQ
│   ├── /rfq/edit/{id}          - Edit RFQ
│   ├── /rfq/view/{id}          - View RFQ + supplier responses
│   └── /rfq/compare/{id}       - Side-by-side supplier comparison
│
├── /sales-projects             - Sales pipeline (Lead → Won/Lost)
│   ├── /sales-projects/edit    - Create new sales project
│   ├── /sales-projects/edit/{id} - Edit sales project
│   └── /sales-projects/view/{id} - View sales project
│
└── /call-reports               - Call Reports / customer interactions
    ├── /call-reports/edit      - New call report
    └── /call-reports/edit/{id} - Edit call report
```

### 2.4 Despatch Module
```
├── /despatch                   - Despatch List
└── /despatch/{id}              - View despatch
```

### 2.5 Purchasing Module
```
├── /purchase-orders            - Purchase Orders List
│   ├── /purchase-orders/create       - Create new PO
│   ├── /purchase-orders/{id}         - View PO
│   ├── /purchase-orders/{id}/edit    - Edit PO
│   ├── /purchase-orders/{id}/history - PO history
│   ├── /purchase-orders/{id}/email   - Email PO
│   └── /purchase-orders/{id}/invoice-details - Invoice details
│
├── /products                   - Products List
│   ├── /products/new           - Create new product
│   ├── /products/create        - Alternative create
│   └── /products/{id}/edit     - Edit product
│
├── /po-request                 - Vehicle Maintenance PO Request (email form)
│
└── /expenses                   - Expenses Management
```

### 2.6 Operations Module
```
├── /job-orders                 - Job Orders List
│   ├── /job-orders/create      - Create new job
│   ├── /joborders/create       - Alternative route
│   ├── /job-orders/{id}        - View job
│   └── /job-orders/{id}/edit   - Edit job
│
├── /timesheets                 - Timesheets
├── /timeshets                  - Timesheets (legacy alias — typo retained)
├── /timesheets/approve         - Approve direct-report timesheets (line-level)
├── /timesheets/missing         - Users missing a timesheet (current week)
├── /approvals/pending          - Multi-level Quote/PO approval queue
├── /staff-whereabouts          - Staff location tracking
└── /project-management         - Project management
```

### 2.7 DRM (Digital Response Management)
```
└── /drm                        - DRM Dashboard
    - Subscriptions management
    - Projects & timesheets
    - Charges tracking
    - Expense reports
    - O365 subscriptions
    - System credentials
```

### 2.8 Accounting & Finance (Admin/Director/Accounts only)
```
├── /accounts                   - Accounts Dashboard
├── /accounting                 - Accounting module
├── /reconciliation             - Aged Reconciliation
├── /reconciliation/sync        - MYOB Sync workflow
├── /aged-payables              - Aged Payables
└── /banking                    - Banking (stub)
```

### 2.9 Reports & Insights
```
├── /reports                    - Reports Dashboard
├── /reports/sales              - Sales Reports (5 charts: month/rep/division/YoY/pending-vs-won)
├── /noticeboard                - Company Noticeboard
└── /calendar                   - Calendar view
```

### 2.10 Marketing Hub
```
├── /marketing                  - Marketing Hub Home
├── /marketing/campaigns        - Email Campaigns
├── /marketing/assets           - Marketing Assets
├── /marketing/brand-assets     - Brand Assets
├── /marketing/strategy         - Marketing Strategy
├── /marketing/ai               - Marketing AI
├── /marketing/customers        - Customer Data Platform
└── /marketing/suppliers        - Supplier Data Platform
```

---

## 3. Admin Section (Admin/Director only)

### 3.1 System Administration
```
├── /admin                      - Admin Home/Setup
├── /admin/setup                - System Setup
├── /admin/platform             - Platform Settings
├── /admin/logs                 - Log Viewer
└── /admin/error-logs           - Error Logs
```

### 3.2 User Management
```
├── /admin/users                - Users List
│   ├── /admin/users/new        - Create user
│   ├── /admin/users/{id}/edit  - Edit user
│   └── /admin/users/{id}/profile - User profile
│
└── /admin/user-roles           - User Roles & Permissions
```

### 3.3 Reference Data
```
├── /admin/divisions            - Divisions
├── /admin/locations            - Locations
├── /admin/currency             - Currency settings
├── /admin/quote-status         - Quote status codes
├── /admin/invoice-status       - Invoice status codes
├── /admin/po-status            - PO status codes
├── /admin/job-order-status     - Job order status codes
├── /admin/activity-types       - Activity types
├── /admin/part-codes           - Part codes
└── /admin/parameters           - System parameters
```

### 3.4 Configuration
```
├── /admin/nav-menu             - Navigation menu settings
├── /admin/setup-menu           - Setup menu settings
├── /admin/brand-assets         - Brand asset management
└── /admin/ai-audit             - AI Audit Log
```

---

## 4. User Profile & Settings

```
├── /profile                    - User Profile
├── /settings                   - User Settings
└── /integrations               - Integrations
```

---

## 5. Help & Support

```
├── /help-center                - Help Centre
├── /help                       - Help redirect
├── /help/roadmap               - Product Roadmap
├── /help/ai-guide              - AI User Guide
├── /about                      - About MyDesk
└── /release-notes              - Release Notes
```

---

## 6. Customer Portal (Authenticated as Customer)

```
/customer-portal                - Customer Dashboard
/customer-portal/files          - Customer files
```

---

## 7. Supplier Portal (Authenticated as Supplier)

```
/supplier-portal                - Supplier Dashboard
```

---

## 8. Stub Routes (Development/Placeholder)

```
/admin/financial-year          - Financial Year admin (stub)
/banking                       - Banking (stub)
```

---

## Navigation Quick Reference

### Icon Legend
- **Dashboard**: Dashboard
- **Activity**: History
- **Favourites**: Star
- **Files**: Folder
- **Contacts**: Person
- **Companies**: Business
- **Quotes**: Description
- **Invoices**: Receipt
- **Despatch**: LocalShipping
- **Purchase Orders**: ShoppingBasket
- **Expenses**: Payment
- **Products**: Category
- **Job Orders**: Work
- **Timesheets**: Timer
- **Reports**: BarChart
- **Marketing**: Brush/Campaign
- **AI**: AutoAwesome
- **Settings**: Settings
- **Help**: HelpOutline

### Role-Based Access

| Section | Admin | Director | Manager | Staff | Accounts | Customer | Supplier |
|---------|-------|----------|---------|-------|----------|----------|----------|
| Dashboard | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| CRM | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Sales | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Purchasing | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Operations | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |
| DRM | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Accounts | ✓ | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Reports | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| Marketing | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Admin | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |

---

## File Structure

Pages are located in: `src/MyDesk.Web/Components/Pages/`

```
Pages/
├── Admin/
│   ├── ErrorLogs.razor
│   ├── UserEdit.razor
│   ├── UsersList.razor
│   ├── UserProfile.razor
│   ├── UserRolesAdmin.razor
│   ├── PlatformAdmin.razor
│   ├── SetupHome.razor
│   ├── SetupMenuSettings.razor
│   ├── NavMenuSettings.razor
│   ├── BrandAssets.razor
│   ├── AiAuditLog.razor
│   ├── LogViewer.razor
│   ├── Divisions.razor
│   ├── Locations.razor
│   ├── CurrencyAdmin.razor
│   ├── QuoteStatusAdmin.razor
│   ├── InvoiceStatusAdmin.razor
│   ├── POStatusAdmin.razor
│   ├── JobOrderStatusAdmin.razor
│   ├── ActivityTypesAdmin.razor
│   ├── PartCodesAdmin.razor
│   └── ParametersAdmin.razor
├── Quotes/
│   ├── QuotesList.razor
│   ├── QuoteEdit.razor
│   ├── QuoteView.razor
│   ├── QuoteHistory.razor
│   └── CopySupplierQuote.razor
├── Invoices/
│   ├── InvoicesList.razor
│   ├── InvoiceEdit.razor
│   ├── InvoiceView.razor
│   ├── InvoiceHistory.razor
│   ├── InvoiceEmail.razor
│   ├── DespatchEdit.razor
│   ├── DeliveryNoteView.razor
│   └── MYOBExport.razor
├── Contacts/
│   ├── ContactsList.razor
│   ├── ContactEdit.razor
│   ├── ContactView.razor
│   └── ImportContactsDialog.razor
├── Companies/
│   ├── CompaniesList.razor
│   └── CompanyEdit.razor
├── PurchaseOrders/
│   ├── PurchaseOrdersList.razor
│   ├── PurchaseOrderEdit.razor
│   ├── PurchaseOrderView.razor
│   ├── PurchaseOrderInvoiceDetails.razor
│   ├── PurchaseOrderEmail.razor
│   └── POHistory.razor
├── Products/
│   ├── ProductsList.razor
│   └── ProductEdit.razor
├── JobOrders/
│   ├── JobOrdersList.razor
│   ├── JobOrderEdit.razor
│   └── JobOrderView.razor
├── Despatch/
│   ├── DespatchList.razor
│   └── DespatchView.razor
├── Marketing/
│   ├── MarketingHub.razor
│   ├── MarketingHome.razor
│   ├── EmailCampaigns.razor
│   ├── BrandAssets.razor
│   ├── MarketingStrategy.razor
│   ├── MarketingAI.razor
│   ├── CustomerDataPlatform.razor
│   ├── SupplierDataPlatform.razor
│   ├── CampaignEditorDialog.razor
│   ├── UploadAssetDialog.razor
│   └── AddEditLinkDialog.razor
├── Reports/
│   └── Reports.razor
├── Reconciliation/
│   ├── ReconciliationDashboard.razor
│   ├── MyobSyncWorkflow.razor
│   └── AgedPayables.razor
├── CustomerPortal/
│   ├── CustomerLogin.razor
│   ├── CustomerDashboard.razor
│   └── CustomerFiles.razor
├── SupplierPortal/
│   ├── SupplierLogin.razor
│   └── SupplierDashboard.razor
├── Help/
│   ├── HelpCenter.razor
│   ├── AiUserGuide.razor
│   ├── ProductRoadmap.razor
│   └── ReleaseNotes.razor
├── Noticeboard/
│   └── NoticeboardList.razor
├── Activity/
│   └── RecentActivity.razor
├── Index.razor
├── Dashboard.razor
├── Login.razor
├── Files.razor
├── Expenses.razor
├── Timesheets.razor
├── StaffWhereabouts.razor
├── ProjectManagement.razor
├── DRM.razor
├── Accounting.razor
├── Calendar.razor
├── Favourites.razor
├── Settings.razor
├── Profile.razor
├── Integrations.razor
├── AskAI.razor
├── SearchResults.razor
├── StubRoutes.razor
├── AccessDenied.razor
├── PrivacyPolicy.razor
├── TermsAndConditions.razor
└── Error.razor
```

---

*Last updated: 2026-05-03*
