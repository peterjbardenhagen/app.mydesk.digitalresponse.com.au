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
Dim strCompany

strCode =		Trim(Request.Form("Code"))
dteDateFrom =	Request.Form("DateFrom")
dteDateTo =		Request.Form("DateTo")
strCompany =	Trim(Request.Form("Company"))

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
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Or Request.Cookies("UserSettings")("UserTypeId") = 4 Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
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
				<td valign="top"><span class="TimesHeader">MyTimesheets : </span><span style="font-size:18px;"><%= strName %></span><br><br>
				For <% If strCompany = "All" Then Response.Write("All companies") Else Response.Write(strCompany) %><br>
				Between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %>
				</td>
			</tr>
		</table>
		
<%
Set rs = Server.CreateObject("ADODB.RecordSet")

sql = "SELECT Expenses.*, Users.Name FROM Expenses INNER JOIN Users ON Users.Code = Expenses.Code WHERE "
If strCode <> "All" Then
	sql = sql & "Expenses.Code = '" & strCode & "' AND "
End If
If intCompanyId <> 0 Then
	sql = sql & "Expenses.CompanyId = " & intCompanyId & " AND "
End If
sql = sql & "(Expenses.DateEntered >= #" & DBDate(dteDateFrom) & "# AND Expenses.DateEntered < #" & DBDate(dteDateTo) & "#) ORDER BY Expenses.[DateEntered] DESC"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>

		<br>
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
			<tr>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;" nowrap>Expense Date</td>
<%
	If strCode = "All" Then
%>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;" nowrap>Sales Rep</td>
<%
	End If
	If strCompany = "All" Then
%>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;" nowrap>Customer</td>
<%
	End If
%>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;">Description</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;text-align:right;">Cost Inc GST</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;text-align:right;">GST</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;text-align:right;">FBT Company Staff</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;text-align:right;">FBT Non Company Staff</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;">Receipt</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;">Reimbursement</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:right;vertical-align:top;">Corporate Card</td>
				<th style="font-weight:bold;background-color:#ebeadb;color:black;text-align:left;vertical-align:top;">Comment</td>
			</tr>
<%
	Do Until rs.EOF
%>
			<tr height=1>
				<td colspan=15>
					<table width="100%" height=1 cellpadding=0 cellspacing=0 border=0>
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
			<tr>
				<td style="vertical-align:top;" nowrap><%= FormatDateU(rs("DateEntered"), False) %></td>
<%
	If strCode = "All" Then
%>
				<td style="vertical-align:top;" nowrap><%= rs("Name") %></td>
<%
	End If
	If strCompany = "All" Then
%>
				<td style="vertical-align:top;"><%= rs("Customer") %></td>
<%
	End If
%>
				<td style="vertical-align:top;"><%= rs("Description") %></td>
				<td style="vertical-align:top;text-align:right;"><% If IsNumeric(rs("CostIncGST")) Then Response.Write(FormatCurrency(rs("CostIncGST"), 2)) %></td>
				<td style="vertical-align:top;text-align:right;"><% If IsNumeric(rs("GST")) Then Response.Write(FormatCurrency(rs("GST"), 2)) %></td>
				<td style="vertical-align:top;text-align:right;"><%= rs("FBTTTL") %></td>
				<td style="vertical-align:top;text-align:right;"><%= rs("FBTNon") %></td>
				<td style="vertical-align:top;text-align:right;"><%= rs("Receipt") %></td>
				<td style="vertical-align:top;text-align:right;"><%= rs("Reimbursement") %></td>
				<td style="vertical-align:top;text-align:right;"><%= rs("TTLCorporateCard") %></td>
				<td style="vertical-align:top;"><%= Replace(rs("Comment"), Chr(10), "<br>") %></td>
			</tr>
<%
		rs.MoveNext
	Loop	
%>
		</table>
<%
Else
	Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no Expenses</td></tr></table>")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
