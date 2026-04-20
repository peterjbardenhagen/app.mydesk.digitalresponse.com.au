# 30 — Users and Roles

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Users/`.

User management and role-based access control. The Users module provides CRUD for system users, while UserRoles define permission templates including PO approval limits and division access rights.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Modern user list with filtering by access level. Admin/Manager only. |
| `Add.asp` | Comprehensive user creation form (20+ fields). |
| `Add_Proc.asp` | Insert handler with division access setup. |
| `Edit.asp` | User edit form with full field access. |
| `Edit_Proc.asp` | Update handler. |
| `EditPassword.asp` | Password change form (self-service or admin). |
| `EditPassword_Proc.asp` | Password update processor. |
| `Del_Proc.asp` | Soft delete (sets Deleted flag). |
| `Hierachy.asp` | Visual org chart / line manager hierarchy viewer. |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Users/` | User list (Admin/Manager only) |
| `…/Users/Add.asp` | New user form |
| `…/Users/Edit.asp?UserId=<n>` | Edit user |
| `…/Users/EditPassword.asp?Code=<str>` | Change password |
| `…/Users/Del_Proc.asp?Code=<str>` | Delete user |
| `…/Users/Hierachy.asp` | View org chart |

---

## 3. Access Control

### Module Gate

**Default.asp** requires UserTypeId ≥ 4 (Admin level):
```asp
If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
    Response.Redirect("../Portal/AccessDenied.asp")
End If
```

**Add.asp** has same restriction.

### List View Scope

Users can only see users at or below their own UserTypeId level:
```asp
If Request.Cookies("UserSettings")("UserTypeId") <= 5 Then
    sql = sql & " AND U.UserTypeId <= " & Request.Cookies("UserSettings")("UserTypeId")
End If
```

This prevents lower-level admins from seeing Director-level users.

### Access Code List

`GetAccessCodesList(CurrentUserCode, CurrentUserTypeId)` returns comma-separated list of user codes that the current user can manage, used throughout the system for filtering.

---

## 4. Data Model

### 4.1 `Users` Table

| Column | Data Type | Notes |
|---|---|---|
| `UserId` | AutoNumber | PK |
| `Code` | Text(50) | Unique username/login |
| `PW` | Text(255) | Password (MD5 hashed) |
| `Name` | Text(100) | Full name |
| `Initials` | Text(10) | |
| `Email` | Text(100) | |
| `Position` | Text(50) | Job title |
| `UserTypeId` | Long Integer | FK UserTypes (1-6) |
| `UserRoleId` | Long Integer | FK UserRoles |
| `DivisionId` | Long Integer | Primary division |
| `LocationId` | Long Integer | FK Locations |
| `LineManagerCode` | Text(50) | FK Users (self-reference for hierarchy) |
| `Phone`, `Mobile`, `Fax` | Text(50) | |
| `Address1`, `Address2`, `Suburb`, `State`, `PostCode`, `Country` | | |
| `Active` | Boolean | Account enabled |
| `Deleted` | Boolean | Soft delete flag |
| `Manager` | Boolean | Can see subordinate data |
| `DaysPerWeek` | Integer | Capacity planning |
| `HoursPerDay` | Integer | |
| `ExpensesPerMonth` | Currency | |
| `Signature` | Memo | Email signature HTML |
| `DateEntered`, `EnteredBy` | | Audit |

### 4.2 `UserTypes` (Permission Levels)

| ID | Type | Description |
|---|---|---|
| 1 | Disabled | Cannot log in |
| 2 | Guest | Minimal read-only access |
| 3 | Standard User | Normal operational access |
| 4 | Power User | Extended access, can manage some setup |
| 5 | Administrator | Full system access except Directors |
| 6 | Director | Full access including all user management |

### 4.3 `UserRoles` (Role Templates)

| Column | Notes |
|---|---|
| `UserRoleId` | PK |
| `UserRole` | Name (e.g., "Sales Rep", "Warehouse Manager") |
| `POApprovalLimit` | Currency - max PO value this role can approve |
| `Quotes` | Boolean - access to Quotes module |
| `Invoices` | Boolean - access to Invoices module |
| `PurchaseOrders` | Boolean - access to PO module |
| `JobOrders` | Boolean - access to Job Orders |
| ... | Additional module flags |

### 4.4 `DivisionAccess` (Cross-Division Permissions)

Links users to divisions they can access beyond their primary:

| Column | Notes |
|---|---|
| `DivisionAccessId` | PK |
| `Code` | User code |
| `DivisionId` | FK Divisions |
| `Manager` | Boolean - manager rights in this division |

---

## 5. UI Flow

### User List (Default.asp)

**Columns**:
- Name (link to Edit)
- Code
- Division
- Position
- Active (Yes/No)
- Manager (Yes/No - via CheckIfAdmin)
- User Type
- Actions (Edit | Delete)

**Filter**: Only shows users the current user can manage (via GetAccessCodesList).

### Add/Edit User Form

**Required Fields**:
- User Role
- User Type
- Primary Division
- Location
- Line Manager
- Name
- Initials
- Email
- Password

**Field Sections**:
1. **Role & Access**: UserRoleId, UserTypeId, DivisionId, LocationId, LineManagerCode, Active, Manager
2. **Contact Info**: Name, Initials, Email, Phone, Mobile, Fax, Position
3. **Address**: Standard address fields
4. **Work Schedule**: DaysPerWeek, HoursPerDay, ExpensesPerMonth
5. **Email Signature**: HTML signature editor
6. **Division Access**: Multi-select for cross-division permissions
7. **Module Permissions**: Quotes, Invoices, PurchaseOrders, etc.

### Password Management

**EditPassword.asp** allows:
- Self-service password change (with current password verification)
- Admin password reset (bypass current password)

**Security**: Passwords stored as MD5 hash.

### Hierarchy Viewer (Hierachy.asp)

Displays organizational chart based on LineManagerCode relationships:
- Tree view of reporting structure
- Shows each user's direct reports
- Highlights approval chains for PO workflow

---

## 6. Integration Points

| Module | Usage |
|---|---|
| **02-Authentication-Portal.md** | User validation on login |
| **12-PurchaseOrders.md** | PO approval chain uses LineManagerCode |
| **All modules** | Division access via cookies |

---

## 7. Known Baseline Issues

1. **Password Hash**: Uses MD5 (considered weak by modern standards). Should migrate to bcrypt/scrypt.

2. **Soft Delete Only**: `Del_Proc.asp` sets `Deleted = 1` but doesn't cascade. Orphaned references may occur.

3. **Code Case Sensitivity**: User Code is case-sensitive in some comparisons but not others (inconsistent).

4. **Duplicate Check**: No unique constraint enforcement on Code field (relies on application logic).

5. **Hierarchy Loops**: No validation prevents a user from being their own line manager (infinite loop in approval chain).

6. **Expensive GetAccessCodesList**: Called frequently, may cause performance issues with large user bases.

---

## 8. Related Modules

- **31-Divisions-Locations.md** — Users belong to Divisions and Locations
- **32-Setup-Admin.md** — User management under Setup
