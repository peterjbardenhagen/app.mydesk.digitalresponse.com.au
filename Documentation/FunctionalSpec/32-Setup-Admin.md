# 32 — Setup and Administration

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Setup/`.

The Setup module provides a centralized hub for system administration functions. It includes a modern card-based navigation interface linking to master data management, system configuration, and maintenance utilities.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Modern Setup hub with card-based navigation (53 KB). |
| `Maintenance.asp` | Database maintenance utilities placeholder. |
| `Script.asp` | Script execution utility. |
| `CreateTableHistory.asp` | Table history/audit log creation utility. |
| `Upgrade22Sept09.asp` | Legacy database upgrade script (reference). |
| `APIKeys/` | Directory for API key management (placeholder). |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Setup/` | Setup hub homepage |
| `…/Setup/Default.asp?Msg=<msg>` | Hub with status message |

---

## 3. Access Control

**Manager Gate** (currently relaxed):
```asp
If Not Request.Cookies("UserSettings")("Manager") Then
    ' Response.Redirect("../Portal/AccessDenied.asp") ' TODO: temporarily disabled
End If

isAdmin = True ' Temporarily grant full access
```

**Note**: The access control is currently commented out with a TODO noting that "Bert or any user that is Director or Administrator user type should have full access to everything in Setup."

---

## 4. Setup Hub Organization

The `Default.asp` presents a card-based navigation system organized into sections:

### 4.1 Administrator Functions

| Card | Target | Description |
|---|---|---|
| Activity Types | `../ActivityTypes` | Activity categories for time tracking |
| Conditions of Sale | `../QuoteCOS` | Terms, warranties, legal text for quotes |
| Copy Contacts | `../CopyContacts` | Duplicate contacts across divisions |
| Currency Rates | `../CurrencyRates` | Exchange rate management |
| Customer Origins | `../CustomerOrigins` | Lead source tracking |
| Data Maintenance | `../DataMaintenance` | Database cleanup utilities |
| Exchange Rates | `../ExchangeRates` | Alternative currency management |
| How Did You Hear | `../HowHear` | Marketing source tracking |
| Job Order Status | `../JobOrderStatus` | Job workflow status configuration |
| Locations | `../Locations` | Depot/office location management |
| Part Codes | `../PartCodes` | Purchase order product codes |
| Payment Types | `../PaymentTypes` | Invoice/PO payment methods |
| Product Types | `../ProductTypes` | Product categorization |
| Products | `../Products` | Product catalog |
| Projects | `../Projects` | Project/job categorization |
| Purchase Order Status | `../PurchaseOrderStatus` | PO workflow states |
| Purchase Order Product Types | `../POProductTypes` | PO line item types |
| Quote Status | `../QuoteStatus` | Quote workflow states |
| Sales Order Status | `../SalesOrderStatus` | Order fulfillment states |
| Shipping Companies | `../ShippingCompanies` | Courier/carrier list |
| States | `../States` | State/territory list |
| Table Comments | `../TableComments` | Record commenting system |
| User Roles | `../UserRoles` | Role templates with permissions |
| Users | `../Users` | User account management |

### 4.2 Manager Functions

| Card | Target | Description |
|---|---|---|
| Companies | `../Companies` | Customer/supplier management |
| Contacts | `../Contacts` | Contact management |
| Divisions | `../Divisions` | Business unit configuration |
| Parameters | `../Parameters` | System-wide settings |

### 4.3 User Functions

| Card | Target | Description |
|---|---|---|
| My Account | `../Users/Edit.asp` | Self-service profile editing |
| My Password | `../Users/EditPassword.asp` | Password change |

---

## 5. UI Design

The Setup hub uses a modern card-based design with:

- **Gradient Icon Backgrounds**: Each card has a unique color-coded icon
- **Hover Effects**: Cards lift and show arrow on hover
- **Responsive Grid**: Auto-fits columns based on viewport
- **Badges**: Section headers show access level indicators
- **Alert Banners**: Success/error messages with color-coded icons

**Card Structure**:
```
┌─────────────────────────────────────┐
│  ┌────┐  Card Title          →     │
│  │Icon│                             │
│  └────┘  Description text           │
└─────────────────────────────────────┘
```

---

## 6. Integration Points

| Module | Connection |
|---|---|
| **30-Users-Roles.md** | Users and UserRoles cards |
| **31-Divisions-Locations.md** | Divisions and Locations cards |
| **21-Companies.md** | Companies card |
| **20-Contacts.md** | Contacts card |
| **22-Products-PartCodes.md** | Products, PartCodes, ProductTypes cards |
| **16-Projects.md** | Projects card |
| **33-Parameters-TableComments-TableFiles.md** | Parameters, TableComments cards |
| **All Modules** | Status lookups (QuoteStatus, POStatus, etc.) |

---

## 7. Maintenance Utilities

### CreateTableHistory.asp
Creates audit trail tables for tracking record changes:
- `[TableName]History` tables
- Captures before/after values
- User and timestamp tracking

### Script.asp
Generic script execution wrapper for:
- Database migrations
- Bulk updates
- Custom maintenance tasks

### Maintenance.asp
Placeholder for future maintenance functions.

---

## 8. Known Baseline Issues

1. **Access Control Disabled**: Manager check is commented out with TODO. Currently all logged-in users see full Setup hub.

2. **Hardcoded isAdmin**: `isAdmin = True` grants full access regardless of role.

3. **Missing Cards**: Some linked modules (ActivityTypes, CopyContacts, CurrencyRates, etc.) may not exist in the codebase.

4. **No Module Existence Check**: Cards link to modules without checking if they exist.

5. **APIKeys Folder Empty**: Directory exists but contains no functionality.

6. **Legacy Upgrade Script**: `Upgrade22Sept09.asp` references outdated schema.

---

## 9. Related Modules

- All Setup sub-modules (referenced in cards)
- **03-Navigation-Header.md** — Setup link in main navigation
