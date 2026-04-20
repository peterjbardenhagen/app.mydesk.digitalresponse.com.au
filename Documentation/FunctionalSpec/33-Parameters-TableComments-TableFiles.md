# 33 — Parameters, Table Comments, and Table Files

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Parameters/` and `Clients/SalesEngineTL/TableComments/`.

System-wide configuration parameters and record-level commenting system. Parameters control global behavior, while TableComments provides per-record notes and follow-up tracking across all modules.

---

## 1. Parameters Module

### 1.1 Files

| File | Role |
|---|---|
| `Default.asp` | Parameter editing form (Manager access). |
| `Edit_Proc.asp` | Update handler for Parameters table. |

### 1.2 URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Parameters/` | Parameter editing form |
| `…/Parameters/Edit_Proc.asp` | Save changes |

### 1.3 Access Control

**Manager Gate** (`Default.asp:75`):
```asp
If Request.Cookies("UserSettings")("Manager") Then
    ' Show form
End If
```

Only managers can view/edit parameters.

### 1.4 Data Model

#### `Parameters` Table

| Column | Data Type | Notes |
|---|---|---|
| `ParameterId` | AutoNumber | PK |
| `UploadFrom` | DateTime | Legacy field - data upload cutoff |
| `MinimumValue` | Currency | Minimum quote/invoice value threshold |

**Note**: The Parameters table appears minimal. Additional system settings may be stored in:
- Cookie-based user preferences
- Division-level settings in `Divisions` table
- Hardcoded configuration in include files

### 1.5 UI Flow

**Form Fields**:
- **Upload From Date** — Calendar picker, required
- **Minimum Value ($)** — Currency input, required

**Layout**: Modern card-based form with cancel/save buttons.

---

## 2. Table Comments Module

### 2.1 Files

| File | Role |
|---|---|
| `Comments.asp` | Inline comment list for a record (iframe-friendly). |
| `Add.asp` | Add comment form with follow-up options. |
| `Add_Proc.asp` | Comment insert handler. |
| `IFrame.asp` | Data grid showing comments across records. |
| `Mark_FollowUpComplete_Proc.asp` | Toggle follow-up completion status. |
| `ViewRecord.asp` | Navigate to commented record. |

### 2.2 URL Map

| URL | Purpose |
|---|---|
| `…/TableComments/Comments.asp?TableId=<n>&ItemId=<n>` | List comments for record |
| `…/TableComments/Add.asp?TableId=<n>&ItemId=<n>` | Add comment form |
| `…/TableComments/IFrame.asp` | Grid view for integration |
| `…/TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=<n>` | Mark follow-up done |
| `…/TableComments/ViewRecord.asp?TableId=<n>&ItemId=<n>` | Go to source record |

### 2.3 Data Model

#### `Comments` Table

| Column | Data Type | Notes |
|---|---|---|
| `CommentId` | AutoNumber | PK |
| `TableId` | Integer | Module identifier (see table mapping below) |
| `ItemId` | Long Integer | Record ID within that table |
| `FromCode` | Text(50) | User who commented |
| `ToCode` | Text(50) | Target user (if directed) |
| `Comment` | Memo | Comment text (500 char) |
| `DateEntered` | DateTime | Timestamp |
| `FollowUpRequired` | Boolean | Requires future action |
| `FollowUpDate` | DateTime | When to follow up |
| `FollowUpComplete` | Boolean | Follow-up action done |

#### TableId Mapping

| TableId | Module/Table |
|---|---|
| 1 | Quotes |
| 2 | Invoices |
| 3 | JobOrders |
| 4 | Contacts |
| 5 | Companies |
| 6 | PurchaseOrders |
| 7 | Products |
| 8 | DeliveryNotes |
| 9 | CallReports |
| 10 | Projects |

### 2.4 UI Flow

**Comment List** (`Comments.asp`):
- Shows all comments for a TableId + ItemId combination
- Columns: Date, User, Comment, Follow-Up Date, Complete Status
- Follow-up required = No → shows "No" in red
- Links to Add Comment and Back

**Add Comment** (`Add.asp`):
- Comment textarea (500 char limit)
- Follow-up required (Yes/No toggle)
- Follow-up date (calendar, shown if Yes)
- Pre-filled TableId and ItemId

**Follow-Up Management**:
- Comments with `FollowUpRequired = True` appear in reports
- `Mark_FollowUpComplete_Proc.asp` toggles completion
- Incomplete follow-ups highlighted in red

### 2.5 Integration Points

| Module | Usage |
|---|---|
| **10-Quotes.md** | Quote comments (TableId=1) |
| **11-Invoices.md** | Invoice comments (TableId=2) |
| **12-PurchaseOrders.md** | PO comments (TableId=6) |
| **20-Contacts.md** | Contact notes (TableId=4) |
| **21-Companies.md** | Company notes (TableId=5) |
| **40-Reports.md** | Follow-up reports |

---

## 3. Table Files Module

### 3.1 Status: **PLANNED BUT NOT IMPLEMENTED**

The Table Files module is referenced in the codebase but no folder exists:

**References Found**:
- `Quotes/IFrame.asp` — references `TableFiles`
- `JobOrders/IFrame.asp` — references `TableFiles`
- `System/IFrame.asp` — references `TableFiles`
- `PortalFrame.asp` — navigation link

### 3.2 Expected Functionality

Table Files would provide file attachment capabilities for any record:

| Feature | Description |
|---|---|
| File Upload | Attach documents to quotes, invoices, etc. |
| File Types | PDF, Word, Excel, Images |
| Storage | File system or database BLOB |
| Security | Permission-based access |
| Thumbnails | Preview for images |

### 3.3 Expected Database Schema

```
TableFiles
- FileId (PK)
- TableId (FK to module)
- ItemId (FK to record)
- FileName
- FilePath
- FileSize
- ContentType
- UploadedBy
- UploadedDate
```

### 3.4 Related Files

The `TableComments` pattern would be followed:
- `TableFiles/Default.asp` — List files
- `TableFiles/Add.asp` — Upload form
- `TableFiles/Del_Proc.asp` — Delete file
- `TableFiles/Download.asp` — Serve file

---

## 4. Known Baseline Issues

### Parameters
1. **Minimal Implementation**: Only 2 parameters exposed, many settings likely hardcoded.
2. **No Parameter Types**: No support for boolean, multi-select, or JSON parameters.
3. **No Audit Trail**: Parameter changes not logged.

### Table Comments
1. **No Edit/Delete**: Comments cannot be modified after posting.
2. **Character Limit**: 500 chars may be insufficient for detailed notes.
3. **No Notifications**: No email alert when follow-up date arrives.
4. **No Threading**: Flat comment list, no reply structure.
5. **TableId Hardcoded**: Adding new modules requires updating all references.

### Table Files
1. **Completely Missing**: Folder and all files absent.
2. **References in Code**: May cause broken links if clicked.

---

## 5. Related Modules

- **32-Setup-Admin.md** — Parameters and TableComments accessed via Setup
- **All Transaction Modules** — Comment integration on Quotes, Invoices, POs, etc.
