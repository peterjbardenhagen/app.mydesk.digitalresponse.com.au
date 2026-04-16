<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim strName
Dim strContactName
Dim intCompanyId
Dim strCustomer
Dim strOrderBy

strCode = Trim(Request.Form("Code"))
intCompanyId = CLng(Request("CompanyId"))
strOrderBy = Trim(Request("OrderBy"))

If strOrderBy = "" Then strOrderBy = " C.Company"

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
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body style="background-color:#ffffff !important;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
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

%>
		<div class="NoPrint" style="background-color: #f8fafc; padding: 16px; border-bottom: 1px solid #e2e8f0; margin-bottom: 24px; display: flex; justify-content: space-between; align-items: center;">
			<form method="post" action="?" name="Form1" style="margin: 0; display: flex; gap: 12px; align-items: center;">
			<input type="hidden" name="OrderBy" value="<%= strOrderBy %>" ID="Hidden1">
			<input type="hidden" name="CompanyId" value="<%= intCompanyId %>" ID="Hidden2">
			<input type="hidden" name="Code" value="<%= strCode %>" ID="Hidden3">
			<button type="button" class="tl-btn-secondary" onclick="document.Form1.OrderBy.value='C.CompanyName';document.Form1.submit();">Order by Company</button>
			<button type="button" class="tl-btn-secondary" onclick="document.Form1.OrderBy.value='C.Surname,C.FirstName';document.Form1.submit();">Order by Name</button>
			<% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %>
			<button type="button" class="tl-btn-primary" onclick="print();">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display:inline-block;vertical-align:middle;margin-right:6px;">
					<polyline points="6 9 6 2 18 2 18 9"></polyline>
					<path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
					<rect x="6" y="14" width="12" height="8"></rect>
				</svg>
				Print Report
			</button>
			<span style="font-size: 13px; color: #64748b;">(Set orientation to landscape)</span>
			<% End If %>
			</form>
		</div>

		<div style="margin-bottom: 32px; padding: 0 24px;">
			<h1 style="font-size: 24px; font-weight: 700; color: #0f172a; margin: 0 0 8px 0; font-family: 'Inter', sans-serif;">My Contacts Report for <%= strName %></h1>
			<p style="font-size: 14px; color: #64748b; margin: 0; font-family: 'Inter', sans-serif;">
				<strong><% If intCompanyId <> 0 Then %>Includes <%= strCustomer %><% Else %>Includes All companies<% End If %></strong><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %>
			</p>
		</div>
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT C.* FROM Contacts_WithCustomersAndSuppliers_V2 C "
If strCode <> "All" And intCompanyId <> 0 Then
	sql = sql & "WHERE Deleted = 0 AND C.Code = '" & strCode & "' AND C.CompanyId = " & intCompanyId
ElseIf strCode <> "All" And intCompanyId = 0 Then
	sql = sql & "WHERE Deleted = 0 AND C.Code = '" & strCode & "'"
ElseIf strCode = "All" And intCompanyId <> 0 Then
	sql = sql & "WHERE Deleted = 0 AND (C.Code = '" & Request.Cookies("UserSettings")("Code") & "' OR Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) AND (C.CompanyId = " & intCompanyId & ")"
ElseIf strCode = "All" And intCompanyId = 0 Then
	sql = sql & "WHERE Deleted = 0 AND (C.Code = '" & Request.Cookies("UserSettings")("Code") & "' OR Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & "))"
End If
sql = sql & " ORDER BY " & strOrderBy
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>

		<div style="padding: 0 24px;">
			<table class="tl-data-grid" style="width: 100%; border-collapse: collapse;">
				<thead>
					<tr>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 25%;">Full Name</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 35%;">Company & Address</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 15%;">Contact</th>
						<th style="text-align: left; padding: 12px; border-bottom: 2px solid #e2e8f0; font-family: 'Inter', sans-serif; font-weight: 600; color: #475569; width: 25%;">Email</th>
					</tr>
				</thead>
				<tbody>
<%

	Do Until rs.EOF

		If Trim(rs("FirstName")) <> "" And Trim(rs("Surname")) <> "." And Trim(rs("Surname")) <> "?" Then
			strContactName = rs("Surname") & ", " & rs("FirstName")
		Else
			strContactName = rs("FirstName")
		End If

		strAddress = ""
		If rs("Address1") <> "" Then
			strAddress = rs("Address1")
			If rs("Address2") <> "" Then
				strAddress = strAddress & ", " & rs("Address2")
			End If
			strAddress = strAddress & "<br>"
		End If
		strAddress = strAddress & " " & rs("Suburb") & " " & rs("PostCode")
		strAddress = Trim(strAddress)

%>
					<tr>
						<td style="padding: 12px; border-bottom: 1px solid #e2e8f0; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;">
							<strong style="color: #0f172a;"><% If Not(Trim(strContactName)&"" = "" Or IsNull(strContactName)) Then Response.Write(strContactName) Else Response.Write("Name not entered") %></strong><br>
							<span style="color: #64748b;"><% If Not(Trim(rs("Position"))&"" = "" Or IsNull(rs("Position"))) Then Response.Write(rs("Position")) Else Response.Write("Position not entered") %></span>
						</td>
						<td style="padding: 12px; border-bottom: 1px solid #e2e8f0; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;">
							<strong style="color: #0f172a;"><% If Not(Trim(rs("CompanyName"))&"" = "" Or IsNull(rs("CompanyName"))) Then Response.Write(rs("CompanyName")) Else Response.Write("Company not entered") %></strong><br>
							<span style="color: #64748b;"><% If Not(Trim(strAddress)&"" = "" Or IsNull(strAddress)) Then Response.Write(strAddress) Else Response.Write("Address not entered") %></span>
						</td>
						<td style="padding: 12px; border-bottom: 1px solid #e2e8f0; font-family: 'Inter', sans-serif; font-size: 13px; color: #475569; vertical-align: top;">
							<div><span style="font-weight: 500; color: #0f172a;">P:</span> <% If Not(Trim(rs("Phone"))&"" = "" Or IsNull(rs("Phone"))) Then Response.Write(rs("Phone")) Else Response.Write("Not entered") %></div>
							<div><span style="font-weight: 500; color: #0f172a;">F:</span> <% If Not(Trim(rs("Fax"))&"" = "" Or IsNull(rs("Fax"))) Then Response.Write(rs("Fax")) Else Response.Write("Not entered") %></div>
							<div><span style="font-weight: 500; color: #0f172a;">M:</span> <% If Not(Trim(rs("Mobile"))&"" = "" Or IsNull(rs("Mobile"))) Then Response.Write(rs("Mobile")) Else Response.Write("Not entered") %></div>
						</td>
						<td style="padding: 12px; border-bottom: 1px solid #e2e8f0; font-family: 'Inter', sans-serif; font-size: 13px; vertical-align: top;">
							<% If Not(Trim(rs("Email"))&"" = "" Or IsNull(rs("Email"))) Then Response.Write(ConvertToEmail(rs("Email"))) Else Response.Write("<span style='color: #94a3b8;'>Email not entered</span>") %>
						</td>
					</tr>
<%
		
		rs.MoveNext
	Loop
	
%>
				</tbody>
			</table>
		</div>
<%

Else
	Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no contacts for this user</td></tr></table>")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
