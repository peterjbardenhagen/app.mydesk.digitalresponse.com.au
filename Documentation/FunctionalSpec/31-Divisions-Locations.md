# 31 — Divisions and Locations

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Divisions/` and `Clients/SalesEngineTL/Locations/`.

Division and Location management provide the organizational structure for the system. Divisions represent business units or brands, while Locations represent physical addresses (offices, depots, warehouses).

---

## 1. Divisions Module

### 1.1 Files

| File | Role |
|---|---|
| `Default.asp` | Modern division list. Director-only access. |
| `Add.asp` | Create division form. |
| `Add_Proc.asp` | Insert handler. |
| `Edit.asp` | Edit division form. |
| `Edit_Proc.asp` | Update handler. |
| `Del_Proc.asp` | Delete division. |

### 1.2 URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Divisions/` | Division list |
| `…/Divisions/Add.asp` | New division |
| `…/Divisions/Edit.asp?DivisionId=<n>` | Edit division |
| `…/Divisions/Del_Proc.asp?DivisionId=<n>` | Delete |

### 1.3 Access Control

**Director-Only Gate** (`Default.asp:13`):
```asp
If Not Request.Cookies("UserSettings")("UserTypeId") = 6 Then
    Response.Redirect("../Portal/AccessDenied.asp")
End If
```

Only UserTypeId = 6 (Director) can manage divisions.

### 1.4 Data Model

#### `Divisions` Table

| Column | Data Type | Notes |
|---|---|---|
| `DivisionId` | AutoNumber | PK |
| `DivisionCode` | Text(10) | Short code (e.g., "TL", "SE") |
| `Division` | Text(50) | Full name |
| `Logo` | Text(50) | Logo filename for documents |
| `ABN` | Text(20) | Australian Business Number |
| `ACN` | Text(20) | Australian Company Number |
| `Address1`, `Address2`, `Suburb`, `StateId`, `State`, `PostCode`, `Country` | | |
| `Quotes` | Boolean | Has Quotes module enabled |
| `Invoices` | Boolean | Has Invoices module enabled |
| `PurchaseOrders` | Boolean | Has PO module enabled |
| `PurchaseRequests` | Boolean | Uses Purchase Request workflow instead of direct PO |
| `JobOrders` | Boolean | Has Job Orders module enabled |
| `Visible` | Boolean | Show in dropdowns |
| `InOrder` | Integer | Sort order |

### 1.5 UI Flow

**List Page** (Default.asp):
- Columns: Division Code, Division Name, Actions
- Badges for Division Code
- Edit | Delete actions

**Form Fields**:
- Division Code (required, 10 char)
- Division Name (required, 50 char)
- Logo filename
- ABN/ACN
- Address fields
- Module toggles (Quotes, Invoices, PurchaseOrders, etc.)
- Purchase Requests flag (alternative PO workflow)
- Visible flag

---

## 2. Locations Module

### 2.1 Files

| File | Role |
|---|---|
| `Default.asp` | Modern location list. Setup menu access. |
| `Add.asp` | Create location form. |
| `Add_Proc.asp` | Insert handler. |
| `Edit.asp` | Edit location form. |
| `Edit_Proc.asp` | Update handler. |
| `Del_Proc.asp` | Delete location. |

### 2.2 URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Locations/` | Location list |
| `…/Locations/Add.asp` | New location |
| `…/Locations/Edit.asp?LocationId=<n>` | Edit location |
| `…/Locations/Del_Proc.asp?LocationId=<n>` | Delete |

### 2.3 Access Control

Standard `ssi_Security.inc` login check. Located under Setup menu (implies Manager+ access).

### 2.4 Data Model

#### `Locations` Table

| Column | Data Type | Notes |
|---|---|---|
| `LocationId` | AutoNumber | PK |
| `Company` | Text(100) | Location name (often "Techlight [City]") |
| `Address1`, `Address2`, `Suburb`, `StateId`, `State`, `PostCode`, `Country` | | |
| `Phone`, `Fax` | Text(50) | |
| `Email` | Text(100) | |
| `PODisplay` | Boolean | Show separate PO address on documents |
| `POAddress1`, `POAddress2`, `POSuburb`, `POStateId`, `POState`, `POPostCode`, `POCountry` | | Separate PO box/address |

### 2.5 UI Flow

**List Page** (Default.asp):
- Breadcrumb: Home / Setup / Locations
- Columns: Company (name), Suburb, State
- Badges for State

**Form Fields**:
- Company/Location Name (required)
- Address fields
- Phone, Fax, Email
- **PO Display** toggle
- **PO Address** fields (shown when PODisplay = true)

---

## 3. Key Differences

| Aspect | Divisions | Locations |
|---|---|---|
| **Purpose** | Business unit / Brand | Physical address / Depot |
| **Access Level** | Director only | Manager+ (Setup menu) |
| **Users** | Users belong to one primary | Users assigned to one |
| **Documents** | Logo, ABN for headers | Return address on POs |
| **Module Toggles** | Yes (Quotes, Invoices, etc.) | No |
| **Special Features** | PurchaseRequests flag | Separate PO address |

---

## 4. Integration Points

### Divisions

| Module | Usage |
|---|---|
| **All modules** | `DivisionIdsAccess` cookie filters all data |
| **10-Quotes.md** | Quote header division |
| **30-Users-Roles.md** | User's primary division |
| **20-Contacts.md** | Contact division scope |
| **21-Companies.md** | Company division |

### Locations

| Module | Usage |
|---|---|
| **30-Users-Roles.md** | User's location |
| **12-PurchaseOrders.md** | PO delivery location selector |
| **11-Invoices.md** | Invoice origin location |
| **View.asp** documents | Location address on printed documents |

---

## 5. Address Display Logic

### Division Address
Used on Quote/Invoice headers as the "from" address.

### Location Address
Used for:
1. User's default location (where they work)
2. PO delivery address options
3. Document return addresses

### PO Address (Locations)
When `PODisplay = True`, documents show separate PO Box address for remittance/payment.

---

## 6. Known Baseline Issues

### Divisions
1. **No Archive Flag**: Divisions cannot be archived, only deleted (prevents historical reporting).
2. **Logo Path Hardcoded**: Logo filename only, path hardcoded in templates (`/images/[Logo]`).
3. **No Division Code Uniqueness**: No validation prevents duplicate DivisionCode values.

### Locations
1. **No Link to Divisions**: Locations exist independently of divisions (cross-division sharing).
2. **Company Name Confusion**: Field named "Company" but represents location name.
3. **No Capacity/Size Fields**: No storage for depot size, capacity, etc.

### Both
1. **State Denormalization**: State name stored alongside StateId (risk of sync issues).
2. **No Audit Trail**: No record of who created/modified divisions/locations.

---

## 7. Related Modules

- **30-Users-Roles.md** — Users reference both Division and Location
- **32-Setup-Admin.md** — Both under Setup menu
