<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

' Get parameters
dteFrom = Request("DateFrom")
dteTo = Request("DateTo")
boolOnlyUnexported = (Request("OnlyUnexported") = "1")

' Validate dates
If dteFrom = "" OR dteTo = "" Then
    MyRedirect("ExportToMYOB.asp?Msg=Please+select+date+range")
End If

' Build SQL query
Dim strSQL, rsInvoices, intCount, curTotal

strSQL = "SELECT Invoices.*, Companies.CompanyName, Companies.ABN, Contacts.FirstName, Contacts.LastName, " & _
         "Contacts.Address, Contacts.City, Contacts.State, Contacts.Postcode, Divisions.Division " & _
         "FROM (((Invoices INNER JOIN Companies ON Invoices.CompanyId = Companies.CompanyId) " & _
         "INNER JOIN Contacts ON Invoices.ContactId = Contacts.ContactId) " & _
         "INNER JOIN Divisions ON Invoices.DivisionId = Divisions.DivisionId) " & _
         "WHERE Invoices.Date >= #" & DBDate(dteFrom) & "# " & _
         "AND Invoices.Date <= #" & DBDate(dteTo) & "# "

' Only export issued invoices
strSQL = strSQL & "AND Invoices.InvoiceStatusId = 2 "

' Optionally filter only unexported
If boolOnlyUnexported Then
    strSQL = strSQL & "AND (Invoices.ExportedToMYOB = 0 OR Invoices.ExportedToMYOB IS NULL) "
End If

' Order by date
strSQL = strSQL & "ORDER BY Invoices.Date, Invoices.InvoiceNum"

Set rsInvoices = dbConn.Execute(strSQL)

' Check if any invoices found
If rsInvoices.BOF AND rsInvoices.EOF Then
    MyRedirect("ExportToMYOB.asp?Msg=No+invoices+found+for+the+selected+date+range")
End If

' Set response headers for CSV download
Response.ContentType = "text/csv"
Response.AddHeader "Content-Disposition", "attachment; filename=MyDesk_Invoices_" & Year(Date()) & Month(Date()) & Day(Date()) & ".csv"

' MYOB AccountRight Import Format - Service Sales
' Columns: Co./Last Name, First Name, Addr 1 - Line 1, Addr 1 - Line 2, Addr 1 - City, Addr 1 - State, Addr 1 - Postcode, 
'          Invoice No., Date, Description, Account Number, Amount, Inc-Tax Amount, GST Amount

Response.Write "Co./Last Name,First Name,Addr 1 - Line 1,Addr 1 - Line 2,Addr 1 - City,Addr 1 - State,Addr 1 - Postcode,"
Response.Write "Invoice No.,Date,Description,Account Number,Amount,Inc-Tax Amount,GST Amount,Comment" & vbCrLf

intCount = 0
curTotal = 0

Do While Not rsInvoices.EOF
    Dim strCompany, strFirstName, strLastName, strAddress1, strAddress2, strCity, strState, strPostcode
    Dim strInvoiceNum, dteInvoiceDate, strDescription, strAccountNum, curAmount, curIncTaxAmount, curGSTAmount
    
    ' Customer details - escape quotes for CSV
    strCompany = CleanForCSV(rsInvoices("CompanyName") & "")
    strFirstName = CleanForCSV(rsInvoices("FirstName") & "")
    strLastName = CleanForCSV(rsInvoices("LastName") & "")
    strAddress1 = CleanForCSV(rsInvoices("Address") & "")
    strAddress2 = "" ' Can add address line 2 if available
    strCity = CleanForCSV(rsInvoices("City") & "")
    strState = CleanForCSV(rsInvoices("State") & "")
    strPostcode = CleanForCSV(rsInvoices("Postcode") & "")
    
    ' Invoice details
    strInvoiceNum = rsInvoices("InvoiceNum") & ""
    dteInvoiceDate = FormatDateU2(rsInvoices("Date"), False)
    strDescription = CleanForCSV("Invoice from Techlight - " & rsInvoices("Reference"))
    strAccountNum = "4-1000" ' Sales account - adjust as needed for MYOB chart of accounts
    
    ' Amounts
    curAmount = CDbl(rsInvoices("PriceExGST") & "")
    curGSTAmount = CDbl(rsInvoices("GST") & "")
    curIncTaxAmount = curAmount + curGSTAmount
    
    ' Write CSV row
    Response.Write """" & strCompany & ""","
    Response.Write """" & strFirstName & " " & strLastName & ""","
    Response.Write """" & strAddress1 & ""","
    Response.Write """" & strAddress2 & ""","
    Response.Write """" & strCity & ""","
    Response.Write """" & strState & ""","
    Response.Write """" & strPostcode & ""","
    Response.Write """" & strInvoiceNum & ""","
    Response.Write """" & dteInvoiceDate & ""","
    Response.Write """" & strDescription & ""","
    Response.Write """" & strAccountNum & ""","
    Response.Write curAmount & ","
    Response.Write curIncTaxAmount & ","
    Response.Write curGSTAmount & ","
    Response.Write """Exported from MyDesk"""
    Response.Write vbCrLf
    
    intCount = intCount + 1
    curTotal = curTotal + curAmount
    
    rsInvoices.MoveNext
Loop

rsInvoices.Close
Set rsInvoices = Nothing

' Mark invoices as exported
strSQL = "UPDATE Invoices SET ExportedToMYOB = -1, ExportedDate = Now() " & _
         "WHERE Date >= #" & DBDate(dteFrom) & "# " & _
         "AND Date <= #" & DBDate(dteTo) & "# " & _
         "AND InvoiceStatusId = 2"
If boolOnlyUnexported Then
    strSQL = strSQL & " AND (ExportedToMYOB = 0 OR ExportedToMYOB IS NULL)"
End If
dbConn.Execute(strSQL)

' Log the export
strSQL = "INSERT INTO InvoiceExportLog (ExportDate, ExportedBy, DateFrom, DateTo, InvoiceCount, TotalAmount, Status) " & _
         "VALUES (Now(), '" & Request.Cookies("UserSettings")("Code") & "', #" & DBDate(dteFrom) & "#, #" & DBDate(dteTo) & "#, " & intCount & ", " & curTotal & ", 'Exported')"
dbConn.Execute(strSQL)

' Helper function to clean strings for CSV
Function CleanForCSV(str)
    If IsNull(str) Then
        CleanForCSV = ""
    Else
        ' Replace double quotes with two double quotes for CSV escaping
        str = Replace(str, """", """)
        CleanForCSV = str
    End If
End Function

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
