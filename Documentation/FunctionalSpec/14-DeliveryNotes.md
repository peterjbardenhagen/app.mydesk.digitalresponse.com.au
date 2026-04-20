# 14 — Delivery Notes

Status: **MINIMAL IMPLEMENTATION** — The DeliveryNotes folder exists but contains only a `Files/` subdirectory. Delivery note functionality is primarily implemented within the **Invoices** module (`Invoices/ViewDeliveryNote.asp`, `Invoices/GenerateDeliveryNote.asp`, etc.).

This document records the folder structure for completeness and notes the distinction between Delivery Notes (generated from Invoices) and Despatch Notes (carrier/package tracking).

---

## 1. Folder Structure

```
Clients/SalesEngineTL/DeliveryNotes/
└── Files/           (empty)
```

---

## 2. Functional Location

Delivery note functionality is implemented in the **Invoices** module:

| Function | Actual Location |
|---|---|
| View Delivery Note | `Invoices/ViewDeliveryNote.asp` |
| Print Delivery Note | `Invoices/ViewDeliveryNote.asp?Print=True` |
| Email Delivery Note | `Invoices/EmailDeliveryNote.asp` |
| Generate DN PDF | `Invoices/GenerateDeliveryNote.asp` → `/MyDeskASPNet/GenerateDeliveryNote.aspx` |
| Enter Despatch Details | `Invoices/EnterDespatchDetails.asp` |
| View Despatch Note | `Invoices/ViewDespatchNote.asp` |

---

## 3. Types of Delivery Documentation

The system distinguishes between three related concepts:

### 3.1 Delivery Note (DN)
- **Purpose**: Accompanies goods to customer; proof of delivery
- **Content**: Quantities and descriptions only (no pricing)
- **Source**: Generated from Invoice
- **Status Impact**: No direct status change
- **File**: `Invoices/Files/DN<InvoiceId>.pdf`

### 3.2 Despatch Note
- **Purpose**: Internal/carrier documentation with tracking details
- **Content**: Carrier name, reference number, package details
- **Source**: Entered via `EnterDespatchDetails.asp` after DN generated
- **View**: `ViewDespatchNote.asp` combines Invoice + Despatch table data

### 3.3 Invoice (with Delivery)
- **Purpose**: Billing document with delivery information
- **Content**: Full pricing plus delivery address
- **Source**: Core invoice document

---

## 4. Data Model

### 4.1 `Despatch` Table

| Column | Notes |
|---|---|
| `DespatchId` | AutoNumber PK |
| `InvoiceId` | FK Invoices |
| `Code` | User who entered details (⚠ currently empty in `_Proc`) |
| `DespatchDate` | Date of despatch |
| `Carrier` | Shipping company (50 char) |
| `CarrierRef` | Tracking reference (50 char) |
| `PackageDetails` | Description of packages (500 char) |
| `InternalNotes` | Internal handling notes (500 char) |

### 4.2 Invoice Delivery Fields

The `Invoices` table stores delivery addressing:

| Field | Purpose |
|---|---|
| `DelCompany` | Delivery recipient company name |
| `DelAddress` | Legacy text-area delivery address |
| `DelAddress1/2`, `DelSuburb`, `DelState`, `DelStateId`, `DelPostCode`, `DelCountry` | Structured delivery address (newer schema) |

---

## 5. Workflow

```
Invoice Created (Status 1)
    ↓
ViewDeliveryNote.asp printed or emailed
    ↓
Status promotes to 2 (Issued) if was 1
    ↓
EnterDespatchDetails.asp captures carrier info
    ↓
ViewDespatchNote.asp shows complete package tracking
```

---

## 6. PDF Generation

Delivery Note PDFs follow the same pattern as Invoices:

```
GenerateDeliveryNote.asp
  ↓ UPDATE InvoiceStatusId = 2 (if was 1)
  ↓ Redirect to /MyDeskASPNet/GenerateDeliveryNote.aspx
      ?InvoiceId=<n>&WorkingDir=/Clients/SalesEngineTL
      
GenerateDeliveryNote.aspx (.vb)
  ↓ ABCpdf renders ViewDeliveryNote.asp?InvoiceId=<n>&Email=True
  ↓ Save to Server.MapPath("/Clients/SalesEngineTL/DeliveryNotes/Files/DeliveryNote.pdf")
  ↓ Email or redirect as configured
```

**Note**: The output path in `EmailDeliveryNote_Proc.asp` references `/DeliveryNotes/Files/DeliveryNote.pdf` (not `/Invoices/Files/`), suggesting the .NET handler is configured to write to this DeliveryNotes folder.

---

## 7. Navigation

Delivery note actions are accessed from:

### Invoices/Default.asp Grid
- **Delivery Note** button per row → `ViewDeliveryNote.asp`

### Invoices/NavBar.asp (when viewing Invoice)
- **Print** (with confirmation prompt for status change)
- **Email** → `EmailDeliveryNote.asp`

### Invoices/NavBarDeliveryNote.asp (when viewing DN)
- **View Invoice** — switch to invoice view
- **Enter Despatch Details** — capture carrier info
- **Email** — send DN via email
- **Print** — print DN

---

## 8. Integration Points

| Module | Integration |
|---|---|
| **11-Invoices.md** | Delivery notes are generated from invoices; share the same header data |
| **50-ASPNet-Interop.md** | PDF generation via `GenerateDeliveryNote.aspx` |
| **10-Quotes.md** | Delivery addresses flow from Quote → Invoice → Delivery Note |

---

## 9. Known Baseline Issues

1. **Empty Code Field**: `EnterDespatchDetails_Proc.asp` doesn't set `Code` field in Despatch table (always empty string)

2. **Despatch Cascade Missing**: `Invoices/Del_Proc.asp` doesn't cascade delete `Despatch` rows, leaving orphans

3. **Folder vs. Files Mismatch**: Code references both `/Invoices/Files/DN<n>.pdf` and `/DeliveryNotes/Files/DeliveryNote.pdf` - actual save location depends on which handler (.NET vs ASP) generates

4. **Single PDF File**: The VB.NET fallback (`GenerateDeliveryNote.aspx`) may overwrite a single `DeliveryNote.pdf` file rather than generating per-ID files

---

## 10. Related Modules

- **11-Invoices.md** — Delivery notes are functionally part of the Invoice module
- **15-JobOrders.md** — Job delivery information flows through to invoices and delivery notes
