<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
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
	<body style="background-color:white;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="parent.document.location.href=parent.document.location.href;" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
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
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Invoices Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and All Entities") Else Response.Write("at " & strDivision) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td style="font-style:italic;"><br>All prices are ex. GST.<br><br></td>
			</tr>
		</table>
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
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
			<tr>
<%
	If intDivisionId = 0 Then
%>
				<td class="HeaderRow">Entity</td>
<%
	End If
	If intCompanyId = 0 Then
%>
				<td class="HeaderRow">Customer</td>
<%
	End If
	If strCode = "All" Then
%>
				<td class="HeaderRow">User</td>
<%
	End If
%>
				<td class="HeaderRow" style="width:70px;">Invoice #</td>
				<td class="HeaderRow" style="width:80px;">Date</td>
				<td class="HeaderRow" style="width:80px;">Status</td>
				<td class="HeaderRow" style="text-align:right;width:140px;">Nett Price (Ex. GST)</td>
				<td class="HeaderRow" style="text-align:right;width:140px;">GST</td>
				<td class="HeaderRow" style="text-align:right;width:140px;">Nett Price (Inc. GST)</td>
			</tr>
<%
	decRunningGSTTotal = 0
	decRunningNettPriceTotal = 0
	Do Until rsIn.EOF
		decRunningGSTTotal = decRunningGSTTotal + rsIn("GSTTotal")
		decRunningNettPriceTotal = decRunningNettPriceTotal + rsIn("NettPriceTotal")
%>
			<tr>
<%
		If intDivisionId = 0 Then
%>
				<td valign="top"><%= rsIn("DivisionCode") %></td>
<%
		End If
		If intCompanyId = 0 Then
%>
				<td valign="top"><b><%= rsIn("Company") %></b></td>
<%
		End If
		If strCode = "All" Then
%>
				<td valign="top"><%= rsIn("Name") %></td>
<%
		End If
%>
				<td valign="top" style="width:70px;"><a href="#" onclick="ViewInvoice('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsIn("InvoiceId") %>);"><%= rsIn("InvoiceId") %></a></td>
				<td valign="top" style="width:80px;" nowrap><%= FormatDateU(rsIn("InvoiceDate"),False) %></td>
				<td valign="top" style="width:80px;"><%= rsIn("InvoiceStatus") %></td>
				<td valign="top" style="text-align:right;width:140px;"><%= FormatCurrency(rsIn("NettPriceTotal"),2) %></td>
				<td valign="top" style="text-align:right;width:140px;"><%= FormatCurrency(rsIn("GSTTotal"),2) %></td>
				<td valign="top" style="text-align:right;width:140px;"><%= FormatCurrency(rsIn("NettPriceTotal")+rsIn("GSTTotal"),2) %></td>
			</tr>
<%
		Set rsComments = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Comments.*, Users.Name From Comments Inner Join Users On Users.Code = Comments.FromCode Where TableId = 10 And ItemId = " & rsIn("InvoiceId")
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
		rsIn.MoveNext
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
				<td style="border-bottom:2px solid black;text-align:right;width:140px;"><%= FormatCurrency(decRunningNettPriceTotal,2) %></td>
				<td style="border-bottom:2px solid black;text-align:right;width:140px;"><%= FormatCurrency(decRunningGSTTotal,2) %></td>
				<td style="border-bottom:2px solid black;text-align:right;width:140px;"><%= FormatCurrency(decRunningNettPriceTotal+decRunningGSTTotal,2) %></td>
			</tr>
		</table>
<%
End If
rsIn.Close
Set rsIn = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->