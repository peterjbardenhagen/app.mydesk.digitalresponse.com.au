<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

On Error Resume Next

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dates.inc"-->
<%

Dim strSort
Dim strCode
Dim intCompanyId
Dim dteDateFrom
Dim dteDateTo
Dim strLetter
Dim strBy

strSort = Trim(Request("Sort"))

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If strCode = "" Then
	strCode = "All"
End If

If strSort = "" Then
	strSort = "Quotes.Date DESC"
End If

intCompanyId = CLng(Request("CompanyId"))
dteDateFrom = FormatDateU(Request("DateFrom"), False)
dteDateTo = FormatDateU(Request("DateTo"), False)
strLetter = Trim(Request("Letter"))
strBy = Trim(Request("By"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Contacts List - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-cache">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-cache">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css" rel="stylesheet" type="text/css">
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
</head>
<body style="background-color:#ffffff; margin: 0; padding: 16px; font-family: 'Inter', sans-serif;">
<%

Dim oRecordset
Dim sql

' Execute a SQL query
sql = "SELECT DISTINCT ContactId As [Id], Users.Name AS [Originator], C.FirstName As [First Name], C.Surname, 'ACTION' As [Action], C.Position, C.CompanyName, C.CustomerCode AS [Customer Code], C.SupplierCode AS [Supplier Code], C.Email, C.Phone, C.Fax, C.Mobile, C.Address1, C.Address2, C.Suburb, C.PostCode As [Post Code], C.Deleted As Deleted FROM Contacts_WithCustomersAndSuppliers_V2 C INNER JOIN Users ON Users.Code = C.Code"
sql = sql & " WHERE C.Deleted = 0 AND "
If strLetter <> "All" Then
	If strBy = "Surname" Then
		sql = sql & "LEFT(C.Surname,1) = '" & strLetter & "' AND "
	Else
		sql = sql & "LEFT(C.CompanyName,1) = '" & strLetter & "' AND "
	End If
End If
sql = sql & " (Users.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Visible") & ") AND Users.Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & "))"
If strCode <> "All" Then sql = sql & " AND Users.Code = '" & strCode & "'"
If intCompanyId <> 0 Then sql = sql & " AND C.CompanyId = " & intCompanyId

if strBy = "Surname" Then
	sql = sql & " ORDER BY C.Surname, C.CompanyName, C.FirstName"
Else
	sql = sql & " ORDER BY C.CompanyName, C.Surname, C.FirstName"
End If

Set oRecordset = dbConn.Execute(sql)

If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

Response.Write("<table class='tl-data-grid'>")
Response.Write("<thead><tr><th>First Name</th><th>Surname</th><th>Company</th><th>Actions</th></tr></thead>")
Response.Write("<tbody>")
Do While Not (oRecordset.EOF)
	Dim Id
	Id = oRecordset("Id")
	Response.Write("<tr>")
	Response.Write("<td>" & oRecordset("First Name") & "</td>")
	Response.Write("<td><strong>" & oRecordset("Surname") & "</strong></td>")
	Response.Write("<td>" & oRecordset("CompanyName") & "</td>")
	Response.Write("<td nowrap><a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Contacts/View.asp?ContactId=" & Id & "' class='tl-btn-secondary' style='padding:4px 8px;font-size:12px;'>View</a> <a href='" & Request.Cookies("ClientSettings")("WorkingDir") & "/Contacts/Edit.asp?ContactId=" & Id & "' class='tl-btn-primary' style='padding:4px 8px;font-size:12px;'>Edit</a></td>")
	Response.Write("</tr>")
	oRecordset.MoveNext
Loop
Response.Write("</tbody></table>")

' Close recordset and connection
oRecordset.close
dbConn.close

If Err.Number <> 0 then
	Response.Write(Err.Description)
	Error.Clear
End If

%>
</body>
</html>