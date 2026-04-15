<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
Dim dteDateFrom
Dim dteDateTo
Dim intCompanyId

strMsg =		Trim(Request("Msg"))
strCode =		Trim(Request.Form("Code"))
dteDateFrom =	Trim(Request.Form("DateFrom"))
dteDateTo =		Trim(Request.Form("DateTo"))
intCompanyId =	CLng(Request.Form("CompanyId"))

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
	<body style="background-color:#ffffff !important;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table2">
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

%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table4">
			<tr>
				<td valign="top"><span class="TimesHeader">My Call Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
		</table>
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select CallReports.*, CallReportTypes.*, Users.Name, Contacts_WithCustomersAndSuppliers_V2.* From (Users INNER JOIN (Contacts_WithCustomersAndSuppliers_V2 INNER JOIN CallReports ON Contacts_WithCustomersAndSuppliers_V2.ContactId = CallReports.ContactId) ON Users.Code = CallReports.Code) INNER JOIN CallReportTypes ON CallReports.CallReportTypeId = CallReportTypes.CallReportTypeId"
sql = sql & " WHERE Users.Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") AND"
If strCode <> "All" Then sql = sql & " Users.Code = '" & strCode & "' AND"
If intCompanyId <> 0 Then sql = sql & " Contacts_WithCustomersAndSuppliers_V2.CompanyId = " & intCompanyId & " AND"
sql = sql & " DateEntered >= #" & dteDateFrom & "# And DateEntered < #" & dteDateTo & "# ORDER BY Users.Name, DateEntered DESC"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
		<br/>
		<table width="1000" cellpadding=3 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td class="HeaderRow" width=90 nowrap><b>Date</b></td>
<%

	If strCode = "All" Then

%>
				<td class="HeaderRow"><b>Name</b></td>
<%

	End If

%>
				<td class="HeaderRow" width=80 nowrap><b>Type</b></td>
<%

	If intCompanyId = 0 Then

%>
				<td class="HeaderRow" width=120 nowrap><b>Customer</b></td>
<%

	End If

%>
				<td class="HeaderRow" width=120 nowrap><b>Contact</b></td>
				<td class="HeaderRow"><b>Purpose of Call</b></td>
				<td class="HeaderRow"><b>Comment</b></td>
			</tr>
<%

	Do Until rs.EOF

%>
			<tr bgcolor="#ffffff">
				<td style="vertical-align:top;" width=80 valign="top"><%= FormatDateU(rs("DateEntered"), False) %></td>
<%

		If strCode = "All" Then

%>
				<td style="vertical-align:top;" nowrap><%= rs("Name") %></td>
<%

		End If

%>
				<td style="vertical-align:top;"><%= rs("CallReportType") %></td>
<%

		If intCompanyId = 0 Then

%>
				<td style="vertical-align:top;"><%= rs("Company") %></td>
<%

		End If

%>
				<td style="vertical-align:top;"><%= rs("FirstName") & " " & rs("Surname") %></td>
				<td style="vertical-align:top;"><%= rs("CallPurpose") %></td>
				<td style="vertical-align:top;"><%= rs("Comment") %></td>
			</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 2 And ItemId = " & rs("CallReportId")
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
					<table width="100%" height=2 cellpadding=0 cellspacing=0 border=0 ID="Table1">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%

			If IsObject(rs2) Then
				rs2.Close
				Set rs2 = Nothing
			End If

		rs.MoveNext
	Loop

%>
		</table>
<%
	Set rsCalls = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Sum(TotalCalls) As TotalCalls2, Company From (Select Count(CallReports.CallReportId) As TotalCalls, C.Company As Company FROM Users INNER JOIN (CallReports INNER JOIN Contacts_WithCustomersAndSuppliers_V2 AS C ON CallReports.ContactId = C.ContactId) ON Users.Code = CallReports.Code Group By Users.DivisionId, Users.Code, C.CompanyId, C.Company, C.Code, CallReports.DateEntered Having "
	If strCode <> "All" Then
		sql = sql & " C.Code = '" & strCode & "' And"
	End If
	sql = sql & " (Users.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") OR Users.Code = '" & Request.Cookies("UserSettings")("Code") & "') AND CallReports.DateEntered >= #" & dteDateFrom & "# And CallReports.DateEntered < #" & dteDateTo & "# Order By C.Company) Group By Company"
	rsCalls.Open sql, dbConn, 0, 1

	If Not(rsCalls.BOF And rsCalls.EOF) Then
%>
		<br>
		<table width="500" cellpadding=5 cellspacing=0 border=0 ID="Table6">
			<tr>
				<td class="TimesItalicBold">Calls Summary</td>
			</tr>
			<tr>
				<td class="TimesItalicBold">Customer</td>
				<td class="TimesItalicBold" style="text-align:right;width:150px;">Contacts</td>
			</tr>
<%
		Do Until rsCalls.EOF
%>
			<tr>
				<td><%= rsCalls("Company") %></td>
				<td style="text-align:right;"><%= rsCalls("TotalCalls2") %></td>
			</tr>
<%
			rsCalls.MoveNext
		Loop
%>
		</table>
<%
	End If

	If IsObject(rsCalls) Then
		rsCalls.Close
		Set rsCalls = Nothing
	End If
Else
	Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no call reports</td></tr></table>")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>	
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
