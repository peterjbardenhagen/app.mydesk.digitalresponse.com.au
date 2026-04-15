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
				<td valign="top"><span class="TimesHeader">My Quotes Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td style="font-style:italic;"><br>All prices are ex. GST.<br><br></td>
			</tr>
		</table>
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
				<td class="HeaderRow">Customer</td>
<%
	End If
	If strCode = "All" Then
%>
				<td class="HeaderRow">User</td>
<%
	End If
%>
				<td class="HeaderRow" style="width:70px;">Quote #</td>
				<td class="HeaderRow" style="width:80px;">Date</td>
				<td class="HeaderRow" style="width:80px;">Status</td>
<%
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rsQu("DivisionId")) Then
%>
				<td class="HeaderRow" style="text-align:right;width:90px;">Total Cost</td>
<%
	End If
%>
				<td class="HeaderRow" style="text-align:right;width:90px;">Nett Price Total</td>
<%
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rsQu("DivisionId")) Then
%>
				<td class="HeaderRow" style="text-align:right;width:50px;">Margin</td>				
<%
	End If
%>
			</tr>
<%
	decRunningUnitCostTotal = 0
	decRunningNettPriceTotal = 0
	Do Until rsQu.EOF
		decRunningUnitCostTotal = decRunningUnitCostTotal + rsQu("UnitCostTotal")
		decRunningNettPriceTotal = decRunningNettPriceTotal + rsQu("NettPriceTotal")
%>
			<tr>
<%
		If intDivisionId = 0 Then
%>
				<td valign="top"><%= rsQu("DivisionCode") %></td>
<%
		End If
		If intCompanyId = 0 Then
%>
				<td valign="top"><b><%= rsQu("Company") %></b><br><%= rsQu("Reference") %></td>
<%
		End If
		If strCode = "All" Then
%>
				<td valign="top"><%= rsQu("Name") %></td>
<%
		End If
%>
				<td valign="top" style="width:70px;"><a href="#" onclick="ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsQu("Qid") %>);"><%= rsQu("Qid") %></a></td>
				<td valign="top" style="width:80px;" nowrap><%= FormatDateU(rsQu("QuoteDate"),False) %></td>
				<td valign="top" style="width:80px;"><%= rsQu("QuoteStatus") %></td>
<%
		If boolDivisionManager Then
%>
				<td valign="top" style="text-align:right;width:90px;"><%= FormatCurrency(rsQu("UnitCostTotal"),2) %></td>
<%
		End If
%>
				<td valign="top" style="text-align:right;width:90px;"><%= FormatCurrency(rsQu("NettPriceTotal"),2) %></td>
<%
		If boolDivisionManager Then
%>
				<td valign="top" style="text-align:right;width:50px;"><%= FormatNumber(rsQu("Margin"),2) %>%</td>
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
<%
		If boolDivisionManager Then
%>
				<td style="border-bottom:2px solid black;text-align:right;width:90px;"><%= FormatCurrency(decRunningUnitCostTotal,2) %></td>
<%
		End If
%>
				<td style="border-bottom:2px solid black;text-align:right;width:90px;"><%= FormatCurrency(decRunningNettPriceTotal,2) %></td>
<%
		If boolDivisionManager Then
%>
				<td style="border-bottom:2px solid black;text-align:right;width:50px;"><% If decRunningUnitCostTotal > 0 And decRunningNettPriceTotal > 0 Then Response.Write(FormatNumber(100*(1-(decRunningUnitCostTotal/decRunningNettPriceTotal)))) Else Response.Write("0.00") %>%</td>
<%
		End If
%>
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