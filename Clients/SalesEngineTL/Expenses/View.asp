<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngExpenseId
lngExpenseId = CLng(Request("ExpenseId"))

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

<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Expenses.*, ExpenseTypes.ExpenseType, Users.Name, Contacts_WithCustomersAndSuppliers_V2.CompanyName, Contacts_WithCustomersAndSuppliers_V2.FirstName, Contacts_WithCustomersAndSuppliers_V2.Surname FROM ExpenseTypes INNER JOIN (Users INNER JOIN (Contacts_WithCustomersAndSuppliers_V2 INNER JOIN Expenses ON Contacts_WithCustomersAndSuppliers_V2.ContactId = Expenses.ContactId) ON (Users.Code = Expenses.Code) AND (Users.Code = Contacts_WithCustomersAndSuppliers_V2.Code)) ON ExpenseTypes.ExpenseTypeId = Expenses.ExpenseTypeId WHERE Expenses.Eid = " & lngExpenseId
Set rs = dbConn.Execute(sql)

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Expenses</a> / View Expense /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<input type="hidden" name="ExpenseId" value="<%= rs("Eid") %>">
								<tr>
									<td valign="top">
										<table>
											<tr>
												<td valign="top" style="font-weight:bold;">Date Entered</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= FormatDateU(rs("DateEntered"), false) %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Expense Date</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= FormatDateU(rs("ExpenseDate"), false) %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Expense Type</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("ExpenseType") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Name</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Name") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Description</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Description") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Customer</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("CompanyName") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Cost Inc GST</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= FormatCurrency(rs("CostIncGST"), 2) %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">GST</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= FormatCurrency(rs("GST"), 2) %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Company Staff</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("FBTTTL") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Non Company Staff</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("FBTNon") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Corporate Card</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><% If rs("TTLCorporateCard") Then Response.Write("Yes") Else Response.Write("No") %></td>
											</tr>
<%
If rs("Comment") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Comment</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Comment") %></td>
											</tr>
<%
End If
%>
										</table>							
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
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
