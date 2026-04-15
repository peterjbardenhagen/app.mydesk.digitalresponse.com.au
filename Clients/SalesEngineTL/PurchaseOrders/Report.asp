<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strCode
Dim intDivisionId
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim intCompanyId
Dim strProject
Dim inPOStatusId
Dim boolRequest

strCode =		Trim(Request.Form("Code"))
intDivisionId =	CInt(Request.Form("DivisionId"))
dteDateFrom =	Request.Form("DateFrom")
dteDateTo =		Request.Form("DateTo")
intCompanyId =	Trim(Request.Form("CompanyId"))
strProject =	Trim(Request.Form("Project"))
intPOStatusId = CInt(Request.Form("POStatusId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
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
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body style="background-color:#ffffff;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
			</tr>
		</table>
		<br>
<%
If strCode <> "All" Then
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sqlUsers = "SELECT name FROM Users WHERE Code = '" & strCode & "'"
	Set rsUsers = dbConn.Execute(sqlUsers)

	strName = rsUsers("Name")

	rsUsers.Close
	Set rsUsers = Nothing
Else
	strName = "All Users"
End If

If intCompanyId <> 0 Then
	Set rsCU = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Company FROM Companies WHERE CompanyId = " & intCompanyId
	Set rsCU = dbConn.Execute(sql)

	strCustomer = rsCU("Company")

	rsCU.Close
	Set rsCU = Nothing
End If

If intDivisionId <> 0 And intDivisionId <> 555 Then
	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Division FROM Divisions WHERE DivisionId = " & intDivisionId
	Set rsDi = dbConn.Execute(sql)

	strDivision = rsDi("Division")

	rsDi.Close
	Set rsDi = Nothing
End If

If intPOStatusId <> 0 And Not (intPOStatusId = 555) Then
	Set rsPOStatus = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT POStatus FROM PurchaseOrderStatus WHERE POStatusId = " & intPOStatusId
	Set rsPOStatus = dbConn.Execute(sql)

	strPOStatus = rsPOStatus("POStatus")

	rsPOStatus.Close
	Set rsPOStatus = Nothing
ElseIf intPOStatusId = 555 Then
	strPOStatus = UCase("All (Active)")
End If
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Purchase Orders Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %><br>
				<% If intPOStatusId > 0 Then %>Of the status <%= strPOStatus %><br><% End If %>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td style="font-style:italic;"><br>All prices are ex. GST.<br><br></td>
			</tr>
		</table>
<%
Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Contacts_WithCustomersAndSuppliers_V2.CompanyName, PO.*, PO.IntroText AS IT, PO.InternalNotes AS [IN], Users.*, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM PurchaseOrderStatus INNER JOIN (PurchaseOrderPaymentTypes INNER JOIN (Contacts_WithCustomersAndSuppliers_V2 INNER JOIN (Users INNER JOIN PurchaseOrders AS PO ON Users.Code = PO.Code) ON Contacts_WithCustomersAndSuppliers_V2.ContactId = PO.ContactId) ON PurchaseOrderPaymentTypes.POPaymentTypeId = PO.POPaymentTypeId) ON PurchaseOrderStatus.POStatusId = PO.POStatusId" &_
		" Where PO.DivisionId = " & intDivisionId
If intPOStatusId <> 0 Then
	If intPOStatusId = 555 Then
		If boolRequest Then
			sql = sql & " AND PO.POStatusId In (1,2,5) "
		Else
			sql = sql & " AND PO.POStatusId In (1,2,3,5) "
		End If
	Else
		sql = sql & "AND (PO.POStatusId = " & intPOStatusId & ")"
	End If
End If
sql = sql & " AND (PO.PODate >= #" & DBDate(dteDateFrom) & "# AND PO.PODate < #" & DBDate(dteDateTo) & "#) AND ("
If strCode = "All" Then
	sql = sql & "(Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") AND Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("PurchaseOrders") & ")) OR "
End If
sql = sql & " PO.Code = '" & Request.Cookies("UserSettings")("Code") & "') ORDER BY PODate DESC"
Set rsQu = dbConn.Execute(sql)
If Not(rsQu.BOF And rsQu.EOF) Then
%>
		<table width="1000" cellpadding=3 cellspacing=0 border=0 ID="Table2">
			<tr>
<%
	If intDivisionId = 0 Then
%>
				<td class="HeaderRow">Division</td>
<%
	End If
	If intCompanyId = 0 Then
%>
				<td class="HeaderRow">Supplier</td>
<%
	End If
	If strCode = "All" Then
%>
				<td class="HeaderRow">User</td>
<%
	End If
%>
				<td class="HeaderRow" style="width:40px;">PO #</td>
				<td class="HeaderRow" style="width:80px;">Date</td>
				<td class="HeaderRow" style="width:120px;">Status</td>
				<td class="HeaderRow" style="text-align:right;" width=100>Total Ex.</td>
			</tr>
<%
	decRunningExTotal = 0
	Do Until rsQu.EOF
		decRunningExTotal = decRunningExTotal + rsQu("PriceExTotal")
%>
			<tr>
<%
		If intDivisionId = 0 Then
%>
				<td><%= rsQu("DivisionCode") %></td>
<%
		End If
		If intCompanyId = 0 Then
%>
				<td><%= rsQu("CompanyName") %></td>
<%
		End If
		If strCode = "All" Then
%>
				<td><%= rsQu("Name") %></td>
<%
		End If
%>
				<td style="width:40px;"><a href="#" onclick="ViewPurchaseOrder('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsQu("POid") %>);"><%= rsQu("POid") %></a></td>
				<td style="width:80px;"><%= FormatDateU(rsQu("PODate"),False) %></td>
				<td style="width:120px;"><%= rsQu("POStatus") %></td>
				<td style="text-align:right;" width=100><%= FormatCurrency(rsQu("PriceExTotal"),2) %></td>
			</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 8 And ItemId = " & rsQu("POid")
		Set rsComments = dbConn.Execute(sql)
		If Not(rsComments.BOF And rsComments.EOF) Then
%>
			<tr>
				<td colspan=10>
					<table bgcolor="#ffffff" width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table5">
						<tr>
							<td><b>The following comments have been made:</b><br></td>
						</tr>
<%
			Do Until rsComments.EOF
%>
						<tr>
							<td>On <%= FormatDateU(rsComments("DateEntered"), False) %> by <%= rsComments("Name") %>: <%= Replace(rsComments("Comment"), Chr(39), "<br>") %>
<%
				If CBool(rsComments("FollowUpRequired")) Then
%>
							Follow up is required <% If CBool(rsComments("FollowUpComplete")) Then %>and is complete<% Else %>and is not complete<% End If %>
<%
				Else
%>
							No follow up was required.
<%
				End If
%>
							</td>
						</tr>
<%
				rsComments.MoveNext
			Loop
			If IsObject(rsComments) then
				rsComments.Close
				Set rsComments = Nothing
			End If
%>
					</table><br>
				</td>
			</tr>
<%
		End If
%>
			<tr height=2>
				<td colspan=8>
					<table width="100%" height=2 cellpadding=0 cellspacing=0 border=0 ID="Table4">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%
		rsQu.MoveNext
	Loop
%>
			<tr>
<%
		If intCompanyId = 0 Then
%>
				<td></td>
<%
		End If
		If intDivisionId = 0 Then
%>
				<td></td>
<%
		End If
		If strCode = "All" Then
%>
				<td></td>
<%
		End If
%>
				<td colspan=3 align="right"><b>Totals:</b>&nbsp;</td>
				<td style="border-bottom:2px solid black;text-align:right;width:100px;"><%= FormatCurrency(decRunningExTotal,2) %></td>
			</tr>
		</table>
<%
End If
rsQu.Close
Set rsQu = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->