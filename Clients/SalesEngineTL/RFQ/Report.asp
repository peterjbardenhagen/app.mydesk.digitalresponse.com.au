<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strCode
Dim intDivisionId
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim intCompanyId
Dim strProject

strCode =		Trim(Request.Form("Code"))
intDivisionId =	CInt(Request.Form("DivisionId"))
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
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
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

If intDivisionId <> 0 And intDivisionId <> 555 Then
	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Division FROM Divisions WHERE DivisionId = " & intDivisionId
	Set rsDi = dbConn.Execute(sql)

	strDivision = rsDi("Division")

	rsDi.Close
	Set rsDi = Nothing
End If
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Request For Quotes Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td><br></td>
			</tr>
		</table>
<%
Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT DISTINCT RFQ.*, C.*, RFQStatus.*, Users.Name FROM Locations INNER JOIN (((Divisions INNER JOIN (Users INNER JOIN RFQ ON Users.Code = RFQ.Code) ON Divisions.DivisionId = RFQ.DivisionId) INNER JOIN Contacts_WithCustomersAndSuppliers_V2 AS C ON RFQ.ContactId = C.ContactId) INNER JOIN RFQStatus ON RFQ.RFQStatusId = RFQStatus.RFQStatusId) ON Locations.LocationId = RFQ.DeliverToLocationId WHERE ("
If intDivisionId > 0 Then
	sql = sql & " RFQ.DivisionId = " & intDivisionId & " AND (RFQ.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") OR RFQ.Code = '" & Request.Cookies("UserSettings")("Code") & "') AND"
End If
If Request.Cookies("UserSettings")("Manager") Then
	If strCode <> "All" Then
		sql = sql & " Users.Code = '" & strCode & "' AND"
	End If
Else
	sql = sql & " Users.Code = '" & Request.Cookies("UserSettings")("Code") & "' AND"
End If
If intCompanyId > 0 Then
	sql = sql & " C.CompanyId = " & intCompanyId & " AND"
End If
sql = sql & " (((Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") AND Users.DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("RFQ") & ")) OR RFQ.Code = '" & Request.Cookies("UserSettings")("Code") & "')))"
sql = sql & " AND (RFQ.RFQDate >= #" & DBDate(dteDateFrom) & "# AND RFQ.RFQDate < #" & DBDate(dteDateTo) & "#) ORDER BY RFQid DESC"
Set rsQu = dbConn.Execute(sql)

If Not(rsQu.BOF And rsQu.EOF) Then
%>
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
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
				<td class="HeaderRow" style="width:80px;">RFQ #</td>
				<td class="HeaderRow" style="width:80px;">Date</td>
				<td class="HeaderRow" style="width:80px;">Status</td>
				<td class="HeaderRow" style="text-align:right;width:100px;">Total Ex GST</td>
				<td class="HeaderRow" style="text-align:right;width:100px;">Total Inc GST</td>
			</tr>
<%
	decRunningTotalEx = 0
	decRunningTotalInc = 0
	Do Until rsQu.EOF
		decRunningTotalEx = decRunningTotalEx + rsQu("TotalEx")
		decRunningTotalInc = decRunningTotalInc + rsQu("TotalInc")
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
				<td style="width:80px;"><%= rsQu("RFQId") %></td>
				<td style="width:80px;"><%= FormatDateU(rsQu("RFQDate"),False) %></td>
				<td nowrap><%= rsQu("RFQStatus") %></td>
				<td style="text-align:right;width:100px;"><%= FormatCurrency(rsQu("TotalEx"),2) %></td>
				<td style="text-align:right;width:100px;"><%= FormatCurrency(rsQu("TotalInc"),2) %></td>
			</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 7 And ItemId = " & rsQu("RFQId")
		Set rsComments = dbConn.Execute(sql)
		If Not(rsComments.BOF And rsComments.EOF) Then
%>
			<tr>
				<td colspan=8>
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
				<td style="border-bottom:2px solid black;text-align:right;width:100px;"><%= FormatCurrency(decRunningTotalEx,2) %></td>
				<td style="border-bottom:2px solid black;text-align:right;width:100px;"><%= FormatCurrency(decRunningTotalInc,2) %></td>
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
