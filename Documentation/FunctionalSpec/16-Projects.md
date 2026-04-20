# 16 — Projects

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Projects/`.

Simple lookup table for project categorization. Projects are used across Quotes, Job Orders, and Purchase Orders for grouping and reporting. The module provides basic CRUD operations under the Setup menu.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | List all projects for divisions the user manages. |
| `Add.asp` | Create new project form. |
| `Add_Proc.asp` | Insert into `Projects` table. |
| `Edit.asp` | Edit project form. |
| `Edit_Proc.asp` | Update `Projects` table. |
| `Del_Proc.asp` | Delete project (no cascade check visible). |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Projects/` | Project list |
| `…/Projects/Add.asp` | New project form |
| `…/Projects/Edit.asp?ProjectId=<n>` | Edit project |
| `…/Projects/Del_Proc.asp?ProjectId=<n>` | Delete project |

---

## 3. Access Control

All pages gate on standard `ssi_Security.inc` (login check only). No additional division/role checks within the module itself.

**List Filter**: Shows only projects for divisions where user has Manager access:
```
Projects.DivisionId In (Request.Cookies("DivisionIdsAccess")("Manager"))
```

**Implication**: Regular users (non-managers) see an empty list but can still access the pages if they manually navigate.

---

## 4. Data Model

### 4.1 `Projects` Table

| Column | Data Type | Notes |
|---|---|---|
| `ProjectId` | AutoNumber | PK |
| `DivisionId` | Long Integer | FK Divisions |
| `Project` | Text(50) | Project name |
| `Visible` | Boolean | Display in dropdowns |

### 4.2 Usage in Other Tables

Projects are referenced (as free text or FK depending on context) in:
- `Quotes.Project`
- `PurchaseOrders.Project`
- `JobOrders.Project`

---

## 5. UI Flow

### List Page (Default.asp)

**Breadcrumb**: Home / Setup / Projects

**Columns**:
- Project name
- Division
- Visible (Yes/No)
- Action (Edit | Delete)

**Add Link**: Top-right "Add Project"

### Add/Edit Form

| Field | Required | Notes |
|---|---|---|
| Division | **Yes** | Dropdown of Manager divisions |
| Project | **Yes** | 50 character text |
| Visible | No | Radio Yes/No (default Yes) |

---

## 6. Integration Points

| Module | Usage |
|---|---|
| **10-Quotes.md** | Project dropdown on Add/Edit Quote |
| **12-PurchaseOrders.md** | Project field on PO form |
| **15-JobOrders.md** | Project field pre-filled from Quote |
| **40-Reports.md** | Filter/group by Project |

---

## 7. Known Baseline Issues

1. **No Cascade Protection**: `Del_Proc.asp` doesn't check if the project is referenced by Quotes, Job Orders, or POs before deleting. Could leave orphaned references.

2. **Manager-Only Visibility**: The list only shows projects for Manager divisions, but the Add form allows selecting any Manager division. Regular users cannot see the list but could theoretically access Edit URLs directly.

3. **No Audit Trail**: No `ProjectAudit` table - changes are not logged.

4. **Case Sensitivity**: Project name uniqueness not enforced (could have "Project A" and "project a").

---

## 8. Related Modules

- **12-PurchaseOrders.md** — POs reference Projects
- **15-JobOrders.md** — Jobs inherit Project from Quotes
- **32-Setup-Admin.md** — Projects are under Setup menu
