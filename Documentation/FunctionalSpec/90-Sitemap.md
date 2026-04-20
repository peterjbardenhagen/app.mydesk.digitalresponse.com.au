# 90 — Sitemap

Complete URL structure and navigation map for Techlight MyDesk.

---

## 1. Root Level

| URL | Purpose | Access |
|---|---|---|
| `/` | Redirects to `/Portal.asp` | Public |
| `/Portal.asp` | Main entry point, login | Public |
| `/PortalFrame.asp` | Application frame | Logged In |
| `/Dashboard.asp` | Post-login redirect | Logged In |

---

## 2. Client Folders

### 2.1 Main Modules

| URL | Module | Access |
|---|---|---|
| `/Clients/SalesEngineTL/` | Client root (redirects) | — |
| `/Clients/SalesEngineTL/Dashboard.asp` | User dashboard | Logged In |
| `/Clients/SalesEngineTL/Portal.asp` | Client portal entry | Logged In |
| `/Clients/SalesEngineTL/PortalFrame.asp` | Client frame | Logged In |

### 2.2 Quotes

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Quotes/` | List/filter quotes |
| `/Clients/SalesEngineTL/Quotes/Default.asp` | Quote list |
| `/Clients/SalesEngineTL/Quotes/Add.asp` | Create quote |
| `/Clients/SalesEngineTL/Quotes/Add2.asp` | Quote form step 2 |
| `/Clients/SalesEngineTL/Quotes/Edit.asp?Qid=<n>` | Edit quote |
| `/Clients/SalesEngineTL/Quotes/View.asp?Qid=<n>` | View/print quote |
| `/Clients/SalesEngineTL/Quotes/Email.asp?Qid=<n>` | Email quote |
| `/Clients/SalesEngineTL/Quotes/GenerateQuote.asp` | Generate PDF |
| `/Clients/SalesEngineTL/Quotes/UpdateStatus.asp?Qid=<n>` | Change status |
| `/Clients/SalesEngineTL/Quotes/EnterDespatchDetails.asp?InvoiceId=<n>` | Enter despatch info |
| `/Clients/SalesEngineTL/Quotes/ViewDespatchNote.asp?InvoiceId=<n>` | View despatch note |
| `/Clients/SalesEngineTL/Quotes/ViewDeliveryNote.asp?InvoiceId=<n>` | View delivery note |
| `/Clients/SalesEngineTL/Quotes/Transporter_QuoteToJob.asp?Qid=<n>` | → Job Orders |
| `/Clients/SalesEngineTL/Quotes/Transporter_QuoteToInvoice.asp?Qid=<n>` | → Invoices |

### 2.3 Invoices

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Invoices/` | Invoice list |
| `/Clients/SalesEngineTL/Invoices/Default.asp` | List/filter |
| `/Clients/SalesEngineTL/Invoices/Add.asp[?Qid=<n>][?JobOrderId=<n>]` | Create invoice |
| `/Clients/SalesEngineTL/Invoices/Edit.asp?InvoiceId=<n>` | Edit invoice |
| `/Clients/SalesEngineTL/Invoices/View.asp?InvoiceId=<n>` | View/print |
| `/Clients/SalesEngineTL/Invoices/Email.asp?InvoiceId=<n>` | Email invoice |
| `/Clients/SalesEngineTL/Invoices/GenerateInvoice.asp` | PDF generation |
| `/Clients/SalesEngineTL/Invoices/UpdateStatus.asp?InvoiceId=<n>` | Status change |
| `/Clients/SalesEngineTL/Invoices/EnterMYOBDetails.asp?InvoiceId=<n>` | MYOB export data |
| `/Clients/SalesEngineTL/Invoices/EnterDespatchDetails.asp?InvoiceId=<n>` | Despatch entry |
| `/Clients/SalesEngineTL/Invoices/EmailDeliveryNote.asp?InvoiceId=<n>` | Email DN |
| `/Clients/SalesEngineTL/Invoices/ViewDeliveryNote.asp?InvoiceId=<n>` | View DN |
| `/Clients/SalesEngineTL/Invoices/ViewDespatchNote.asp?InvoiceId=<n>` | View despatch |

### 2.4 Purchase Orders

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/PurchaseOrders/` | PO list |
| `/Clients/SalesEngineTL/PurchaseOrders/Default.asp` | Filter/list |
| `/Clients/SalesEngineTL/PurchaseOrders/Add.asp` | Division selector |
| `/Clients/SalesEngineTL/PurchaseOrders/Add2.asp[?RFQid=<n>][?Qid=<n>]` | Create PO |
| `/Clients/SalesEngineTL/PurchaseOrders/Edit.asp?POid=<n>` | Edit PO |
| `/Clients/SalesEngineTL/PurchaseOrders/View.asp?POid=<n>[&Print=True][&Email=True]` | View/print |
| `/Clients/SalesEngineTL/PurchaseOrders/ViewRequest.asp?POid=<n>` | View as request |
| `/Clients/SalesEngineTL/PurchaseOrders/Approve.asp?POid=<n>` | Approve action |
| `/Clients/SalesEngineTL/PurchaseOrders/Decline.asp?POid=<n>` | Decline action |
| `/Clients/SalesEngineTL/PurchaseOrders/UpdateStatus.asp?POid=<n>` | Status change |
| `/Clients/SalesEngineTL/PurchaseOrders/Email.asp?POid=<n>` | Email compose |
| `/Clients/SalesEngineTL/PurchaseOrders/GeneratePO.asp` | PDF generation |
| `/Clients/SalesEngineTL/PurchaseOrders/EnterInvoiceDetails.asp?POid=<n>` | Invoice details |
| `/Clients/SalesEngineTL/PurchaseOrders/ViewHistory.asp?POid=<n>` | Audit trail |
| `/Clients/SalesEngineTL/PurchaseOrders/GenerateFromRFQ.asp?RFQid=<n>` | Create from RFQ |
| `/Clients/SalesEngineTL/PurchaseOrders/Report.asp` | PO report |

### 2.5 Job Orders

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/JobOrders/` | Job monitoring list |
| `/Clients/SalesEngineTL/JobOrders/Default.asp` | Filter/list |
| `/Clients/SalesEngineTL/JobOrders/Add.asp?Qid=<n>` | Create from quote |
| `/Clients/SalesEngineTL/JobOrders/Edit.asp?JobOrderId=<n>&JobOrderContentId=<n>` | Edit line item |
| `/Clients/SalesEngineTL/JobOrders/EditJobOrder.asp?JobOrderId=<n>` | Edit header |
| `/Clients/SalesEngineTL/JobOrders/View.asp?JobOrderId=<n>` | Picking slip |
| `/Clients/SalesEngineTL/JobOrders/ViewHistory.asp?JobOrderId=<n>` | Job history |
| `/Clients/SalesEngineTL/JobOrders/Transporter.asp?JobOrderId=<n>` | → Invoice |
| `/Clients/SalesEngineTL/JobOrders/Report.asp` | Job report |

### 2.6 Contacts

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Contacts/` | Contact list |
| `/Clients/SalesEngineTL/Contacts/Default.asp` | Filter/list |
| `/Clients/SalesEngineTL/Contacts/Add.asp` | New contact |
| `/Clients/SalesEngineTL/Contacts/AddNewWin.asp` | Popup add |
| `/Clients/SalesEngineTL/Contacts/Edit.asp?ContactId=<n>` | Edit contact |
| `/Clients/SalesEngineTL/Contacts/View.asp?ContactId=<n>` | View card |
| `/Clients/SalesEngineTL/Contacts/DeliveryAddress.asp?ContactId=<n>` | Delivery address |
| `/Clients/SalesEngineTL/Contacts/InvoiceAddress.asp?ContactId=<n>` | Invoice address |
| `/Clients/SalesEngineTL/Contacts/Report.asp` | Contact report |

### 2.7 Companies

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Companies/` | Company list |
| `/Clients/SalesEngineTL/Companies/Default.asp` | Filter/list |
| `/Clients/SalesEngineTL/Companies/Add.asp` | New company |
| `/Clients/SalesEngineTL/Companies/Edit.asp?CompanyId=<n>` | Edit company |

### 2.8 Products & Part Codes

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Products/` | Division selector |
| `/Clients/SalesEngineTL/Products/Default2.asp?DivisionId=<n>` | Product list |
| `/Clients/SalesEngineTL/Products/Add.asp` | New product |
| `/Clients/SalesEngineTL/Products/Edit.asp?ProductId=<n>` | Edit product |
| `/Clients/SalesEngineTL/Products/Select.asp` | Modal selector |
| `/Clients/SalesEngineTL/PartCodes/` | Part code list |
| `/Clients/SalesEngineTL/PartCodes/Add.asp` | New part code |
| `/Clients/SalesEngineTL/PartCodes/Edit.asp?PartCodeId=<n>` | Edit part code |

### 2.9 Projects

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Projects/` | Project list |
| `/Clients/SalesEngineTL/Projects/Add.asp` | New project |
| `/Clients/SalesEngineTL/Projects/Edit.asp?ProjectId=<n>` | Edit project |

---

## 3. Setup & Administration

### 3.1 Setup Hub

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Setup/` | Setup hub |
| `/Clients/SalesEngineTL/Setup/Default.asp` | Setup navigation |

### 3.2 Master Data

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Users/` | User management |
| `/Clients/SalesEngineTL/Users/Add.asp` | New user |
| `/Clients/SalesEngineTL/Users/Edit.asp?UserId=<n>` | Edit user |
| `/Clients/SalesEngineTL/Users/EditPassword.asp?Code=<str>` | Change password |
| `/Clients/SalesEngineTL/Divisions/` | Division management |
| `/Clients/SalesEngineTL/Locations/` | Location management |
| `/Clients/SalesEngineTL/Parameters/` | System parameters |
| `/Clients/SalesEngineTL/TableComments/` | Comments management |

---

## 4. Supporting Modules

### 4.1 Portal & Access Control

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Portal/` | Portal folder |
| `/Clients/SalesEngineTL/Portal/AccessDenied.asp` | Access denied page |
| `/Clients/SalesEngineTL/Portal/Validate.asp` | Validation |
| `/Clients/SalesEngineTL/Portal/Validate_Portal.asp` | Portal validation |

### 4.2 Purchasing (Navigation Hub)

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Purchasing/Default.asp` | Purchasing hub |

### 4.3 Reports

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Reports/` | Reports hub |
| `/Clients/SalesEngineTL/Reports/Default.asp` | Report navigation |
| `/Clients/SalesEngineTL/Reports/SalesReportGen.asp` | Sales report generator |
| `/Clients/SalesEngineTL/Reports/SalesReport.asp` | Sales report |
| `/Clients/SalesEngineTL/Reports/PurchaseOrders_ByMonth_ByDivision.asp` | PO analytics |

---

## 5. System Folders

### 5.1 Shared System Includes

| URL | Purpose |
|---|---|
| `/System/` | System folder |
| `/System/ssi_*.inc` | Server-side includes |
| `/System/ssi_*.asp` | ASP function libraries |
| `/System/Constants.asp` | Global constants |
| `/System/Style_Techlight.css` | Main stylesheet |
| `/System/Style_Modern.css` | Modern UI styles |
| `/System/Style2.css` | Legacy styles |
| `/System/Global.js` | JavaScript utilities |
| `/System/grid.js` | Grid control |
| `/System/paging1.js` | Pagination |
| `/System/cal2.js` | Calendar control |

### 5.2 Images

| URL | Purpose |
|---|---|
| `/Images/` | Image assets |
| `/images/[DivisionLogo]` | Division logos |
| `/favicon.ico` | Site favicon |

---

## 6. ASP.NET Interop

| URL | Purpose |
|---|---|
| `/MyDeskASPNet/` | .NET handlers |
| `/MyDeskASPNet/GenerateQuote.aspx` | Quote PDF |
| `/MyDeskASPNet/GenerateInvoice.aspx` | Invoice PDF |
| `/MyDeskASPNet/GeneratePurchaseOrder.aspx` | PO PDF |
| `/MyDeskASPNet/GenerateDeliveryNote.aspx` | DN PDF |
| `/MyDeskASPNet/ScrapeToPDF.aspx` | Generic PDF |

---

## 7. Planned/Unimplemented URLs

| URL | Status | Notes |
|---|---|---|
| `/Clients/SalesEngineTL/GlobalSearch.asp` | Planned | Global search results |
| `/Clients/SalesEngineTL/AskAI.asp` | Planned | AI assistant |
| `/Clients/SalesEngineTL/CallReports/` | Planned | Call logging module |
| `/Clients/SalesEngineTL/RFQ/` | Planned | RFQ module UI |
| `/Clients/SalesEngineTL/TableFiles/` | Planned | File attachments |

---

## 8. Query Parameter Reference

### Common Parameters

| Parameter | Used In | Description |
|---|---|---|
| `Msg` | Most Default.asp pages | Status message to display |
| `Cache` | IFrame.asp pages | Cache-busting random value |
| `DivisionId` | Most modules | Filter by division |
| `Code` | Most modules | Filter by user code |
| `DateFrom`, `DateTo` | List pages | Date range filter |

### Entity-Specific Parameters

| Parameter | Entity | Description |
|---|---|---|
| `Qid` | Quotes | Quote ID |
| `InvoiceId` | Invoices | Invoice ID |
| `POid` | PurchaseOrders | PO ID |
| `JobOrderId` | JobOrders | Job ID |
| `ContactId` | Contacts | Contact ID |
| `CompanyId` | Companies | Company ID |
| `ProductId` | Products | Product ID |
| `ProjectId` | Projects | Project ID |
| `UserId` | Users | User ID |
| `RFQid` | RFQ | RFQ ID (planned) |

---

## 9. Navigation Flow Diagram

```
/Portal.asp (Login)
    ↓
/Dashboard.asp (Home)
    ↓
    ├─→ /Quotes/ (Sales)
    ├─→ /Invoices/ (Billing)
    ├─→ /PurchaseOrders/ (Procurement)
    ├─→ /JobOrders/ (Operations)
    ├─→ /Contacts/ (CRM)
    ├─→ /Reports/ (Analytics)
    ├─→ /Setup/ (Administration)
    └─→ /MyDeskASPNet/ (PDF Generation)
```
