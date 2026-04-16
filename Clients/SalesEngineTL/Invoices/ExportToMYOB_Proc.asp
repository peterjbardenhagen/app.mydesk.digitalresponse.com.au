<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%
Response.ContentType = "text/csv"
Response.AddHeader "Content-Disposition", "attachment; filename=Techlight_Invoices_" & Replace(Date(),"/","-") & ".csv"

' Security check
If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then 
    Response.End
End If

' CSV Header for MYOB Sales Import
' Format: Co./Last Name,First Name,Invoice No,Date,Description,Amount,Status
Response.Write "Co./Last Name,First Name,Invoice No,Date,Description,Amount,Status" & vbCrLf

Dim dteFrom, dteTo
dteFrom = FormatDateU(Request("DateFrom"), False)
dteTo = FormatDateU(Request("DateTo"), False)

sql = "SELECT Invoices.*, Companies.Company AS CompanyName, Contacts.FirstName, Contacts.Surname AS LastName " & _
      "FROM (Invoices INNER JOIN Companies ON Invoices.CompanyId = Companies.CompanyId) " & _
      "LEFT JOIN Contacts ON Invoices.ContactId = Contacts.ContactId " & _
      "WHERE Invoices.Date >= #" & ConvertDate(dteFrom) & "# " & _
      "AND Invoices.Date <= #" & ConvertDate(dteTo) & "# " & _
      "AND Invoices.InvoiceStatusId = 2 " & _
      "ORDER BY Invoices.Date"

Set rs = dbConn.Execute(sql)

Dim invoiceCount
invoiceCount = 0

Do While Not rs.EOF
    ' Escape quotes in company name
    strCompany = Replace(rs("CompanyName") & "", """", """""")
    strFirstName = Replace(rs("FirstName") & "", """", """""")
    strLastName = Replace(rs("LastName") & "", """", """""")
    
    Response.Write """" & strCompany & ""","  ' Co./Last Name
    Response.Write """" & strFirstName & " " & strLastName & ""","  ' First Name
    Response.Write """" & rs("InvoiceNum") & ""","  ' Invoice No
    Response.Write """" & FormatDateU(rs("Date"), False) & ""","  ' Date
    Response.Write """Invoice from Techlight"","  ' Description
    Response.Write """" & rs("PriceExGST") & ""","  ' Amount (ex GST)
    Response.Write """Issued"""  ' Status comment
    Response.Write vbCrLf
    
    invoiceCount = invoiceCount + 1
    rs.MoveNext
Loop

' Update Invoices tracking and audit log
If invoiceCount > 0 Then
    ' Mark as exported
    sql = "UPDATE Invoices SET ExportedToMYOB=-1, ExportedDate=Now() " & _
          "WHERE Date >= #" & ConvertDate(dteFrom) & "# AND Date <= #" & ConvertDate(dteTo) & "# AND InvoiceStatusId = 2"
    dbConn.Execute(sql)
    
    ' Log to Export history table
    sql = "INSERT INTO InvoiceExportLog (ExportedBy, DateFrom, DateTo, InvoiceCount, Status) " & _
          "VALUES ('" & Replace(Request.Cookies("UserSettings")("Code"), "'", "''") & "', #" & ConvertDate(dteFrom) & "#, #" & ConvertDate(dteTo) & "#, " & invoiceCount & ", 'Exported')"
    On Error Resume Next
    dbConn.Execute(sql)
    On Error GoTo 0
End If

rs.Close
Set rs = Nothing
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
