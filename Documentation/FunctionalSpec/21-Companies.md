# 21 — Companies

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Companies/`.

Company (customer/supplier) management. Companies are the parent entities for Contacts and provide the foundation for customer/supplier relationships across Quotes, Invoices, and Purchase Orders. The module is restricted to Manager-level users.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Filter + list page with alphabetical navigation. Manager-only access. |
| `IFrame.asp` | Data grid showing companies with action buttons. |
| `Add.asp` | Create company form. |
| `Add_Proc.asp` | Insert handler for Companies table. |
| `Edit.asp` | Edit company form. |
| `Edit_Proc.asp` | Update handler. |
| `Del_Proc.asp` | Delete company. |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Companies/` | Filter + list |
| `…/Companies/Add.asp` | New company form |
| `…/Companies/Edit.asp?CompanyId=<n>` | Edit company |
| `…/Companies/Del_Proc.asp?CompanyId=<n>` | Delete |
| `…/Companies/IFrame.asp` | Grid data for list |

---

## 3. Access Control

**Manager-Only Gate** (`Default.asp:10`):
```asp
If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")
```

Only users with Manager flag can access Companies list and management functions.

**Division Scope**: Companies are filtered by divisions where the user has Manager access (`DivisionIdsAccess("Manager")`).

---

## 4. Data Model

### 4.1 `Companies` Table

| Column | Data Type | Notes |
|---|---|---|
| `CompanyId` | AutoNumber | PK |
| `DivisionId` | Long Integer | FK Divisions |
| `Company` | Text(100) | Company name |
| `CustomerCode` | Text(50) | External/customer reference code |
| `Customer` | Boolean | Is a customer |
| `Supplier` | Boolean | Is a supplier |
| `Address1`, `Address2` | Text(50) | |
| `Suburb` | Text(50) | |
| `StateId` | Long Integer | FK States |
| `State` | Text(50) | Denormalized |
| `PostCode` | Text(10) | |
| `Country` | Text(50) | |
| `Phone`, `Fax` | Text(50) | |
| `Website` | Text(100) | |
| `ABN` | Text(20) | Australian Business Number |
| `ACN` | Text(20) | Australian Company Number |
| `Notes` | Memo | |
| `Terms` | Memo | Payment terms |
| `CreditLimit` | Currency | |
| `Visible` | Boolean | Show in dropdowns |
| `DateEntered` | DateTime | |
| `EnteredBy` | Text(50) | User code |

### 4.2 `CompanyTypes` Lookup

Companies can be categorized by type (referenced via `CompanyTypeId` if present).

---

## 5. UI Flow

### List Page (Default.asp)

**Breadcrumb**: Home / Setup / Companies

**Filters**:
- **Division**: Dropdown of Manager divisions
- **Starts With**: Alphabetical A-Z + 0-9 filter

**Grid Columns** (from `IFrame.asp`):
- Company name
- Customer Code
- Address
- Phone
- Action (Edit | Delete)

### Add/Edit Form

**Fields**:
- Company (required)
- Customer Code
- Customer / Supplier checkboxes
- Address fields
- Phone, Fax, Website
- ABN, ACN
- Terms (textarea)
- Credit Limit
- Notes (textarea)
- Visible (Yes/No)

**Validation**:
- Company name required
- Website must start with http:// if provided

---

## 6. Integration Points

| Module | Usage |
|---|---|
| **10-Quotes.md** | Company selector on Quote form |
| **11-Invoices.md** | Billing company |
| **12-PurchaseOrders.md** | Supplier company |
| **15-JobOrders.md** | Customer company |
| **20-Contacts.md** | Contacts belong to Companies |

---

## 7. Special Company: "Not an account" (ID 142)

CompanyId 142 is a special system record representing ad-hoc customers/suppliers without formal accounts. When selected:
- Contact forms show free-text Company Name field
- Invoices/Quotes allow one-off transactions
- No credit tracking applied

---

## 8. Known Baseline Issues

1. **No Cascade Delete Protection**: Deleting a company doesn't check for linked Contacts, Quotes, or Invoices. May create orphaned records.

2. **No Duplicate Prevention**: Same company name can be entered multiple times.

3. **ABN/ACN Validation**: No format validation on Australian Business/Company Numbers.

4. **Credit Limit Not Enforced**: Credit limit field exists but isn't checked when creating Quotes or Invoices.

5. **State/StateId Sync**: State name is denormalized but not automatically synced when StateId changes.

---

## 9. Related Modules

- **20-Contacts.md** — Contacts belong to Companies
- **32-Setup-Admin.md** — Companies under Setup menu
