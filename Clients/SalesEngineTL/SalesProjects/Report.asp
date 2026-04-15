<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim intCompanyId
Dim strProject

strCode =		Trim(Request.Form("Code"))
dteDateFrom =	Request.Form("DateFrom")
dteDateTo =		Request.Form("DateTo")
intCompanyId =	Trim(Request.Form("CompanyId"))
strProject =	Trim(Request.Form("Project"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
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
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
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
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Sales Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %> and <% If strProject = "All" Then Response.Write("all projects") Else Response.Write(strProject) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
		</table>
		
<%
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT iif(SalesProjects.AcceptedDate > #01-Jan-01#, 'Accepted', iif(SalesProjects.RejectedDate > #01-Jan-01#, 'Rejected', iif(SalesProjects.ProspectDate > #01-Jan-01#, 'Prospect', iif(SalesProjects.TenderDate > #01-Jan-01#, 'Tender', 'In Progress')))) As [Status], SalesProjects.*, Users.Name, Contacts_WithCustomers.*, Contacts_WithCustomers.Company As CompanyName FROM (SalesProjects INNER JOIN Users ON Users.Code = SalesProjects.Code) Inner Join Contacts_WithCustomers On Contacts_WithCustomers.ContactId = SalesProjects.ContactId WHERE "
If strCode <> "All" Then
	sql = sql & "SalesProjects.Code = '" & strCode & "' AND "
End If
If intCompanyId <> 0 Then
	sql = sql & "Contacts_WithCustomers.CompanyId = " & intCompanyId & " AND "
End If
If strProject <> "All" Then
	sql = sql & "SalesProjects.Project = '" & strProject & "' AND "
End If
sql = sql & " Not(SalesProjects.RejectedDate > #01-Jan-01#) AND Users.Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") AND (SalesProjects.DateEntered >= #" & DBDate(dteDateFrom) & "# AND SalesProjects.DateEntered < #" & DBDate(dteDateTo) & "#) ORDER BY SalesProjects.[DateEntered] DESC"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>

		<br>
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
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
	Do Until rs.EOF
%>
			<tr>
				<td style="vertical-align:top;" nowrap>
					<table width="100%" cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td width=130 nowrap style="font-weight:bold;">Date entered</td>
							<td><%= FormatDateU(rs("DateEntered"), False) %></td>
						</tr>
						<tr>
							<td width=130 nowrap style="font-weight:bold;">Status</td>
							<td><%= rs("Status") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Name</td>
							<td><%= rs("Name") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Customer</td>
							<td><%= rs("CompanyName") %> - <%= rs("Surname") & ", " & rs("FirstName") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Project</td>
							<td><%= rs("Project") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Product/Service</td>
							<td><%= rs("Product") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">One Off Sales Project</td>
							<td><%= Replace(Replace(rs("OneOffSalesProject"), True, "Yes"), False, "No") %></td>
						</tr>
<%
	If rs("OneOffSalesProject") Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Value</td>
							<td><% If IsNumeric(rs("Value")) Then Response.Write(FormatCurrency(rs("Value"), 2)) %></td>
						</tr>
<%
	Else
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Value Per Month</td>
							<td><% If IsNumeric(rs("AmountPerMonth")) Then Response.Write(FormatCurrency(rs("AmountPerMonth"), 2)) %></td>
						</tr>
<%
	End If
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Potential Order Date</td>
							<td><%= FormatDateU2(rs("PotentialOrderDate"), false) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;">Comment</td>
							<td><%= Replace(rs("Comment"), Chr(10), "<br>") %></td>
						</tr>
					</table>
				</td>
			</tr>
			</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 5 And ItemId = " & rs("SalesProjectId")
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

		' Table Files
		Set rsFiles = Server.CreateObject("ADODB.RecordSet")
		sql = "Select TableFiles.*, Users.Name From TableFiles Inner Join Users On Users.Code = TableFiles.Code Where TableId = 5 And ItemId = " & rs("SalesProjectId")
		Set rsFiles = dbConn.Execute(sql)
		If Not(rsFiles.BOF And rsFiles.EOF) Then
%>
			<tr>
				<td colspan=10>
					<table bgcolor="#ffffff" width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table6">
						<tr>
							<td><b>The following files have been uploaded:</b><br></td>
						</tr>
<%
			Do Until rsFiles.EOF
%>
						<tr>
							<td>On <%= FormatDateU(rsFiles("DateEntered"), False) %> by <%= rsFiles("Name") %>: <%= Replace(rsFiles("Description"), Chr(39), "<br>") %></td>
						</tr>
<%
				rsFiles.MoveNext
			Loop
			If IsObject(rsFiles) then
				rsFiles.Close
				Set rsFiles = Nothing
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
					<table width="100%" height=2 cellpadding=0 cellspacing=0 border=0 ID="Table2">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%
		rs.MoveNext
	Loop	
%>
		</table>
<%
Else
	Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no Sales Projects</td></tr></table>")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
