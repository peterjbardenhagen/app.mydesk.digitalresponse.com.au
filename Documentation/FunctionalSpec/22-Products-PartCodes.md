# 22 — Products and Part Codes

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Products/` and `Clients/SalesEngineTL/PartCodes/`.

Product catalog and part code management. Products define the sellable items for Quotes and Invoices with pricing and categorization. Part Codes provide division-specific product identifiers used in Purchase Orders.

---

## 1. Products Module

### 1.1 Files

| File | Role |
|---|---|
| `Default.asp` | Division selector (intermediate step). |
| `Default2.asp` | Actual product list for selected division. |
| `IFrame.asp` | Product grid with pricing. |
| `Add.asp` | Create product form. |
| `Add_Proc.asp` | Insert handler. |
| `Edit.asp` | Edit product form. |
| `Edit_Proc.asp` | Update handler. |
| `Del_Proc.asp` | Delete product. |
| `Select.asp` | Modal product selector for Quote/Invoice line items. |
| `SelectProduct.asp` / `SelectProduct_Default.asp` | Alternative product selection interfaces. |

### 1.2 URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Products/` | Select division first |
| `…/Products/Default2.asp?DivisionId=<n>` | Product list for division |
| `…/Products/Add.asp` | New product |
| `…/Products/Edit.asp?ProductId=<n>` | Edit product |
| `…/Products/Select.asp` | Modal selector for forms |

### 1.3 Access Control

Products access gated on Quotes permission:
```
Request.Cookies("DivisionIdsAccess")("Quotes") <> "0"
```

### 1.4 Data Model

#### `Products` Table

| Column | Data Type | Notes |
|---|---|---|
| `ProductId` | AutoNumber | PK |
| `DivisionId` | Long Integer | FK Divisions |
| `ProductCode` | Text(50) | SKU/Code |
| `ProductDescription` | Text(255) | |
| `ProductTypeId` | Long Integer | FK ProductTypes |
| `UnitCost` | Currency | Cost price |
| `NettPrice` | Currency | Sell price |
| `GST` | Boolean | GST applicable |
| `Visible` | Boolean | Show in selectors |
| `InOrder` | Integer | Sort order |

#### `ProductTypes` Lookup

| Column | Notes |
|---|---|
| `ProductTypeId` | PK |
| `ProductType` | Category name |
| `DivisionId` | Scoped to division |
| `InOrder` | Sort order |

### 1.5 UI Flow

**Division Selection**: Products are division-scoped. User must select a division before viewing products.

**List Columns**:
- Product Code
- Description
- Type
- Unit Cost
- Nett Price
- GST
- Action (Edit | Delete)

**Product Selector** (`Select.asp`):
- Modal window for Quote/Invoice line item entry
- Search/filter by code/description
- Returns ProductId, Code, Description, Price to parent form

---

## 2. Part Codes Module

### 2.1 Files

| File | Role |
|---|---|
| `Default.asp` | Modern list of all part codes across divisions. |
| `Add.asp` | Create part code form. |
| `Add_Proc.asp` | Insert handler. |
| `Edit.asp` | Edit part code form. |
| `Edit_Proc.asp` | Update handler. |
| `Del_Proc.asp` | Delete part code. |

### 2.2 URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/PartCodes/` | List all part codes |
| `…/PartCodes/Add.asp` | New part code |
| `…/PartCodes/Edit.asp?PartCodeId=<n>` | Edit |
| `…/PartCodes/Del_Proc.asp?PartCodeId=<n>` | Delete |

### 2.3 Access Control

Standard login check via `ssi_Security.inc`. Located under Setup menu (implied Manager access).

### 2.4 Data Model

#### `PartCodes` Table

| Column | Data Type | Notes |
|---|---|---|
| `PartCodeId` | AutoNumber | PK |
| `DivisionId` | Long Integer | FK Divisions |
| `PartCode` | Text(50) | Code identifier |
| `Description` | Text(255) | |
| `Visible` | Boolean | Show in PO dropdowns |

### 2.5 UI Flow

**List Page**: Shows all part codes with Division column (cross-division view).

**Form Fields**:
- Part Code (required)
- Description
- Division (dropdown of Manager divisions)
- Visible (Yes/No)

---

## 3. Key Differences: Products vs Part Codes

| Aspect | Products | Part Codes |
|---|---|---|
| **Purpose** | Sellable items (Quotes/Invoices) | Procurement items (POs) |
| **Pricing** | Has UnitCost and NettPrice | Code only, no pricing |
| **Division Scope** | User selects division to view | List shows all divisions |
| **Menu Location** | Main menu (with Quotes) | Setup menu |
| **Usage** | QuoteContents, InvoiceContents | PurchaseOrderContents |
| **Selector UI** | Modal Select.asp | Inline dropdown |

---

## 4. Integration Points

### Products

| Module | Usage |
|---|---|
| **10-Quotes.md** | Quote line items reference Products |
| **11-Invoices.md** | Invoice line items reference Products |
| **15-JobOrders.md** | Job line items reference Products |

### Part Codes

| Module | Usage |
|---|---|
| **12-PurchaseOrders.md** | PO line items use Part Codes |

---

## 5. Known Baseline Issues

### Products
1. **No Inventory Tracking**: Products table has no quantity/stock tracking.
2. **No Price History**: No record of price changes over time.
3. **Division-Only View**: Cannot view products across divisions simultaneously.
4. **No Barcode Field**: No dedicated barcode/SKU field separate from ProductCode.

### Part Codes
1. **No Link to Products**: Part Codes and Products are separate tables with no relationship.
2. **No Pricing**: Part codes don't store cost information (entered manually on POs).
3. **No Usage Count**: No tracking of how often a part code is used.

### Both
1. **No Import/Export**: No bulk import/export functionality.
2. **No Audit Trail**: No record of who created/modified items.

---

## 6. Related Modules

- **12-PurchaseOrders.md** — Uses Part Codes
- **10-Quotes.md** — Uses Products
- **32-Setup-Admin.md** — Part Codes under Setup
