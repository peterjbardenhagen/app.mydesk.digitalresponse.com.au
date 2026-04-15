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
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true

			if (validFlag) {
			if (!emptyField(document.Form1.AcceptedDate) && !emptyField(document.Form1.RejectedDate)) {
				alert("A Sales Project cannot have dates for being accepted and rejected.");
				validFlag = false;
				document.Form1.AcceptedDate.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select a Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Project)) {
				alert("Please complete the Project field.");
				validFlag = false;
				document.Form1.Project.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Product)) {
				alert("Please complete the Product/Service field.");
				validFlag = false;
				document.Form1.Product.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Value)) {
				if (isNaN(document.Form1.Value.value)) {
					alert("Please enter a valid number for the Value field.");
					validFlag = false;
					document.Form1.Value.focus();
				}
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.AmountPerMonth)) {
				if (isNaN(document.Form1.AmountPerMonth.value)) {
					alert("Please enter a valid number for the Amount Per Month field.");
					validFlag = false;
					document.Form1.AmountPerMonth.focus();
				}
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.NumberOfMonths)) {
				if (isNaN(document.Form1.NumberOfMonths.value)) {
					alert("Please enter a valid number for the Expected Number of Months field.");
					validFlag = false;
					document.Form1.NumberOfMonths.focus();
				}
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PotentialOrderDate)) {
				alert("Please complete the Potential Order Date field.");
				validFlag = false;
				document.Form1.PotentialOrderDate.focus();
			}}
			return validFlag
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select SalesProjects.*, Contacts_WithCustomers.*, Contacts_WithCustomers.Company As CompanyName From SalesProjects Inner Join Contacts_WithCustomers On Contacts_WithCustomers.ContactId = SalesProjects.ContactId Where SalesProjectId = " & lngSalesProjectId
Set rs = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("Manager") Or rs("Code") = Request.Cookies("UserSettings")("Code") Then

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Sales Projects</a> / Edit Sales Project /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="SalesProjectId" value="<%= rs("SalesProjectId") %>">
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Prospect Date</td>
									<td valign="top"><input type="input" value="<% If FormatDateU(rs("ProspectDate"), False) <> "01-Jan-2001" Then Response.Write(FormatDateU(rs("ProspectDate"), False)) %>" name="ProspectDate" readonly ID="Input2"> <a href="javascript:showCal('Calendar12')"><img src="/Images/Calendar.gif" border=0></a>&nbsp;&nbsp;&nbsp;&nbsp;<input type="button" value="Clear" onclick="document.Form1.ProspectDate.value = '';" ID="Button3" NAME="Button3"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Tender/Quote Date</td>
									<td valign="top"><input type="input" value="<% If FormatDateU(rs("TenderDate"), False) <> "01-Jan-2001" Then Response.Write(FormatDateU(rs("TenderDate"), False)) %>" name="TenderDate" readonly ID="Input3"> <a href="javascript:showCal('Calendar13')"><img src="/Images/Calendar.gif" border=0></a>&nbsp;&nbsp;&nbsp;&nbsp;<input type="button" value="Clear" onclick="document.Form1.TenderDate.value = '';" ID="Button2" NAME="Button2"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Accepted Date</td>
									<td valign="top"><input type="input" value="<% If FormatDateU(rs("AcceptedDate"), False) <> "01-Jan-2001" Then Response.Write(FormatDateU(rs("AcceptedDate"), False)) %>" name="AcceptedDate" readonly ID="Input4"> <a href="javascript:showCal('Calendar14')"><img src="/Images/Calendar.gif" border=0></a>&nbsp;&nbsp;&nbsp;&nbsp;<input type="button" value="Clear" onclick="document.Form1.AcceptedDate.value = '';" ID="Button1" NAME="Button1"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Rejected Date</td>
									<td valign="top"><input type="input" value="<% If FormatDateU(rs("RejectedDate"), False) <> "01-Jan-2001" Then Response.Write(FormatDateU(rs("RejectedDate"), False)) %>" name="RejectedDate" readonly ID="Input5"> <a href="javascript:showCal('Calendar15')"><img src="/Images/Calendar.gif" border=0></a>&nbsp;&nbsp;&nbsp;&nbsp;<input type="button" value="Clear" onclick="document.Form1.RejectedDate.value = '';" ID="Button4" NAME="Button4"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Contact</td>
									<td valign="top">
									<select name="ContactId" ID="Select1" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
										<option selected value="<%= rs("ContactId") %>"><%= rs("CompanyName") %> - <%= rs("Surname") %>, <%= rs("FirstName") %></option>
										<option value=""> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>

<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT *, Contacts_WithCustomers.Company As CompanyName FROM Contacts_WithCustomers WHERE Code = '" & rs("Code") & "' ORDER BY Company, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
										<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
									</select>
									<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Customer');">Create New Contact</a>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Project</td>
									<td valign="top"><input type="text" name="Project" style="width:280px;" ID="Text2" value="<%= rs("Project") %>" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Product/Service</td>
									<td valign="top">
									<textarea name="Product" id="Product" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Product") %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<%= 500 - Len(rs("Product")) %>" readonly ID="Text1">
									</td>								
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>One Off Sales Project</b></td>
									<td valign="top">
										<input type="radio" name="OneOffSalesProject" value="-1" ID="Radio1" <% If rs("OneOffSalesProject") = -1 Then Response.Write("checked") %>> Yes<br/>
										<input type="radio" name="OneOffSalesProject" value="0" ID="Radio2" <% If rs("OneOffSalesProject") = 0 Then Response.Write("checked") %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">If one off then<br>Value</td>
									<td valign="top">$<input type="text" size=20 name="Value" id="Text4" maxlength=10 value="<%= rs("Value") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">If ongoing then<br>Amount Per Month</td>
									<td valign="top">$<input type="text" size=20 name="AmountPerMonth" id="Text5" maxlength=10 value="<%= rs("AmountPerMonth") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">If ongoing then<br>Expected Number of Months</td>
									<td valign="top"><input type="text" size=4 name="NumberOfMonths" id="Text6" maxlength=8 value="<%= rs("NumberOfMonths") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Potential Order Date</td>
									<td valign="top"><input type="input" name="PotentialOrderDate" readonly ID="Input1" value="<%= FormatDateU(rs("PotentialOrderDate"), False) %>"> <a href="javascript:showCal('Calendar8')"><img src="/Images/Calendar.gif" border=0></a></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><span style="font-weight:bold;">Comment</td>
									<td valign="top">
									<textarea name="Comment" id="Comment" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Comment") %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<%= 500 - Len(rs("Comment")) %>" readonly ID="Text3">
									</td>
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<%

End If

%>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
