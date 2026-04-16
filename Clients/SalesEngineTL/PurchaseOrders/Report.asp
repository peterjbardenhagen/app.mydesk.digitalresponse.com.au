<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
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
<!--#include virtual="/System/ssi_Security.inc"-->
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
	<body style="background-color:#ffffff;" Marginheight=0 Marginwidth=0 topMargin=0 leftMargin=0>
		<div class="NoPrint" style="background-color: #f8fafc; padding: 16px; border-bottom: 1px solid #e2e8f0; margin-bottom: 24px; display: flex; justify-content: space-between; align-items: center;">
			<div style="flex: 1;"></div>
			<div style="display: flex; gap: 12px; align-items: center;">
				<% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %>
				<button type="button" class="tl-btn-primary" onclick="print();">
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display:inline-block;vertical-align:middle;margin-right:6px;">
						<polyline points="6 9 6 2 18 2 18 9"></polyline>
						<path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
						<rect x="6" y="14" width="12" height="8"></rect>
					</svg>
					Print Report
				</button>
				<span style="font-size: 13px; color: #64748b;">(Landscape)</span>
				<% End If %>
			</div>
		</div>
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
		<div style="margin-bottom: 32px; padding: 0 24px;">
			<h1 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0 0 8px 0; font-family: 'Inter', sans-serif;">My Purchase Orders Report for <%= strName %></h1>
			<p style="font-size: 14px; color: #64748b; margin: 0; font-family: 'Inter', sans-serif;">
				<strong>Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %></strong><br>
				<% If intPOStatusId > 0 Then %>Of the status <%= strPOStatus %><br><% End If %>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %>
			</p>
			<p style="font-size: 12px; font-style: italic; color: #94a3b8; margin-top: 8px;">All prices are ex. GST.</p>
		</div>
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
		<div style="padding: 0 24px;">
			<table class="tl-data-grid" style="width: 100%; border-collapse: collapse;">
				<thead>
					<tr>
<%
	If intDivisionId = 0 Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">Division</th>
<%
	End If
	If intCompanyId = 0 Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">Supplier</th>
<%
	End If
	If strCode = "All" Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">User</th>
<%
	End If
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 60px;">PO #</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 100px;">Date</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 120px;">Status</th>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 120px;">Total Ex.</th>
					</tr>
				</thead>
				<tbody>
<%
	decRunningExTotal = 0
	Do Until rsQu.EOF
		decRunningExTotal = decRunningExTotal + rsQu("PriceExTotal")
%>
					<tr style="border-bottom: 1px solid #e2e8f0;">
<%
		If intDivisionId = 0 Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><%= rsQu("DivisionCode") %></td>
<%
		End If
		If intCompanyId = 0 Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><strong style="color: #0f172a;"><%= rsQu("CompanyName") %></strong></td>
<%
		End If
		If strCode = "All" Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><%= rsQu("Name") %></td>
<%
		End If
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><a href="#" onclick="ViewPurchaseOrder('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsQu("POid") %>);" style="color: #007bff; text-decoration: none; font-weight: 500;"><%= rsQu("POid") %></a></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; white-space: nowrap;"><%= FormatDateU(rsQu("PODate"),False) %></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><span class="tl-badge tl-badge-info"><%= rsQu("POStatus") %></span></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right; font-weight: 600;"><%= FormatCurrency(rsQu("PriceExTotal"),2) %></td>
					</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 8 And ItemId = " & rsQu("POid")
		Set rsComments = dbConn.Execute(sql)
		If Not(rsComments.BOF And rsComments.EOF) Then
%>
					<tr style="border-bottom: 2px solid #cbd5e1; background-color: #f8fafc;">
						<td colspan=10 style="padding: 12px 24px;">
							<div style="font-family: 'Inter', sans-serif; font-size: 12px; color: #475569;">
								<strong style="color: #0f172a; margin-bottom: 8px; display: block;">The following comments have been made:</strong>
<%
			Do Until rsComments.EOF
%>
								<div style="margin-bottom: 6px; padding-left: 12px; border-left: 2px solid #cbd5e1;">
									<span style="font-weight: 500;"><%= rsComments("Name") %> (<%= FormatDateU(rsComments("DateEntered"), False) %>):</span> <%= Replace(rsComments("Comment"), Chr(39), "<br>") %>
<%
				If CBool(rsComments("FollowUpRequired")) Then
%>
									<br><span style="color: #c53030; font-size: 11px;">Follow up is required <% If CBool(rsComments("FollowUpComplete")) Then %>and is complete<% Else %>and is not complete<% End If %></span>
<%
				Else
%>
									<br><span style="color: #38a169; font-size: 11px;">No follow up was required.</span>
<%
				End If
%>
								</div>
<%
				rsComments.MoveNext
			Loop
			If IsObject(rsComments) then
				rsComments.Close
				Set rsComments = Nothing
			End If
%>
							</div>
						</td>
					</tr>
<%
		End If
%>
<%
		rsQu.MoveNext
	Loop
%>
					<tr style="background-color: #f1f5f9; font-weight: 700;">
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
						<td colspan=3 style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;">Totals:</td>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningExTotal,2) %></td>
					</tr>
				</tbody>
			</table>
		</div>
<%
End If
rsQu.Close
Set rsQu = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->