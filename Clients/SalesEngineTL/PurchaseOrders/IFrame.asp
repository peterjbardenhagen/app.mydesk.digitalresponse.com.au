<%

'Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.AddHeader "pragma","no-cache"
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-cache"

On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Security.inc"-->
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
Dim boolRequest
Dim rsDivP
Dim intPOStatusId

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

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

intPOStatusId = CInt(Request("POStatusId"))

%>
<!--#include virtual="/System/ssi_dbConn_Open.inc"-->
<%

' Get division properties
Set rsDivP = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & intDivisionId
Set rsDivP = dbConn.Execute(sql)

If Not (rsDivP.BOF And rsDivP.EOF) Then
	If rsDivP("PurchaseRequests") Then
		boolRequest = true
	Else
		boolRequest = false
	End If
End If

rsDivP.Close
Set rsDivP = Nothing

%>
<html>
<head>
	<title>MyDesk</title>
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
sql = "SELECT DISTINCT PurchaseOrders.POid, iif(Divisions.PurchaseRequests = 1 And Not(PurchaseOrders.POStatusId >= 3 And Not(PurchaseOrders.POStatusId = 5 Or PurchaseOrders.POStatusId = 6)),'Not available',PurchaseOrders.POid) As [#], Divisions.Division AS [Division], Users.Name As [Originator], 'DESCRIPTION' AS [Description], C.CompanyName, 'ACTION' As Action, PurchaseOrderStatus.POStatus, 'FILES' AS Files, 'HISTORY' AS History, PurchaseOrders.PriceExTotal, PurchaseOrders.GST AS [GST Applicable], PurchaseOrders.PODate As [PO Date], PurchaseOrders.HasCapEx As [Is Cap Ex], PurchaseOrders.DateRequired As [Date Required], 'PONEXTAPPROVER' As [Next Approver], 'POLASTAPPROVER' As [Final Approver], iif(Locations.Company LIKE '%SPECIFIED ADDRESS%',PurchaseOrders.DeliverToLocation,Locations.Company + ' - ' + Locations.Suburb) As [Delivery Address] FROM Locations INNER JOIN (((Divisions INNER JOIN (Users INNER JOIN PurchaseOrders ON Users.Code = PurchaseOrders.Code) ON Divisions.DivisionId = PurchaseOrders.DivisionId) INNER JOIN Contacts_WithCustomersAndSuppliers_V2 AS C ON PurchaseOrders.ContactId = C.ContactId) INNER JOIN PurchaseOrderStatus ON PurchaseOrders.POStatusId = PurchaseOrderStatus.POStatusId) ON Locations.LocationId = PurchaseOrders.DeliverToLocationId WHERE "
sql = sql & " PurchaseOrders.DivisionId = " & intDivisionId & " AND PurchaseOrders.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("PurchaseOrders") & ") AND PurchaseOrders.Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") AND"
If strCode <> "All" Then sql = sql & " Users.Code = '" & strCode & "' AND"
If intCompanyId > 0 Then sql = sql & " C.CompanyId = " & intCompanyId & " AND"
If intPOStatusId > 0 Then
	If intPOStatusId = 555 Then
		If boolRequest Then
			sql = sql & " PurchaseOrders.POStatusId In (1,2,5) AND"
		Else
			sql = sql & " PurchaseOrders.POStatusId In (1,2,3,5) AND"
		End If
	Else
		sql = sql & " PurchaseOrders.POStatusId = " & intPOStatusId & " AND"
	End If
End If
sql = sql & " (PurchaseOrders.PODate >= #" & DBDate(dteDateFrom) & "# AND PurchaseOrders.PODate < #" & DBDate(dteDateTo) & "#) ORDER BY POid DESC"

Set oRecordset = dbConn.Execute(sql)

If oRecordset.BOF And oRecordset.EOF Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/NoRecords.asp")

Response.Write("<table cellspacing=0>")
Response.Write("<tr><td class='header'>PO #</td><td class='header'>Originator</td><td class='header'>Supplier</td><td class='header'>Action</td><td class='header'>Status</td><td class='header'>PO Date</td><td class='header'>PriceExTotal</td></tr>")
Do While Not (oRecordset.EOF)
	Dim Id
	Dim Action
	Id = oRecordset("POid")
	
	'Action
	Action = "<input type='button' onclick='parent.document.location.href = """ & Request.Cookies("ClientSettings")("WorkingDir") & "/PurchaseOrders/View.asp?POid=" & Id & """;' value='View' /> <input type='button' onclick='parent.document.location.href = """ & Request.Cookies("ClientSettings")("WorkingDir") & "/PurchaseOrders/UpdateStatus.asp?POid=" & Id & """;' value='Update Status'/> <input type='button' onclick='parent.document.location.href = """ & Request.Cookies("ClientSettings")("WorkingDir") & "/PurchaseOrders/Edit.asp?POid=" & Id & """;' value='Edit' />"
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId) Then
		Action = Action & " <input type='button' onclick='deleteRecord(" & Id & ");' value='Delete'/>"
	End If

	' Description
	Dim rsDesc
	Dim sql_Description
	Dim strDesc
	Dim i
	
	Set rsDesc = Server.CreateObject("ADODB.RecordSet")
	sql_Description = "Select Top 5 Quantity, Description From PurchaseOrderContents Where POid = " & Id
	Set rsDesc = dbConn.Execute(sql_Description)
	
	If Not(rsDesc.BOF And rsDesc.EOF) Then
		i = 1
		strDesc = "This quote includes the following:" & vbcrlf
		Do Until rsDesc.EOF
			strDesc = strDesc & rsDesc("Quantity") & " x " & rsDesc("Description") & vbcrlf
			i = i + 1
			rsDesc.MoveNext
		Loop
	End If
	
	If IsObject(rsDesc) Then
		rsDesc.Close
		Set rsDesc = Nothing
	End If
	
	If strDesc <> "" Then
		strDesc = "<a href='#' onclick='alert(""" & Replace(Replace(Replace(strDesc, vbcrlf, "\n")&"","'","`"),"""","`") & """);'>Click here</a>"
	Else
		strDesc = "No contents"
	End If	
	
	
	Response.Write("<tr style='height:1px !important;background-color:white;'></tr>")
	Response.Write("<tr>")
	Response.Write("<td>" & oRecordset("POid") & "</td>")
	Response.Write("<td>" & oRecordset("Originator") & "</td>")
'	Response.Write("<td>" & strDesc & "</td>")
	Response.Write("<td>" & oRecordset("CompanyName") & "</td>")
	Response.Write("<td>" & Action & "</td>")
	Response.Write("<td>" & oRecordset("POStatus") & "</td>")
	Response.Write("<td>" & FormatDateU2(oRecordset("PO Date"), False) & "</td>")
	Response.Write("<td>$" & FormatNumber(oRecordset("PriceExTotal"),2) & "</td>")
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
