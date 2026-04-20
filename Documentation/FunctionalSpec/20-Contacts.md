# 20 — Contacts

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Contacts/`.

Contact management for customers and suppliers. Contacts are linked to Companies and used throughout the system for Quotes, Invoices, Purchase Orders, and Job Orders. The module supports full CRUD with modern UI and address management.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Modern filter + list page with user/company filters. |
| `IFrame.asp` | Data grid showing contacts with company and contact details. |
| `Add.asp` | Create contact form (modern UI). |
| `Add_Proc.asp` | Insert handler for Contacts table. |
| `AddNewWin.asp` | Popup window version of Add form (for modal creation). |
| `AddNewWin_Proc.asp` | Processor for popup Add form. |
| `Edit.asp` | Edit contact form with full details. |
| `Edit_Proc.asp` | Update handler. |
| `View.asp` | Contact card view (read-only summary). |
| `Del_Proc.asp` | Delete contact. |
| `DeliveryAddress.asp` / `InvoiceAddress.asp` | Address management sub-forms for delivery/invoice addressing. |
| `EmailList.asp` | Simple email list selector. |
| `SelectEmailFromContact.asp` / `SelectFaxFromContact.asp` | Modal selectors for email/fax extraction. |
| `Report.asp` | Printable contact report. |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Contacts/` | Filter + list |
| `…/Contacts/Add.asp` | New contact form |
| `…/Contacts/AddNewWin.asp` | Popup contact add (for modal use) |
| `…/Contacts/Edit.asp?ContactId=<n>` | Edit contact |
| `…/Contacts/View.asp?ContactId=<n>` | View contact card |
| `…/Contacts/Del_Proc.asp?ContactId=<n>` | Delete |
| `…/Contacts/DeliveryAddress.asp?ContactId=<n>` | Manage delivery address |
| `…/Contacts/InvoiceAddress.asp?ContactId=<n>` | Manage invoice address |
| `…/Contacts/Report.asp` | Printable report |

---

## 3. Access Control

All pages use standard `ssi_Security.inc` login check. No additional division restrictions on the list view — users can see contacts across all divisions they have access to via `GetAccessCodesList`.

Contact visibility for dropdowns in other modules (Quotes, Invoices, etc.) is filtered by:
- User's Code (owner)
- Division access
- Visible flag

---

## 4. Data Model

### 4.1 `Contacts` Table

| Column | Data Type | Notes |
|---|---|---|
| `ContactId` | AutoNumber | PK |
| `Code` | Text(50) | FK Users - owner |
| `CompanyId` | Long Integer | FK Companies (142 = "Not an account") |
| `FirstName` | Text(50) | |
| `Surname` | Text(50) | |
| `Position` | Text(50) | Job title |
| `Phone` | Text(50) | |
| `Mobile` | Text(50) | |
| `Fax` | Text(50) | |
| `Email` | Text(100) | Validated with regex |
| `Website` | Text(100) | Must start with http:// |
| `Address1`, `Address2` | Text(50) | |
| `Suburb` | Text(50) | |
| `StateId` | Long Integer | FK States |
| `State` | Text(50) | Denormalized state name |
| `PostCode` | Text(10) | |
| `Country` | Text(50) | |
| `DelAddress1`, `DelAddress2`, `DelSuburb`, `DelState`, `DelStateId`, `DelPostCode`, `DelCountry` | | Delivery address (overrides default) |
| `InvAddress1`, `InvAddress2`, `InvSuburb`, `InvState`, `InvStateId`, `InvPostCode`, `InvCountry` | | Invoice address (overrides default) |
| `Visible` | Boolean | Show in dropdowns |
| `Customer`, `Supplier` | Boolean | Type flags |
| `Notes` | Memo | Free text notes |
| `DivisionId` | Long Integer | FK Divisions |

### 4.2 `Contacts_WithCustomersAndSuppliers` (View)

Combined view showing contacts with their company names and customer/supplier flags for easier querying.

### 4.3 `Contacts_WithCustomersAndSuppliers_V2` (View)

Enhanced version with additional computed fields used in Purchase Orders and Quotes.

---

## 5. UI Flow

### List Page (Default.asp)

**Modern UI Elements**:
- Breadcrumb: Home / Contacts
- "New Contact" button with icon
- Filter panel with User dropdown, Company selector
- Results in iframe grid

**Filter Options**:
- User (All users for Managers; own code for regular)
- Company (dropdown with "All companies" + "Not an account")
- Results update iframe via POST to `IFrame.asp`

### Add/Edit Form

**Required Fields** (client-side validation):
- First Name
- Surname
- Company (CompanyId or CCompany for non-accounts)
- Phone OR Mobile (at least one)

**Optional Fields**:
- Position
- Email (validated with regex: `^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$`)
- Website (must start with `http://`)
- Address fields
- Notes (500 char limit)

**Company Selection Pattern**:
- Dropdown shows existing companies
- "Not an account" (142) allows free-text Company Name (CCompany)
- JavaScript toggles CCompany field visibility

### Address Management

Contacts can have separate addresses for:
1. **Default/Primary** (Address1/2, Suburb, State, PostCode, Country)
2. **Delivery** (DelAddress1/2, DelSuburb, DelState, DelPostCode, DelCountry)
3. **Invoice** (InvAddress1/2, InvSuburb, InvState, InvPostCode, InvCountry)

The `DeliveryAddress.asp` and `InvoiceAddress.asp` pages allow editing these specific address types.

---

## 6. Integration Points

| Module | Usage |
|---|---|
| **10-Quotes.md** | Contact selector on Quote form (filtered by Customer=True) |
| **11-Invoices.md** | Contact for billing and delivery |
| **12-PurchaseOrders.md** | Supplier contact selector |
| **15-JobOrders.md** | Customer contact reference |
| **21-Companies.md** | Contacts linked to Companies |

---

## 7. Special Selectors

### Email Selection (`SelectEmailFromContact.asp`)
Modal window for extracting email addresses from contacts for use in email composition forms.

### Fax Selection (`SelectFaxFromContact.asp`)
Similar modal for fax numbers.

---

## 8. Known Baseline Issues

1. **Email Regex Complexity**: The email validation regex is complex and may reject valid email addresses.

2. **Website Protocol Enforcement**: Requires `http://` prefix but doesn't validate the URL is reachable.

3. **No Duplicate Check**: Adding a contact doesn't check for existing contacts with same name/email.

4. **Address Synchronization**: Changes to Company address don't cascade to existing Contacts.

5. **Popup Window Dependencies**: `AddNewWin.asp` uses `window.opener` references that may break in modern browsers with strict cross-origin policies.

---

## 9. Related Modules

- **21-Companies.md** — Contacts belong to Companies
- **31-Divisions-Locations.md** — Contacts are scoped to Divisions
