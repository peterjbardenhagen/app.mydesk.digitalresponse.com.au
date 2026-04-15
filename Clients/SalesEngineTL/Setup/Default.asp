<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Setup /></span>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
<%

If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table2">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

%>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td>
									<br>
<%
If Request.Cookies("UserSettings")("UserTypeId") > 5 Then
%>
									<b>Administrator functions</b><br><br>
									<li><a href="../ActivityTypes">Activity Types</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../QuoteCOS">Conditions of Sale</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../CopyContacts">Copy Contacts</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../CurrencyRates">Currency Rates</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../Divisions">Divisions</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../ExpenseTypes">Expense Types</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../ExpenseTypeGroups">Expense Type Groups</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../FilesCategories">Files Categories</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../ImportData">Import Data</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../Locations">Locations</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="Maintenance.asp">Maintenance</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../PartCodes">Part Codes</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../SQLQuery">SQL Query</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../UserRoles">User Roles</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<br>
<%
End If
%>
									<b>Manager functions</b><br><br>
									<li><a href="../Companies">Companies</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									<li><a href="../Projects">Projects</a></li><br><img src="/Images/Spacer.gif" width=200 height=5 border=0 alt=""><br>
									</td>
								</tr>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->