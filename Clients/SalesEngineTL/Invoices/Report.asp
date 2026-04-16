<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strCode
Dim intDivisionId
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim intCompanyId
Dim intInvoiceStatusId

strCode =		Trim(Request.Form("Code"))
intDivisionId =	CInt(Request.Form("DivisionId"))
dteDateFrom =	Request.Form("DateFrom")
dteDateTo =		Request.Form("DateTo")
intCompanyId =	Trim(Request.Form("CompanyId"))
intInvoiceStatusId = CInt(Request("InvoiceStatusId"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
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
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)
%>
		<div style="margin-bottom: 32px; padding: 0 24px;">
			<h1 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0 0 8px 0; font-family: 'Inter', sans-serif;">My Invoices Report for <%= strName %></h1>
			<p style="font-size: 14px; color: #64748b; margin: 0; font-family: 'Inter', sans-serif;">
				<strong>Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and All Entities") Else Response.Write("at " & strDivision) %></strong><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %>
			</p>
			<p style="font-size: 12px; font-style: italic; color: #94a3b8; margin-top: 8px;">All prices are ex. GST unless specified.</p>
		</div>
<%
Set rsIn = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT DISTINCTROW Invoices.*, InvoiceStatus.InvoiceStatus, Users.Name, iif(Invoices.CompanyId = 142, Invoices.CCompany, Companies.Company) AS [Company] "
sql = sql & "FROM Users INNER JOIN (Companies INNER JOIN ((Invoices INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) INNER JOIN Divisions ON Invoices.DivisionId = Divisions.DivisionId) ON Companies.CompanyId = Invoices.CompanyId) ON Users.Code = Invoices.Code "
sql = sql & "WHERE (Invoices.InvoiceDate >= #" & DBDate(dteDateFrom) & "# AND Invoices.InvoiceDate < #" & DBDate(dteDateTo) & "#) "
If intDivisionId <> 0 Then
	sql = sql & " AND Invoices.DivisionId = " & intDivisionId
End If
If intCompanyId <> 0 Then
	sql = sql & " AND Companies.CompanyId = " & intCompanyId
End If
If strCode <> "All" Then
	sql = sql & " AND Invoices.Code = '" & strCode & "'"
End If
If intInvoiceStatusId > 0 Then
	If intInvoiceStatusId = 555 Then
		sql = sql & " AND Invoices.InvoiceStatusId Not In (4,5)"
	Else
		sql = sql & " AND Invoices.InvoiceStatusId = " & intInvoiceStatusId
	End If
End If
sql = sql & " AND ((Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") AND Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ")) OR Invoices.Code = '" & Request.Cookies("UserSettings")("Code") & "') ORDER BY Invoices.InvoiceDate Desc, Divisions.Division, Companies.Company, Users.Name"
Set rsIn = dbConn.Execute(sql)

If Not(rsIn.BOF And rsIn.EOF) Then
%>
		<div style="padding: 0 24px;">
			<table class="tl-data-grid" style="width: 100%; border-collapse: collapse;">
				<thead>
					<tr>
<%
	If intDivisionId = 0 Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">Entity</th>
<%
	End If
	If intCompanyId = 0 Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">Customer</th>
<%
	End If
	If strCode = "All" Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">User</th>
<%
	End If
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 70px;">Invoice #</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 80px;">Date</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 80px;">Status</th>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 140px;">Nett Price (Ex. GST)</th>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 140px;">GST</th>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 140px;">Nett Price (Inc. GST)</th>
					</tr>
				</thead>
				<tbody>
<%
	decRunningGSTTotal = 0
	decRunningNettPriceTotal = 0
	Do Until rsIn.EOF
		decRunningGSTTotal = decRunningGSTTotal + rsIn("GSTTotal")
		decRunningNettPriceTotal = decRunningNettPriceTotal + rsIn("NettPriceTotal")
%>
					<tr style="border-bottom: 1px solid #e2e8f0;">
<%
		If intDivisionId = 0 Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><%= rsIn("DivisionCode") %></td>
<%
		End If
		If intCompanyId = 0 Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><strong style="color: #0f172a;"><%= rsIn("Company") %></strong></td>
<%
		End If
		If strCode = "All" Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><%= rsIn("Name") %></td>
<%
		End If
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><a href="#" onclick="ViewInvoice('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsIn("InvoiceId") %>);" style="color: #007bff; text-decoration: none; font-weight: 500;"><%= rsIn("InvoiceId") %></a></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; white-space: nowrap;"><%= FormatDateU(rsIn("InvoiceDate"),False) %></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><span class="tl-badge tl-badge-info"><%= rsIn("InvoiceStatus") %></span></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right;"><%= FormatCurrency(rsIn("NettPriceTotal"),2) %></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right;"><%= FormatCurrency(rsIn("GSTTotal"),2) %></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right; font-weight: 600;"><%= FormatCurrency(rsIn("NettPriceTotal")+rsIn("GSTTotal"),2) %></td>
					</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 10 And ItemId = " & rsIn("InvoiceId")
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
		rsIn.MoveNext
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
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningNettPriceTotal,2) %></td>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningGSTTotal,2) %></td>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningNettPriceTotal+decRunningGSTTotal,2) %></td>
					</tr>
				</tbody>
			</table>
		</div>
<%
End If
rsIn.Close
Set rsIn = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->