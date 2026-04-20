# 02 — Authentication, Portal & Session

Status: **IN REVIEW** — verified against source.

Covers every page in `Clients/SalesEngineTL/Portal/` plus the root sign-in entry points and the session/cookie schema that every other page relies on.

---

## 1. Page inventory

| File | Purpose |
|---|---|
| `/Default.asp` | **Primary sign-in page** (root). Shows the Techlight login card. If already logged in, redirects to `TL_WORKING_DIR/Dashboard.asp`. |
| `/Default2.asp` | Legacy alternate login page (kept for backwards compatibility). |
| `/Clients/SalesEngineTL/DefaultFrame.asp` | Self-contained sign-in page used when the login has to render inside an existing frame. Posts to `Portal/Validate.asp`. |
| `/Clients/SalesEngineTL/Portal/Validate.asp` | **Login POST handler**. Authenticates, loads permissions, populates Session + Cookies, redirects to `/SetCookies.asp`. |
| `/SetCookies.asp` | Post-login redirector — currently a thin wrapper that opens/closes DB and issues `Response.Redirect("/Portal.asp")`. |
| `/Clients/SalesEngineTL/Portal/LogOff.asp` | Logout. `Session.Abandon`, expires every cookie, redirects to `/?Msg=…`. |
| `/Clients/SalesEngineTL/Portal/ChangePassword.asp` | Password change form (self-service; also used when server enforces a rotation). |
| `/Clients/SalesEngineTL/Portal/ChangePassword_Proc.asp` | Verifies current password, writes new `PW` + `DatePasswordChanged` in `Users`, forces re-login. |
| `/Clients/SalesEngineTL/Portal/AccessDenied.asp` | Branded permission-denied screen with "Go Back" / "Return Home". |
| `/Clients/SalesEngineTL/Portal/Error.asp` | Generic error fallback. |
| `/ForgotPassword_Proc.asp` | "Forgot Password" modal handler — emails a secure login link. |
| `/AutoLogin.asp` | Accepts a token from that emailed link and logs the user in automatically. |

---

## 2. The sign-in page — `/Default.asp`

### URL & parameters

- **URL**: `https://techlight.digitalresponse.com.au/` (or `/Default.asp`).
- **Query params**:
  - `Msg` (optional) — message shown in the red error panel above the form (e.g. "Login failed", "Password changed. Please login again.", "You have logged off successfully.").

### Behaviour

On load:
1. Includes `Constants.asp` + `ssi_Functions.asp` (no DB connection needed).
2. Sets legacy `Session("Stylesheet")` and `Session("HomeColor1")` defaults if blank.
3. Checks `Session("LoggedIn")` and, as a fallback, the `LoggedIn` cookie. If either is truthy, re-hydrates the Session from the `UserSettings` cookie values and redirects to `TL_WORKING_DIR & "/Dashboard.asp"`.
4. Otherwise renders the login card.

### UI structure

- Dark radial-gradient background (`#1a1f2e → #242b3d`) with two animated floating circles.
- Centred white card (`max-width: 440px`, 48px padding) with a 4px gradient top accent (`--tl-primary → --tl-accent`).
- Logo (custom teal SVG), title "Techlight MyDesk", subtitle "Project Lighting Specialists".
- Welcome text: "Welcome Back / Sign in to access your dashboard".
- Error panel (conditionally shown) using `.error-message`.
- Form (`/Clients/SalesEngineTL/Portal/Validate.asp`, POST):
  - `Username` (required, autocomplete="username", autofocus)
  - `Password` (required, autocomplete="current-password")
  - `RememberMe` checkbox
- Submit button: "Sign In" (primary gradient).
- Footer links: "Forgot Password?" (opens `forgotModal`), "Support" (opens `supportModal`), "Secure Connection" badge, copyright.

### Modals

1. **Forgot Password modal** (`#forgotModal`):
   - Input: `ResetEmail` (email, required)
   - Posts to `/ForgotPassword_Proc.asp`
   - Server emails a secure-link back to the user.
2. **Support modal** (`#supportModal`):
   - Static info: `tel:0452491013`, `mailto:info@digitalresponse.com.au`.

### "Remember me" behaviour

- Client-side JavaScript writes/clears `RememberMeUsername` and `RememberMePassword` cookies on submit, **30-day expiry**, `path=/; SameSite=Strict`.
- On page load, these cookies are read and pre-fill the form and tick the checkbox.

> **Security note** (baseline observation, not a recommendation): the remember-me password is stored URL-encoded in a non-HttpOnly client cookie. This is the as-is behaviour.

### Validation rules

- Client-side: both fields must be non-empty (otherwise `alert('Please enter both username and password')` and submission is blocked).
- Server-side: see `Validate.asp` below.

---

## 3. Login handler — `Portal/Validate.asp`

### URL & form parameters

- **URL**: `/Clients/SalesEngineTL/Portal/Validate.asp`
- **Method**: POST (but also accepts GET via `Request(...)` which reads both)
- **Form fields**:
  - `Username` (maps to `Users.Name`)
  - `Password` (maps to `Users.PW`)

### Server-side flow

1. Includes `Var.asp` (sets `strWorkingDir`, `strGlobalPrefix`, `strGlobalState` and `ApprovalPassword` cookie), `ssi_dbConn_open.inc`, `ssi_Functions.asp`.
2. Hardens `Session("WorkingDir")` — if blank, force-sets to `/Clients/SalesEngineTL`.
3. Ensures `ClientSettings` cookie contains `WorkingDir`, `Prefix=TL`, `State=AUS` (expires 1000 days).
4. Escapes `'` in username/password (`Replace(..., "'", "''")`).
5. Runs this primary authentication query (abbreviated):
   ```sql
   SELECT Users.*, LineManagers.Name AS LineManagerName, LineManagers.Email AS LineManagerEmail,
          Divisions.Division, Locations.ExpenseTypeGroupId
   FROM Locations
     INNER JOIN (Divisions
       INNER JOIN (Users
         LEFT JOIN Users AS LineManagers ON Users.LineManagerCode = LineManagers.Code
       ) ON Divisions.DivisionId = Users.DivisionId
     ) ON Locations.LocationId = Users.LocationId
   WHERE Users.[Name] = '<username>' AND Users.[PW] = '<password>' AND Users.Active = -1
   ```
   (executed via `SafeExecute` — returns `Nothing` on error instead of raising).
6. **On match** — populates the Session (see §5 Session schema).
7. Runs a second query against `UsersAccess` to build six comma-separated division-ID strings:
   - `DivisionIdsVisible`, `DivisionIdsManager`, `DivisionIdsQuotes`, `DivisionIdsRFQ`, `DivisionIdsPurchaseOrders`, `DivisionIdsPayroll`.
   Each row checks the boolean flags `Visible`, `Manager`, `Quotes`, `RFQ`, `PurchaseOrders`, `Payroll` and appends the `DivisionId` if true.
8. **Admin name overrides** (hard-coded): if `Name = "Bert Beijnon"` OR `"Peter Bardenhagen"` → forces `UserTypeId = 1`, `Admin = True`, `Manager = True`.
9. **UserTypeId overrides**: if `UserTypeId = 1` OR `>= 5`, grants all boolean flags AND overwrites the six `DivisionIds*` strings with every `DivisionId` in the `Divisions` table (i.e. system-wide access).
10. Normalises the strings (strips trailing `", "`, defaults empty to `"0"`), then stores them into Session as `DivisionIds*` and `ArrDivisionIdsManager`, `ArrDivisionIdsPayroll` duplicates.
11. Redirects to `/SetCookies.asp?WorkingDir=<TL_WORKING_DIR>` → which in turn redirects to `/Portal.asp` (historic entry) → modern code pushes users into `Dashboard.asp` instead.
12. **On no-match** — clears Session, redirects to `/?Msg=Login+failed,+incorrect+Username+and/or+Password.+Please+try+again.`

### UserTypeId legend (inferred from code)

| `Users.UserTypeId` | Meaning |
|---|---|
| `1` | Director — full access, all divisions, admin UI enabled |
| `2` | Manager |
| `3` | Standard User |
| `4` | (unused/custom) |
| `5+` | Senior / system-level — promoted to full access at login |

`IsDirector()` (in `ssi_Header_Techlight.inc`) returns `true` only for `UserTypeId = 1`.

### Password rotation

`Users.DatePasswordChanged` drives a forced rotation. The flow:
- If `intDaysSinceLastPasswordChange > N` (threshold set in legacy code; the variable is declared in `Validate.asp` but the rotation trigger is performed in `ChangePassword.asp` via the `Days` query param).
- `ChangePassword.asp?Days=<n>` renders with the message "It has been **N days** since you last changed your password. Before you can login again, you must change your password."
- `ChangePassword_Proc.asp` verifies the current password, then runs:
  ```sql
  UPDATE Users SET DatePasswordChanged = '<ServerToEST(Now)>', [PW] = '<new>'
  WHERE [Name] = '<session name>' AND [PW] = '<current>'
  ```
  and redirects to `Session("WorkingDir") & "/?Msg=Password+changed..."`.

### Validation rules

- **Required**: Username + Password (front-end only; back-end accepts blank but query won't match).
- **Password confirmation** (on change): `NewPassword == NewPasswordC` (client-side `alert()`).
- **Current-password check** (on change): server-side `SELECT * FROM Users WHERE Name = <session> AND PW = <entered>`.
- **Max length**: `maxlength=20` on all password fields.
- **Inactive users**: `Users.Active = -1` is required (classic VB `True`). Users with `Active = 0` cannot log in even with correct credentials.

---

## 4. Logout — `Portal/LogOff.asp`

### URL & parameters

- **URL**: `/Clients/SalesEngineTL/Portal/LogOff.asp`
- **Query params**: `Msg` (optional) — passed through to the next page.

### Behaviour

1. Calls `Session.Abandon`.
2. Iterates `Request.Cookies` and expires every cookie (`Expires = Date() - 1`, value = `""`).
3. Redirects via `MyRedirect(…)` (a helper that uses meta-refresh/JS to break out of frames):
   - With `Msg` empty → `"/?Msg=You+have+logged+off+successfully"`
   - With `Msg` → `"/Default.asp?Msg=" & Request("Msg")`

### Security-include related

- `/Clients/SalesEngineTL/ssi_Security.inc` (the legacy stub) redirects failed sessions to this `LogOff.asp` with `?Msg=Request.Cookies+Expired`.
- `/System/ssi_Security.inc` (modern) redirects failed sessions to `/Default.asp?Msg=Request.Cookies+Expired` instead.

---

## 5. Session & cookie schema

The application stores identity/permissions in **both** ASP Session and persistent cookies. Session is authoritative on each request, cookies are the fallback when Session has expired but the browser still has a valid `LoggedIn` cookie (see `/Default.asp` lines 47–77 for the rehydration logic).

### 5.1 Session variables (set by `Portal/Validate.asp`)

| Variable | Type | Source | Purpose |
|---|---|---|---|
| `LoggedIn` | Boolean | literal `True` on success | Authoritative login flag |
| `Code` | String | `Users.Code` (e.g. "MD0140") | **Primary user identifier** — used in every ownership filter, audit trail, approval chain |
| `Name` | String | `Users.Name` | Display name, also used to look up `LineManagerCode` |
| `Email` | String | `Users.Email` | Default "From" on outgoing emails |
| `Initials` | String | `Users.Initials` | Avatar initials |
| `DivisionId` | Long | `Users.DivisionId` | Primary division (governs quote/PO division default) |
| `Division` | String | `Divisions.Division` | Display |
| `LocationId` | Long | `Users.LocationId` | Office/branch |
| `ExpenseTypeGroupId` | Long | `Locations.ExpenseTypeGroupId` | Drives the visible expense categories |
| `UserTypeId` | Long | `Users.UserTypeId` | Role tier (see legend above) |
| `LineManagerCode` | String | `Users.LineManagerCode` | Escalation target for approvals |
| `LineManagerName` / `LineManagerEmail` | String | Joined from `Users AS LineManagers` | For email routing |
| `HoursPerDay`, `HoursPerWeek` | Number | `Users.HoursPerDay`, `… × DaysPerWeek` | Timesheet calculations; defaults 8 / 40 |
| `Admin` | Boolean | Hard-coded for Bert/Peter; otherwise derived from `UserTypeId`/`Manager` | Shows admin UI |
| `Manager` | Boolean | true if any `UsersAccess.Manager` row is true, OR promoted by UserTypeId override | Enables Manager-only screens |
| `Quotes` | Boolean | true if any `UsersAccess.Quotes` row is true (or promoted) | Quotes module visibility |
| `RFQ` | Boolean | ditto for `UsersAccess.RFQ` | RFQ module visibility |
| `PurchaseOrders` | Boolean | ditto for `UsersAccess.PurchaseOrders` | PO module visibility |
| `Payroll` | Boolean | ditto for `UsersAccess.Payroll` | Payroll / Timesheets approver |
| `DivisionIdsVisible` | String (CSV) | list of `DivisionId`s visible to this user | `WHERE DivisionId IN (…)` filter on list pages |
| `DivisionIdsManager` | String (CSV) | divisions where user is Manager | Manager screens / approvals |
| `DivisionIdsQuotes` | String (CSV) | divisions where user can access Quotes | Quotes filters |
| `DivisionIdsRFQ` | String (CSV) | … | RFQ filters |
| `DivisionIdsPurchaseOrders` | String (CSV) | … | PO filters |
| `DivisionIdsPayroll` | String (CSV) | … | Payroll filters |
| `ArrDivisionIdsManager`, `ArrDivisionIdsPayroll` | String (CSV) | alias copies | Backward compat with older code that expects these names |
| `WorkingDir` | String | `"/Clients/SalesEngineTL"` | Application root (hard-enforced) |
| `Stylesheet` | String | defaults to `TL_STYLESHEET` | Legacy stylesheet path |
| `HomeColor1` | String | defaults to `TL_COLOR_HOME` | Legacy theme accent |

### 5.2 Cookies (set at login + persisted)

| Cookie | Sub-keys | Lifetime | Purpose |
|---|---|---|---|
| `LoggedIn` | (scalar `True`/`False`) | Session/persistent | Read by every `ssi_Security.inc` |
| `UserSettings` | `Name`, `Code`, `Email`, `Initials`, `DivisionId`, `Division`, `UserTypeId`, `LocationId`, `ExpenseTypeGroupId`, `Admin`, `Manager` | Session (per login) | Cross-frame identity. Header/PortalFrame read these rather than Session to survive frame-boundary quirks. |
| `ClientSettings` | `WorkingDir`, `Prefix`, `State`, `Stylesheet`, `HomeColor1` | `Date() + 1000` days | App root + theming (historic multi-tenant hangover) |
| `DivisionIdsAccess` | `Quotes`, `RFQ`, `PurchaseOrders`, `Payroll`, `ArrDivisionIds`, `ArrDivisionIdsManager` | Session | Division-level permissions in cookie form |
| `ApprovalPassword` | (scalar = `TL_APPROVAL_PASSWORD`) | `Date() + 365` | Shared approval secret, checked inline on PO/quote approval pages |
| `RememberMeUsername`, `RememberMePassword` | (scalars, URL-encoded) | 30 days | Client-side remember-me pre-fill |

### 5.3 Identity rehydration (`/Default.asp`)

If `Session("LoggedIn")` is missing/falsy but the `LoggedIn` cookie is `"True"`, `/Default.asp` rebuilds the Session from `UserSettings` cookie keys:

```
Session("Code")           ← Cookies("UserSettings")("Code")
Session("Name")           ← Cookies("UserSettings")("Name")
Session("Email")          ← Cookies("UserSettings")("Email")
Session("Initials")       ← Cookies("UserSettings")("Initials")
Session("DivisionId")     ← Cookies("UserSettings")("DivisionId")
Session("Division")       ← Cookies("UserSettings")("Division")
Session("UserTypeId")     ← Cookies("UserSettings")("UserTypeId")
Session("LocationId")     ← Cookies("UserSettings")("LocationId")
Session("ExpenseTypeGroupId") ← Cookies("UserSettings")("ExpenseTypeGroupId")
```

Then redirects to `TL_WORKING_DIR & "/Dashboard.asp"`.

---

## 6. Access denied — `Portal/AccessDenied.asp`

### URL & parameters

- **URL**: `/Clients/SalesEngineTL/Portal/AccessDenied.asp`
- **Query params**: none — called by module pages as a hard redirect when the user lacks rights.

### UI

- Injects `Clients/SalesEngineTL/Header.asp` (full navigation rendered for context).
- Centred white card, red left/top border, red circle-with-X icon, `h1="Access Denied"`, explanatory paragraph, two buttons:
  - **Go Back** (`history.back()`) — secondary.
  - **Return Home** (`/Portal.asp`) — primary.

### Usage

Module screens redirect here when:
- User has `LoggedIn = true` but is missing a cookie/session flag.
- Example: `If Request.Cookies("DivisionIdsAccess")("Quotes") = "0" Then Response.Redirect(strWorkingDir & "/Portal/AccessDenied.asp")`.

---

## 7. Forgot-password flow

```
Login page ──► "Forgot Password?" modal (client-side) ──► POST /ForgotPassword_Proc.asp
                                                                 │
                                                                 ▼
                                                         Lookup Users by Email
                                                                 │
                                                                 ▼
                                                         Generate one-time token
                                                                 │
                                                                 ▼
                                                         Email secure link to user
                                                                 │
                                                                 ▼
                                            User clicks link ──► /AutoLogin.asp?Token=…
                                                                 │
                                                                 ▼
                                            Token verified → Session/Cookies populated
                                            → Redirect to Dashboard
```

Details of the token format and email template live in `ForgotPassword_Proc.asp` and `AutoLogin.asp` — see the source for string templates.

---

## 8. Post-login landing

After successful `Validate.asp`:
1. Redirect → `/SetCookies.asp?WorkingDir=/Clients/SalesEngineTL`.
2. `SetCookies.asp` opens/closes DB (side-effect: warms connection) and redirects → `/Portal.asp` (legacy), which most of the time then redirects → `/Clients/SalesEngineTL/Dashboard.asp` via the modern `Default.asp` rehydration path.

In practice, the first screen the user sees is **Dashboard.asp** (see `05-Dashboard.md`).

---

## 9. Permission gates in module pages

The typical gate at the top of a secured module page is:

```asp
<!--#include virtual="/System/ssi_Security.inc"-->   ' Hard redirect if not logged in
<%
' Module-level gate
If Not CBool(Session("Quotes")) And Request.Cookies("DivisionIdsAccess")("Quotes") = "0" Then
    Response.Redirect(TL_WORKING_DIR & "/Portal/AccessDenied.asp")
    Response.End
End If

' Row-level / division-level gate
If InStr("," & Session("DivisionIdsVisible") & ",", "," & CStr(rs("DivisionId")) & ",") = 0 Then
    Response.Redirect(TL_WORKING_DIR & "/Portal/AccessDenied.asp")
End If
%>
```

Individual modules describe their specific gates in their own spec file.
