<% 

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
Dim strProject
Dim intQuoteStatusId

strCode =		Trim(Request.Form("Code"))
intDivisionId =	CInt(Request.Form("DivisionId"))
dteDateFrom =	Request.Form("DateFrom")
dteDateTo =		Request.Form("DateTo")
intCompanyId =	Trim(Request.Form("CompanyId"))
strProject =	Trim(Request.Form("Project"))
intQuoteStatusId = CInt(Request("QuoteStatusId"))


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
				<button type="button" class="tl-btn-secondary" onclick="parent.document.location.href=parent.document.location.href;">Close [x]</button>
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
			<h1 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0 0 8px 0; font-family: 'Inter', sans-serif;">My Quotes Report for <%= strName %></h1>
			<p style="font-size: 14px; color: #64748b; margin: 0; font-family: 'Inter', sans-serif;">
				<strong>Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %></strong><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %>
			</p>
			<p style="font-size: 12px; font-style: italic; color: #94a3b8; margin-top: 8px;">All prices are ex. GST.</p>
		</div>
<%
Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, iif(Companies.CompanyId=142,Contacts.CCompany,Companies.Company) As Company, QuoteStatus.QuoteStatus, Users.Name, Divisions.DivisionCode From (Users INNER JOIN (Companies INNER JOIN ((Quotes INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) INNER JOIN Contacts ON Quotes.ContactId = Contacts.ContactId) ON Companies.CompanyId = Contacts.CompanyId) ON Users.Code = Quotes.Code) INNER JOIN Divisions ON Quotes.DivisionId = Divisions.DivisionId Where (Quotes.QuoteDate >= #" & DBDate(dteDateFrom) & "# AND Quotes.QuoteDate < #" & DBDate(dteDateTo) & "#) "
If intDivisionId <> 0 Then
	sql = sql & " AND Quotes.DivisionId = " & intDivisionId
End If
If intCompanyId <> 0 Then
	sql = sql & " AND Companies.CompanyId = " & intCompanyId
End If
If strCode <> "All" Then
	sql = sql & " AND Quotes.Code = '" & strCode & "'"
End If
If intQuoteStatusId > 0 Then
	If intQuoteStatusId = 555 Then
		sql = sql & " AND Quotes.QuoteStatusId Not In (4,5)"
	Else
		sql = sql & " AND Quotes.QuoteStatusId = " & intQuoteStatusId
	End If
End If
sql = sql & " AND ((Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") AND Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ")) OR Quotes.Code = '" & Request.Cookies("UserSettings")("Code") & "') ORDER BY Quotes.QuoteDate Desc, Divisions.Division, Companies.Company, Users.Name"
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
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">Customer</th>
<%
	End If
	If strCode = "All" Then
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569;">User</th>
<%
	End If
%>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 70px;">Quote #</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 80px;">Date</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 80px;">Status</th>
<%
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rsQu("DivisionId")) Then
%>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 90px;">Total Cost</th>
<%
	End If
%>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 90px;">Nett Price Total</th>
<%
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rsQu("DivisionId")) Then
%>
						<th style="text-align: right; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 50px;">Margin</th>				
<%
	End If
%>
					</tr>
				</thead>
				<tbody>
<%
	decRunningUnitCostTotal = 0
	decRunningNettPriceTotal = 0
	Do Until rsQu.EOF
		decRunningUnitCostTotal = decRunningUnitCostTotal + rsQu("UnitCostTotal")
		decRunningNettPriceTotal = decRunningNettPriceTotal + rsQu("NettPriceTotal")
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
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><strong style="color: #0f172a;"><%= rsQu("Company") %></strong><br><span style="color: #64748b;"><%= rsQu("Reference") %></span></td>
<%
		End If
		If strCode = "All" Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><%= rsQu("Name") %></td>
<%
		End If
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><a href="#" onclick="ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsQu("Qid") %>);" style="color: #007bff; text-decoration: none; font-weight: 500;"><%= rsQu("Qid") %></a></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; white-space: nowrap;"><%= FormatDateU(rsQu("QuoteDate"),False) %></td>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;"><span class="tl-badge tl-badge-info"><%= rsQu("QuoteStatus") %></span></td>
<%
		If boolDivisionManager Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right;"><%= FormatCurrency(rsQu("UnitCostTotal"),2) %></td>
<%
		End If
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right; font-weight: 600;"><%= FormatCurrency(rsQu("NettPriceTotal"),2) %></td>
<%
		If boolDivisionManager Then
%>
						<td style="padding: 12px; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top; text-align: right;"><%= FormatNumber(rsQu("Margin"),2) %>%</td>
<%
		End If
%>
					</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 6 And ItemId = " & rsQu("Qid")
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
<%
		If boolDivisionManager Then
%>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningUnitCostTotal,2) %></td>
<%
		End If
%>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><%= FormatCurrency(decRunningNettPriceTotal,2) %></td>
<%
		If boolDivisionManager Then
%>
						<td style="text-align: right; padding: 16px 12px; font-family: 'Inter', sans-serif; font-size: 14px;"><% If decRunningUnitCostTotal > 0 And decRunningNettPriceTotal > 0 Then Response.Write(FormatNumber(100*(1-(decRunningUnitCostTotal/decRunningNettPriceTotal)))) Else Response.Write("0.00") %>%</td>
<%
		End If
%>
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