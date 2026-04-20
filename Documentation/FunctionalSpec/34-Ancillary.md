# 34 — Ancillary Modules

Status: **IN REVIEW** — Modules that support the core workflow but are not primary transaction systems.

This document covers smaller, supporting modules that provide utility functions, lookup tables, and cross-cutting features used across the application.

---

## 1. Activity Types

### Purpose
Categorize time tracking activities and project work types.

### Expected Files (referenced but may not exist)
- `ActivityTypes/Default.asp` — List
- `ActivityTypes/Add.asp` — Create
- `ActivityTypes/Edit.asp` — Modify

### Data Model

**`ActivityTypes` Table**
| Column | Notes |
|---|---|
| `ActivityTypeId` | PK |
| `ActivityType` | Name (e.g., "Sales Call", "Site Visit") |
| `DivisionId` | Scope |
| `Billable` | Boolean |
| `Visible` | Boolean |

### Integration
- Used in timesheet entry
- Links to Quotes for time allocation

---

## 2. Conditions of Sale (QuoteCOS)

### Purpose
Manage terms and conditions, warranties, and legal text for customer quotes.

### Expected Files
- `QuoteCOS/Default.asp`
- `QuoteCOS/Add.asp`
- `QuoteCOS/Edit.asp`

### Data Model

**`QuoteCOS` (Conditions of Sale)**
| Column | Notes |
|---|---|
| `QuoteCOSId` | PK |
| `DivisionId` | Scope |
| `COSText` | Memo - full terms text |
| `Visible` | Boolean |

### Integration
- Selected on Quote form
- Printed on Quote PDF

---

## 3. Copy Contacts

### Purpose
Utility for duplicating and managing contact records across divisions.

### Expected Files
- `CopyContacts/Default.asp`
- `CopyContacts/Copy.asp`

### Functionality
- Select source contact
- Select target division(s)
- Copy with or without history
- Bulk contact operations

---

## 4. Currency Rates / Exchange Rates

### Purpose
Manage foreign exchange rates for multi-currency transactions.

### Data Model

**`CurrencyRates`** or **`ExchangeRates`**
| Column | Notes |
|---|---|
| `RateId` | PK |
| `FromCurrency` | Text(3) - e.g., "AUD" |
| `ToCurrency` | Text(3) - e.g., "USD" |
| `Rate` | Decimal |
| `EffectiveDate` | DateTime |
| `EnteredBy` | User code |

### Integration
- Quote/Invoice currency conversion
- Historical rate lookup

---

## 5. Customer Origins / How Did You Hear

### Purpose
Track marketing sources and lead origins.

### Data Model

**`CustomerOrigins`** / **`HowHear`**
| Column | Notes |
|---|---|
| `OriginId` | PK |
| `Origin` | Name (e.g., "Google Ad", "Referral") |
| `Visible` | Boolean |

### Integration
- Selected on Contact/Company form
- Marketing source reports

---

## 6. Data Maintenance

### Purpose
Database cleanup and integrity utilities.

### Expected Functions
- Remove orphaned records
- Archive old transactions
- Rebuild indexes
- Fix broken foreign keys
- Compress database

---

## 7. Job Order Status

### Purpose
Configure workflow states for job orders.

### Data Model

**`JobOrderStatus`** (already documented in 15-JobOrders.md)
| Code | Status |
|---|---|
| 10 | New |
| 20 | Picking |
| ... | ... |

### Management
- Add custom statuses
- Set display order
- Define color coding

---

## 8. Payment Types

### Purpose
Manage payment method options.

### Data Model

**`PaymentTypes`** (Invoice)
| Column | Notes |
|---|---|
| `PaymentTypeId` | PK |
| `PaymentType` | Name (e.g., "Credit Card", "EFT") |
| `Visible` | Boolean |

**`PurchaseOrderPaymentTypes`** (PO)
| Column | Notes |
|---|---|
| `POPaymentTypeId` | PK |
| `POPaymentType` | Name |

---

## 9. Product Types / PO Product Types

### Purpose
Categorize products for Quotes and Purchase Orders.

### Data Model

**`ProductTypes`**
| Column | Notes |
|---|---|
| `ProductTypeId` | PK |
| `DivisionId` | Scope |
| `ProductType` | Name |
| `Visible` | Boolean |

**`PurchaseOrderProductTypes`**
| Column | Notes |
|---|---|
| `POProductTypeId` | PK |
| `DivisionId` | Scope |
| `POProductType` | Name |
| `CapEx` | Capital expenditure flag |
| `Visible` | Boolean |

---

## 10. Quote Status / Sales Order Status

### Purpose
Configure workflow states for quotes and sales orders.

### Data Model

**`QuoteStatus`**
| ID | Status |
|---|---|
| 1 | Draft |
| 2 | Sent |
| 3 | Accepted |
| 4 | Declined |
| 5 | Expired |

**`SalesOrderStatus`**
| ID | Status |
|---|---|
| 1 | Pending |
| 2 | Confirmed |
| 3 | Processing |
| 4 | Shipped |
| 5 | Invoiced |

---

## 11. Shipping Companies

### Purpose
Manage courier and carrier list.

### Data Model

**`ShippingCompanies`**
| Column | Notes |
|---|---|
| `ShippingCompanyId` | PK |
| `ShippingCompany` | Name (e.g., "TNT", "StarTrack") |
| `Visible` | Boolean |

### Integration
- Despatch note carrier selection
- Tracking URL templates

---

## 12. States

### Purpose
State/territory reference data.

### Data Model

**`States`**
| Column | Notes |
|---|---|
| `StateId` | PK |
| `State` | Name (e.g., "NSW", "Victoria") |
| `Country` | Australia/New Zealand |

---

## 13. User Roles

### Purpose
Define role templates with permissions and approval limits.

### Data Model

**`UserRoles`** (detailed in 30-Users-Roles.md)
| Column | Notes |
|---|---|
| `UserRoleId` | PK |
| `UserRole` | Name (e.g., "Sales Rep") |
| `POApprovalLimit` | Currency |
| `Quotes`, `Invoices`, etc. | Module access flags |

---

## 14. Implementation Status Summary

| Module | Status | Location |
|---|---|---|
| Activity Types | Planned | Referenced in Setup |
| Conditions of Sale | Planned | Referenced in Setup |
| Copy Contacts | Planned | Referenced in Setup |
| Currency Rates | Planned | Referenced in Setup |
| Customer Origins | Planned | Referenced in Setup |
| Data Maintenance | Planned | Referenced in Setup |
| Job Order Status | Implemented | In database |
| Payment Types | Implemented | In database |
| Product Types | Implemented | In database |
| PO Product Types | Implemented | In database |
| Quote Status | Implemented | In database |
| Sales Order Status | Partial | Some overlap with QuoteStatus |
| Shipping Companies | Planned | Referenced in Setup |
| States | Implemented | In database |
| User Roles | Implemented | Full module exists |

---

## 15. Common Patterns

### Lookup Table Structure
Most ancillary modules follow this pattern:

```
[TableName]
- [TableName]Id (PK, AutoNumber)
- DivisionId (FK, for scoped tables)
- [TableName] (Text, the value)
- Visible (Boolean, show in dropdowns)
- InOrder (Integer, sort order)
```

### UI Pattern
- List page with Add button
- Simple Add/Edit form
- Delete with confirmation
- No complex workflows

---

## 16. Related Modules

- **32-Setup-Admin.md** — All ancillary modules accessed via Setup hub
- **All Transaction Modules** — Use lookup tables for dropdowns
