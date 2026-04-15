# MyDesk WIP April 2026 - Implementation Plans

## Summary of Work Items

1. **[URGENT] Quote Recalculation Bug** - Totals incorrect after line item edits
2. **Quote Ownership/Sender Dropdown** - Allow Bert/Isaac to send quotes on behalf of each other
3. **Search Quotes by Customer** - Add customer search capability
4. **Browser/Desktop Usability** - Shortcuts and Edge configuration
5. **[NEW] MYOB Integration** - Export invoices to MYOB for aged receivables

---

## 1. Quote Recalculation Bug (PRIORITY)

### Problem Statement
Quote totals are recalculated incorrectly after editing line items. Bert must manually verify every total before sending.

### Root Cause Analysis
The issue likely occurs in:
- `Quotes/Edit_Proc.asp` - Line item update logic
- `Quotes/Add2.asp` / `Add_Proc.asp` - Quote creation calculations
- JavaScript calculation in browser vs server-side mismatch

### Investigation Steps
1. Review `Edit_Proc.asp` line 45-120 where item totals are calculated
2. Check for rounding issues (currency calculations)
3. Verify GST calculation order (line total vs quote total)
4. Check if `QuoteItems` table update triggers total recalculation correctly

### Code Changes Required

**File: `/Clients/SalesEngineTL/Quotes/Edit_Proc.asp`**

Current pattern (problematic):
```vbscript
' Existing code may calculate:
'total = SUM(line_qty * line_price)
'Then apply GST - but line items may already include GST
```

Fix - Ensure consistent calculation:
```vbscript
' After updating QuoteItems, recalculate totals
sql = "SELECT SUM(Qty * NetPrice) AS SubTotal FROM QuoteItems WHERE Qid=" & lngQid
Set rs = dbConn.Execute(sql)
dblSubTotal = CDbl(rs("SubTotal"))
dblGST = dblSubTotal * 0.10  ' 10% GST
dblTotal = dblSubTotal + dblGST

' Update Quotes table with recalculated totals
sql = "UPDATE Quotes SET CostExGST=" & dblSubTotal & ", " & _
      "GST=" & dblGST & ", PriceExGST=" & dblTotal & " WHERE Qid=" & lngQid
dbConn.Execute(sql)
```

### Database Changes
**NONE** - Logic fix only, schema is correct.

### Testing Plan
1. Create test quote with 3 line items
2. Edit middle item - change quantity
3. Verify total = SUM(NetPrice * Qty) * 1.10
4. Check edge cases: 0 qty, decimal quantities, large amounts

---

## 2. Quote Ownership / Sender Name Dropdown

### Problem Statement
Quotes show as sent from the logged-in user (Isaac), but Bert is the actual sender. Need ability to select sender.

### Current State
- `Quotes` table has `Code` field (linked to `Users` table)
- Email templates pull sender from `Session("Name")` or `Users.Name`

### Option A: Simple Dropdown (Recommended)

#### Database Changes

**Table: `Quotes`**
```sql
ALTER TABLE Quotes ADD COLUMN SenderCode TEXT(10)
```

**Table: `Users`** (if not exists)
```sql
-- Add CanSendAsOthers flag for security
ALTER TABLE Users ADD COLUMN CanSendAsOthers YESNO DEFAULT No
```

#### Code Changes

**File: `Quotes/Edit.asp` - Add sender dropdown**

```vbscript
' Near line 120-150 where user/contact selection is
Set rsUsers = dbConn.Execute("SELECT Code, Name FROM Users WHERE Active=-1 ORDER BY Name")
%>
<tr>
    <td>Quote From:</td>
    <td>
        <select name="SenderCode" id="SenderCode">
            <option value="<%=rsQu("Code")%>"><%=rsQu("Name")%> (Current User)</option>
            <% If Request.Cookies("UserSettings")("Manager") OR _
               Request.Cookies("UserSettings")("Code") = "MD0290" Then ' Bert or Managers
                Do While Not rsUsers.EOF
                    If rsUsers("Code") <> rsQu("Code") Then %>
                        <option value="<%=rsUsers("Code")%>"><%=rsUsers("Name")%></option>
            <%      End If
                    rsUsers.MoveNext
                Loop
            End If %>
        </select>
    </td>
</tr>
```

**File: `Quotes/Edit_Proc.asp`**
```vbscript
' Capture sender code
strSenderCode = Request("SenderCode")
If strSenderCode = "" Then strSenderCode = Request.Cookies("UserSettings")("Code")

sql = "UPDATE Quotes SET SenderCode='" & strSenderCode & "' WHERE Qid=" & lngQid
dbConn.Execute(sql)
```

**File: `Quotes/View.asp` - Email template sender**
```vbscript
' Line ~200 where sender info is displayed
' Change from:
' From: <%=rsQu("Name")%>
' To:
Set rsSender = dbConn.Execute("SELECT * FROM Users WHERE Code='" & rsQu("SenderCode") & "'")
If rsSender.EOF Then Set rsSender = rsQu  ' Fallback
From: <%=rsSender("Name")%><br>
Email: <%=rsSender("Email")%><br>
Phone: <%=rsSender("Phone")%>
```

**File: `Quotes/Email_Proc.asp`**
```vbscript
' Use SenderCode for email "From" field
Set rsSender = dbConn.Execute("SELECT * FROM Users WHERE Code='" & rsQu("SenderCode") & "'")
strFromEmail = rsSender("Email")
strFromName = rsSender("Name")
' Update email sending logic to use these instead of Session variables
```

### Option B: Auto-Detect (Less Flexible)
Always show Bert as sender for certain quote types or customers. Not recommended due to inflexibility.

---

## 3. Search Quotes by Customer

### Current State
- Quotes list filters by: Date Range, User, Division, Status
- No customer/company name search

### Database Changes
**NONE** - Uses existing `Quotes.CompanyId` -> `Companies.CompanyName`

### Code Changes

**File: `Quotes/Default.asp` - Add search field**

```vbscript
' Near line 40-50 where other filters are defined
strCustomerSearch = Trim(Request("CustomerSearch"))
%>
<!-- Add to filter table -->
<tr>
    <td>Customer:</td>
    <td><input type="text" name="CustomerSearch" value="<%=strCustomerSearch%>"></td>
</tr>
```

**File: `Quotes/IFrame.asp` - Add to WHERE clause**

```vbscript
' Near line 80-100 where SQL is built
sql = "SELECT Quotes.*, Companies.CompanyName, Users.Name " & _
      "FROM (Quotes INNER JOIN Companies ON Quotes.CompanyId = Companies.CompanyId) " & _
      "INNER JOIN Users ON Quotes.Code = Users.Code " & _
      "WHERE 1=1 "

' Add customer search
If strCustomerSearch <> "" Then
    sql = sql & " AND (Companies.CompanyName LIKE '%" & Replace(strCustomerSearch, "'", "''") & "%' " & _
          " OR Companies.CompanyName LIKE '%" & Replace(strCustomerSearch, "'", "''") & "%')"
End If

' Existing filters
If intDivisionId > 0 Then
    sql = sql & " AND Quotes.DivisionId = " & intDivisionId
End If

' ... rest of query
```

**File: `Quotes/IFrame.asp` - Add customer column to grid**
```javascript
// In grid column definition (around line 68-80)
// Add column for CompanyName
active-column-X {width: 150px; text-align: left;}  // Add to CSS section
```

### Alternative: Advanced Search Popup
Create `Quotes/Search.asp` with more options (customer, project, amount range) that redirects to filtered list.

---

## 4. Browser/Desktop Usability

### Non-Code Tasks
1. **Desktop Shortcut**
   - Create shortcut to `https://techlight.digitalresponse.com.au/Clients/SalesEngineTL/`
   - Icon: Use `/Images/favicon.ico` or Techlight logo

2. **Edge Configuration**
   - Add site to "IE Mode" list if compatibility issues persist
   - Allow pop-ups for the domain (for PDF generation)

### Code Changes (Optional Enhancements)

**File: `/Clients/SalesEngineTL/Portal/Validate.asp`**
```vbscript
' Add "Remember Me" checkbox (optional)
' Store frequently used email recipients in localStorage via JavaScript
```

**File: `Quotes/Email.asp` - Recent Recipients Dropdown**
```vbscript
' Query recent email recipients from QuoteEmailHistory table (if exists)
' Or use JavaScript localStorage to remember last 10 recipients
```

---

## 5. MYOB Integration - Export Invoices

### Business Requirement
Sherell manually enters invoices from MyDesk into MYOB for aged receivables. Need automated export/import.

### MYOB Background
- **MYOB AccountRight** (desktop) or **MYOB Business** (cloud)
- AccountRight supports: 
  - Text/CSV import for sales
  - ODBC connection (limited)
  - API (Business/essentials only)

### Option A: CSV Export (RECOMMENDED - Quick Win)

#### New Page: `Invoices/ExportToMYOB.asp`

```vbscript
<%
' Security check
If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then 
    Response.Redirect "../Portal/AccessDenied.asp"
End If

' Date range selection
dteFrom = Request("DateFrom")
dteTo = Request("DateTo")

If dteFrom = "" Then
    dteFrom = FormatDateU(DateAdd("d", -30, ServerToEST(Now())), False)
    dteTo = FormatDateU(Now(), False)
End If
%>
<!-- Form for date range -->
<form action="ExportToMYOB_Proc.asp" method="POST">
    From: <input type="text" name="DateFrom" value="<%=dteFrom%>">
    To: <input type="text" name="DateTo" value="<%=dteTo%>">
    <input type="submit" value="Generate CSV">
</form>
```

#### New Page: `Invoices/ExportToMYOB_Proc.asp`

```vbscript
<%
Response.ContentType = "text/csv"
Response.AddHeader "Content-Disposition", "attachment; filename=MyDesk_Invoices_" & Date() & ".csv"

' CSV Header for MYOB Sales Import
' Format: Co./Last Name,First Name,Addr 1,Addr 2,Invoice No,Date,Description,Amount,Job,Comment
Response.Write "Co./Last Name,First Name,Invoice No,Date,Description,Amount,Status" & vbCrLf

sql = "SELECT Invoices.*, Companies.CompanyName, Contacts.FirstName, Contacts.LastName " & _
      "FROM (Invoices INNER JOIN Companies ON Invoices.CompanyId = Companies.CompanyId) " & _
      "LEFT JOIN Contacts ON Invoices.ContactId = Contacts.ContactId " & _
      "WHERE Invoices.Date >= #" & Request("DateFrom") & "# " & _
      "AND Invoices.Date <= #" & Request("DateTo") & "# " & _
      "AND Invoices.InvoiceStatusId = 2 " & _  ' Issued only
      "ORDER BY Invoices.Date"

Set rs = dbConn.Execute(sql)

Do While Not rs.EOF
    ' Escape quotes in company name
    strCompany = Replace(rs("CompanyName"), """", """")
    strFirstName = Replace(rs("FirstName") & "", """", """)
    strLastName = Replace(rs("LastName") & "", """", """)
    
    Response.Write """" & strCompany & ""","  ' Co./Last Name
    Response.Write """" & strFirstName & " " & strLastName & ""","  ' First Name
    Response.Write rs("InvoiceNum") & ","  ' Invoice No
    Response.Write FormatDateU(rs("Date"), False) & ","  ' Date
    Response.Write """Invoice from Techlight"","  ' Description
    Response.Write rs("PriceExGST") & ","  ' Amount (ex GST - MYOB will calc GST)
    Response.Write "Issued"  ' Status comment
    Response.Write vbCrLf
    
    rs.MoveNext
Loop
%>
```

#### Database Changes

**Table: `InvoiceExportLog`** (Track what was exported)
```sql
CREATE TABLE InvoiceExportLog (
    ExportId COUNTER PRIMARY KEY,
    ExportDate DATETIME DEFAULT Now(),
    ExportedBy TEXT(10),
    DateFrom DATETIME,
    DateTo DATETIME,
    InvoiceCount INTEGER,
    TotalAmount CURRENCY,
    Status TEXT(20)
)
```

**Table: `Invoices`** (Add export tracking)
```sql
ALTER TABLE Invoices ADD COLUMN ExportedToMYOB YESNO DEFAULT No
ALTER TABLE Invoices ADD COLUMN ExportedDate DATETIME
```

Update `ExportToMYOB_Proc.asp` to mark invoices as exported:
```vbscript
' After generating CSV
sql = "UPDATE Invoices SET ExportedToMYOB=-1, ExportedDate=Now() " & _
      "WHERE Date >= #" & Request("DateFrom") & "# " & _
      "AND Date <= #" & Request("DateTo") & "#"
dbConn.Execute(sql)

' Log the export
dbConn.Execute "INSERT INTO InvoiceExportLog (ExportedBy, DateFrom, DateTo, InvoiceCount, Status) " & _
               "VALUES ('" & Request.Cookies("UserSettings")("Code") & "', #" & Request("DateFrom") & "#, #" & _
               Request("DateTo") & "#, " & rs.RecordCount & ", 'Exported')"
```

#### UI Changes

**File: `Invoices/Default.asp`**
- Add "Export to MYOB" button next to "Add Invoice"
- Add "Exported" column to grid (checkmark if ExportedToMYOB = Yes)

**File: `Invoices/IFrame.asp`**
- Add filter: "Show: All | Not Exported to MYOB | Exported"

### Option B: Direct MYOB AccountRight Integration (Advanced)

**Requires:** MYOB AccountRight with ODBC driver or API access

#### Components Needed
1. **MYOB ODBC Connection** - Read/Write directly to MYOB database
2. **MYOB API Client** - For cloud-based MYOB Business

#### New Page: `Invoices/SyncWithMYOB.asp`

This would use ASP.NET interop pattern (similar to PDF generation):

1. ASP Classic collects invoices to sync
2. Redirects to `MyDeskASPNet/SyncInvoicesToMYOB.aspx`
3. .NET code uses MYOB SDK/ODBC to push invoices
4. Returns status to ASP Classic

#### Code: `MyDeskASPNet/SyncInvoicesToMYOB.aspx.cs`

```csharp
using MYOB.AccountRight.SDK;
using MYOB.AccountRight.SDK.Services;

protected void Page_Load(object sender, EventArgs e)
{
    // Get OAuth tokens (configured in web.config)
    var configuration = new ApiConfiguration(
        "https://api.myob.com/accountright/",
        clientId, clientSecret, redirectUri);
    
    // Connect to company file
    var companyFile = GetCompanyFile(configuration);
    var cfService = new CompanyFileService(configuration);
    
    // Create invoice service
    var invoiceService = new InvoiceService(configuration);
    
    // Loop through invoice IDs passed from ASP Classic
    var qids = Request["InvoiceIds"].Split(',');
    foreach(var invoiceId in qids)
    {
        var invoice = new Invoice
        {
            Customer = new CustomerLink { UID = customerUid },
            Date = invoiceDate,
            Lines = new List<InvoiceLine>
            {
                new InvoiceLine
                {
                    Description = lineDescription,
                    Total = lineAmount
                }
            }
        };
        
        invoiceService.Insert(companyFile, invoice, credentials);
    }
}
```

#### Challenges
- MYOB AccountRight must be open for API/ODBC access
- OAuth authentication setup required
- Company file credentials needed
- More complex error handling

### Option C: One-Click Export + Email (Hybrid)

Sherell clicks button, system:
1. Generates CSV
2. Emails CSV to Sherell's inbox
3. Sherell saves and imports to MYOB

This combines Option A with email automation.

---

## Database Changes Summary

| Table | Change | Purpose |
|-------|--------|---------|
| `Quotes` | Add `SenderCode TEXT(10)` | Track quote sender |
| `Invoices` | Add `ExportedToMYOB YESNO` | Track export status |
| `Invoices` | Add `ExportedDate DATETIME` | When exported |
| `Users` | Add `CanSendAsOthers YESNO` | Permission to send as others |
| **NEW** `InvoiceExportLog` | Create table | Audit trail for exports |

---

## Implementation Priority

1. **CRITICAL**: Fix quote recalculation bug (affects business credibility)
2. **HIGH**: Add sender dropdown (Isaac onboarding blocker)
3. **MEDIUM**: Customer search (efficiency improvement)
4. **MEDIUM**: MYOB CSV export (Sherell time savings)
5. **LOW**: Browser shortcuts (one-time IT task)

---

## Testing Checklist

### Quote Recalculation
- [ ] Create quote with 3 items at different prices
- [ ] Edit item 2 quantity from 1 to 5
- [ ] Verify total = (sum of line net prices) * 1.10
- [ ] Edit item 3 price
- [ ] Verify total recalculates correctly
- [ ] Print quote - verify printed total matches screen

### Quote Sender
- [ ] Isaac creates quote, selects "From: Bert"
- [ ] Verify email shows Bert's name/email
- [ ] Verify printed quote shows Bert as sender
- [ ] Verify audit trail shows actual user (Isaac) made changes

### Customer Search
- [ ] Search "SDF" - should find "SDF ELECTRICAL"
- [ ] Search "Blue" - should find "Bluestar"
- [ ] Empty search shows all results

### MYOB Export
- [ ] Export 5 invoices to CSV
- [ ] Verify CSV imports to MYOB without errors
- [ ] Verify GST is calculated correctly in MYOB
- [ ] Verify invoices marked as "Exported" in MyDesk
