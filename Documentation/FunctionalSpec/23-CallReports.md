# 23 — Call Reports

Status: **PLANNED BUT NOT IMPLEMENTED** — The Call Reports module is referenced in the navigation (`Portal.asp`, `PortalFrame.asp`) but the folder and implementation do not exist.

---

## 1. Implementation Status

| Component | Status |
|---|---|
| Database Schema | Unknown |
| UI Folder | **Missing** — `/Clients/SalesEngineTL/CallReports/` does not exist |
| Navigation Links | **Present** — Referenced in Portal.asp |
| Functionality | **Not Available** |

---

## 2. References Found

The following references indicate planned but unimplemented functionality:

```asp
' From Portal.asp and PortalFrame.asp - navigation entries
CallReports access check: Request.Cookies("DivisionIdsAccess")("CallReports")
```

---

## 3. Expected Functionality (Inferred)

Based on similar CRM systems and the naming convention, Call Reports would likely include:

| Feature | Description |
|---|---|
| Call Logging | Record sales/support calls with customers |
| Follow-up Tracking | Schedule and track follow-up calls |
| Call Outcomes | Log results (sale made, callback required, etc.) |
| Duration Tracking | Call time logging |
| Contact Association | Link calls to Contacts |
| Reporting | Calls by user, outcome, date range |
| Reminders | Alert for scheduled callbacks |

---

## 4. Database Tables (Expected)

If implemented, the module would likely use tables such as:

```
CallReports
- CallReportId (PK)
- ContactId (FK)
- Code (User)
- CallDate
- Duration
- Outcome
- Notes
- FollowUpRequired
- FollowUpDate
- CreatedDate
```

---

## 5. Navigation Location

The Call Reports link would appear in the main portal navigation alongside other modules, accessible to users with the appropriate division access cookie:
```
DivisionIdsAccess("CallReports") <> "0"
```

---

## 6. Recommendation

To complete this module, the following would need to be developed:

1. **Database Schema**: Create `CallReports` table with appropriate foreign keys
2. **UI Pages**:
   - `CallReports/Default.asp` — List/filter calls
   - `CallReports/Add.asp` — Log new call
   - `CallReports/Edit.asp` — Update call record
   - `CallReports/View.asp` — View call details
   - `CallReports/Report.asp` — Aggregate reporting
3. **Integration Points**:
   - Link from Contacts to view call history
   - Dashboard alerts for scheduled follow-ups
   - Email reminders for callbacks

---

## 7. Related Modules

- **20-Contacts.md** — Call Reports would reference Contacts
- **05-Dashboard.md** — Would feed dashboard reminders
