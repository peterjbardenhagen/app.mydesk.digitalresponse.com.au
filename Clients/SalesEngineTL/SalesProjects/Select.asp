<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim lngSalesProjectId

strCode = Trim(Request("Code"))
lngSalesProjectId = CLng(Request("SalesProjectId"))

If strCode = "" Then strCode = Request.Cookies("UserSettings")("Code")

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
		<script language="javascript">
			function SetSalesProjectId(Id) {
				try {
					window.opener.document.parentWindow.MainFrame.document.Form1.SalesProjectId.value = Id;
				} catch(e) {
				}
				window.close();
			}
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">Sales Projects</span><br><br>
				</td>
			</tr>
		</table>
		<table ID="Table5">
			<form method="get" name="Form1" ID="Form1">
			<tr>
				<td style="font-weight:bold;">Select user&nbsp;&nbsp;&nbsp;</td>
				<td>
					<select name="Code" ID="Select4" onchange="document.location.href='Select.asp?Code='+document.Form1.Code.value;">
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = strCode Then
%>
						<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			Else
%>
						<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			End If	
			rsUsers.MoveNext
		Loop
	End If

	rsUsers.Close
	Set rsUsers = Nothing

%>
					</select>
				</td>
			</tr>
			</form>
		</table><br>
<%
	If lngSalesProjectId > 0 Then
		sql = "SELECT iif(SalesProjects.AcceptedDate > #01-Jan-01#, 'Accepted', iif(SalesProjects.RejectedDate > #01-Jan-01#, 'Rejected', iif(SalesProjects.ProspectDate > #01-Jan-01#, 'Prospect', iif(SalesProjects.TenderDate > #01-Jan-01#, 'Tender', 'In Progress')))) As [Status], SalesProjects.*, Users.Name, Contacts_WithCustomers.* FROM (SalesProjects INNER JOIN Users ON Users.Code = SalesProjects.Code) Inner Join Contacts_WithCustomers On Contacts_WithCustomers.ContactId = SalesProjects.ContactId Where SalesProjectId = " & lngSalesProjectId
		Set rs = dbConn.Execute(sql)
		
		If Not(rs.BOF And rs.EOF) Then
%>
		<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table6">
			<tr>
				<td colspan=10><b>Currently selected sales project:</b><br></td>
			</tr>
			<tr>
				<td>
					<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table7">
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
					<br>
				</td>
			</tr>
		</table>
<%
		Else
			Response.Write("Selected sales project no longer exists")
		End If
		
		rs.Close
		Set rs = Nothing
	End If
%>

<%
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT iif(SalesProjects.AcceptedDate > #01-Jan-01#, 'Accepted', iif(SalesProjects.RejectedDate > #01-Jan-01#, 'Rejected', iif(SalesProjects.ProspectDate > #01-Jan-01#, 'Prospect', iif(SalesProjects.TenderDate > #01-Jan-01#, 'Tender', 'In Progress')))) As [Status], SalesProjects.*, Users.Name, Contacts_WithCustomers.* FROM (SalesProjects INNER JOIN Users ON Users.Code = SalesProjects.Code) Inner Join Contacts_WithCustomers On Contacts_WithCustomers.ContactId = SalesProjects.ContactId WHERE "
	If strCode <> "All" Then
		sql = sql & "SalesProjects.Code = '" & strCode & "' AND "
	End If
	sql = sql & " Not(SalesProjects.RejectedDate > #01-Jan-01#) AND Users.Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") ORDER BY SalesProjects.[DateEntered] DESC"
	Set rs = dbConn.Execute(sql)

	If Not(rs.BOF And rs.EOF) Then
%>
		<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table2">
			<tr>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;">Action</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;width:80px;">Date Entered</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;">Status</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;width:120px;">Name</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;">Customer</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;">Project</td>
				<td style="background-color:#eeeeee;vertical-align:top;font-size:12px;font-weight:bold;width:150px;">Value</td>
			</tr>
<%
		Do Until rs.EOF
%>
			<tr>
				<td style="vertical-align:top;font-size:12px;"><input type="button" value="Select" onclick="SetSalesProjectId(<%= rs("SalesProjectId") %>)"></td>
				<td style="vertical-align:top;font-size:12px;"><%= FormatDateU(rs("DateEntered"), False) %></td>
				<td style="vertical-align:top;font-size:12px;"><%= rs("Status") %></td>
				<td style="vertical-align:top;font-size:12px;"><%= rs("Name") %></td>
				<td style="vertical-align:top;font-size:12px;"><%= rs("CompanyName") %> - <%= rs("Surname") & ", " & rs("FirstName") %></td>
				<td style="vertical-align:top;font-size:12px;"><%= rs("Project") %><br><%= rs("Product") %></td>
<%
			If rs("OneOffSalesProject") Then
%>
				<td style="vertical-align:top;font-size:12px;"><% If IsNumeric(rs("Value")) Then Response.Write(FormatCurrency(rs("Value"), 2)) %></td>
<%
			Else
%>
				<td style="vertical-align:top;font-size:12px;"><% If IsNumeric(rs("AmountPerMonth")) Then Response.Write(FormatCurrency(rs("AmountPerMonth"), 2)) %> per month</td>
<%
			End If
%>
			</tr>
			<tr height=2>
				<td colspan=8>
					<table width="100%" height=2 cellpadding=0 cellspacing=0 border=0 ID="Table8">
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
