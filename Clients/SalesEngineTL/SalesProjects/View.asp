<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngSalesProjectId
lngSalesProjectId = CLng(Request("SalesProjectId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select iif(SalesProjects.AcceptedDate > #01-Jan-01#, 'Accepted', iif(SalesProjects.RejectedDate > #01-Jan-01#, 'Rejected', iif(SalesProjects.ProspectDate > #01-Jan-01#, 'Prospect', iif(SalesProjects.TenderDate > #01-Jan-01#, 'Tender', 'In Progress')))) As [Status], SalesProjects.*, Users.*, Contacts_WithCustomers.*, Contacts_WithCustomers.Company As CompanyName From (SalesProjects Inner Join Users On Users.Code = SalesProjects.Code) Inner Join Contacts_WithCustomers On Contacts_WithCustomers.ContactId = SalesProjects.ContactId Where SalesProjectId = " & lngSalesProjectId
Set rs = dbConn.Execute(sql)

%>

<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
<%
If (rs.BOF And rs.EOF) Then
%>
	<script language="javascript">
		alert("Sales Project has been removed.");
		window.close();
	</script>
<%
Else
%>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<span class="TimesHeader">Sales Project</span><br>
		<br>
		<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table5">
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Status</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("Status") %></td>
			</tr>
<%
	If CDate(rs("ProspectDate")) <> "1/01/2001" Then
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Prospect Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("ProspectDate"), False) %></td>
			</tr>
<%
	End If
	If CDate(rs("TenderDate")) <> "1/01/2001" Then
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Tender/Quote Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("TenderDate"), False) %></td>
			</tr>
<%
	End If
	If CDate(rs("AcceptedDate")) <> "1/01/2001" Then
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Accepted Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("AcceptedDate"), False) %></td>
			</tr>
<%
	End If
	If CDate(rs("RejectedDate")) <> "1/01/2001" Then
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Rejected Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("RejectedDate"), False) %></td>
			</tr>
<%
	End If
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Sales Representative</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("Name") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Contact</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("CompanyName") %> - <%= rs("Surname") & ", " & rs("FirstName") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("DateEntered"), False) %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Project</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("Project") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Product/Service</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("Product") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>One Off Sales Project</td>
				<td>&nbsp;</td>
				<td valign="top"><%= Replace(Replace(rs("OneOffSalesProject"), True, "Yes"), False, "No") %></td>
			</tr>
<%
	If Not rs("OneOffSalesProject") Then
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Amount Per Month</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatCurrency(rs("AmountPerMonth"),2) %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Expected Number of Months</td>
				<td>&nbsp;</td>
				<td valign="top"><%= rs("NumberOfMonths") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Total Value</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatCurrency(rs("AmountPerMonth")*rs("NumberOfMonths"),2) %></td>
			</tr>
<%
	Else
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Value</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatCurrency(rs("Value"),2) %></td>
			</tr>
<%
	End If
%>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Potential Order Date</td>
				<td>&nbsp;</td>
				<td valign="top"><%= FormatDateU(rs("PotentialOrderDate"), False) %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" width=165>Comment</td>
				<td>&nbsp;</td>
				<td valign="top"><%= Replace(rs("Comment"), Chr(10), "<br>") %></td>
			</tr>
		</table>
<%
End If
If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->