<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
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
<html>
<head>
	<title></title>
	<META http-equiv="Cache-Control" content="no-cache">
	<META http-equiv="Expires" content="0">
	<META http-equiv="Pragma" content="no-cache">
	<link href="/System/Style2.css" rel="stylesheet" type="text/css" ></link>
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	<style>
		body, p, td, span, div {
			font-size:12px !important;
		}
		table {
			width: 100%;
			background-color: #ffffff;
			padding-bottom: 5px;
			padding-top: 5px;
			padding-left: 5px;
			padding-right: 5px;
			margin: 0px;
			border: 5px;
		}
		tr {
			margin: 5px;
		}
		td {
			padding: 10px;
			background-color:#eeeeee;
		}
		.header {
			font-weight: bold;
			background-color: #cccccc;
		}
		td,.header {
			font-size: 12px;
		}

	</style>
</head>
<body style="background-color:#eeeeee;">
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

Response.Write("<table cellspacing=0>")
Response.Write("<tr><td class='header'>First Name</td><td class='header'>Surname</td><td class='header'>Company</td><td class='header'>Action</td></tr>")
Do While Not (oRecordset.EOF)
	Dim Id
	Id = oRecordset("Id")
	Response.Write("<tr style='height:1px !important;background-color:white;'></tr>")
	Response.Write("<tr>")
	Response.Write("<td>" & oRecordset("First Name") & "</td>")
	Response.Write("<td>" & oRecordset("Surname") & "</td>")
	Response.Write("<td>" & oRecordset("CompanyName") & "</td>")
	Response.Write("<td>" & "<input type='button' onclick='parent.document.location.href=""../Contacts/View.asp?ContactId=" & Id & """' value='View'/> <input type='button' onclick='parent.document.location.href=""" & Request.Cookies("ClientSettings")("WorkingDir") & "/Contacts/Edit.asp?ContactId=" & Id & """' value='Edit'/> <input type='button' onclick='deleteRecord(" & Id & ");' value='Delete'/></td>")
	Response.Write("</tr>")
	oRecordset.MoveNext
Loop
Response.Write("</table>")

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