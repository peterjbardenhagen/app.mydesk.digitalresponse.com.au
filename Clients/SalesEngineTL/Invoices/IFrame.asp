<%

Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dates.inc"-->
<%

Dim nRows ' Number of rows in 
Dim nColumns
Dim rs
Dim cmd
Dim CurPage
Dim strCode
Dim intDivisionId
Dim intCompanyId
Dim strProject
Dim dteDateFrom
Dim dteDateTo
Dim intInvoiceStatusId
Dim sql

strCode = Trim(Request("Code"))

If strCode = "" Then
	strCode = "All"
End If

intDivisionId = CInt(Request("DivisionId"))
intCompanyId = CLng(Request("CompanyId"))
strProject = Trim(Request("Project"))

If strProject = "" Then
	strProject = "All"
End If

dteDateFrom = FormatDateU(Request("DateFrom"), False)
dteDateTo = FormatDateU(Request("DateTo"), False)
intInvoiceStatusId = CInt(Request("InvoiceStatusId"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<html>
<head>
	<title>MyDesk</title>
	<META http-equiv="Cache-Control" content="no-cache">
	<META http-equiv="Expires" content="0">
	<META http-equiv="Pragma" content="no-cache">
	<link href="/System/Style2.css" rel="stylesheet" type="text/css" ></link>
	<link href="/System/grid.css" rel="stylesheet" type="text/css" ></link>
	<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>

	<!-- grid format -->
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
Dim strSql

' Execute a SQL query
strSql = "SELECT DISTINCT Invoices.InvoiceId As [Invoice #], Divisions.Division AS [Division], iif(Invoices.CompanyId = 142, Invoices.CCompany, Companies.Company) AS [Company], 'DESCRIPTION' AS [Description], 'ACTION' As Action, InvoiceStatus.InvoiceStatus As [Invoice Status], Invoices.InvoiceDate As [Invoice Date], Invoices.NettPriceTotal, Users.Name As [Invoiced By], 'HISTORY' AS History FROM ((Divisions INNER JOIN (Users INNER JOIN Invoices ON Users.Code = Invoices.Code) ON Divisions.DivisionId = Invoices.DivisionId) INNER JOIN Companies ON Companies.CompanyId = Invoices.CompanyId) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " Invoices.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " Invoices.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
If intCompanyId > 0 Then
	strSql = strSql & " Companies.CompanyId = " & intCompanyId & " AND"
End If
If intInvoiceStatusId > 0 Then
	If intInvoiceStatusId = 555 Then
		strSql = strSql & " Invoices.InvoiceStatusId Not In (2) AND"
	Else
		strSql = strSql & " Invoices.InvoiceStatusId = " & intInvoiceStatusId & " AND"
	End If
End If
strSql = strSql & " (Invoices.InvoiceDate >= #" & DBDate(dteDateFrom) & "# AND Invoices.InvoiceDate < #" & DBDate(dteDateTo) & "#) ORDER BY InvoiceId DESC"
Set oRecordset = dbConn.Execute(strSql)

If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

Response.Write("<table cellspacing=0>")
Response.Write("<tr><td class='header'>Invoice #</td><td class='header'>Company</td><td class='header'>Action</td><td class='header'>Invoice Status</td><td class='header'>Invoice Date</td><td class='header'>Nett Price Total</td><td class='header'>Invoiced By</td></tr>")
Do While Not (oRecordset.EOF)
	Dim Id
	Dim Action
	Dim Description
	Id = oRecordset("Invoice #")
	Action = "<input type='button' onclick='parent.document.location.href = """ & Request.Cookies("ClientSettings")("WorkingDir") & "/Invoices/View.asp?InvoiceId=" & Id & """;' value='View'/> <input type='button' onclick='parent.document.location.href = """ & Request.Cookies("ClientSettings")("WorkingDir") & "/Invoices/ViewDeliveryNote.asp?InvoiceId=" & Id & """;' value='Delivery Note'/> <input type='button' onclick='deleteRecord(" & Id & ");' value='Delete'/>"
	Description = ""
	
	
	
	
	' Description
	Dim rsDesc
	Dim strDesc
	Dim i
	Dim s

	Set rsDesc = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From InvoiceContents Where InvoiceId = " & Id
	Set rsDesc = dbConn.Execute(sql)

	If Not(rsDesc.BOF And rsDesc.EOF) Then
		i = 1
		strDesc = "This invoice includes the following:" & vbcrlf
		Do Until rsDesc.EOF
			strDesc = strDesc & rsDesc("Description") & vbcrlf
			i = i + 1
			rsDesc.MoveNext
		Loop
	End If

	If IsObject(rsDesc) Then
		rsDesc.Close
		Set rsDesc = Nothing
	End If

	If strDesc <> "" Then
		s = "<a href='#' onclick='alert(""" & Replace(Replace(Replace(strDesc, vbcrlf, "\n")&"","'","`"),"""","`") & """);'>Click here</a>"
	Else
		s = "No contents"
	End If

	
	
	Response.Write("<tr style='height:1px !important;background-color:white;'></tr>")
	Response.Write("<tr>")
	Response.Write("<td>" & Id & "</td>")
	Response.Write("<td>" & oRecordset("Company") & "</td>")
'	Response.Write("<td>" & s & "</td>")
	Response.Write("<td>" & Action & "</td>")
	Response.Write("<td>" & oRecordset("Invoice Status") & "</td>")
	Response.Write("<td>" & FormatDateU2(oRecordset("Invoice Date"), False) & "</td>")
	Response.Write("<td>$" & FormatNumber(oRecordset("NettPriceTotal"),2) & "</td>")
	Response.Write("<td>" & oRecordset("Invoiced By") & "</td>")
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
