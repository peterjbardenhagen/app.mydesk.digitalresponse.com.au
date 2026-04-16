<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("D", 1, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
strCode = Trim(Request("Code"))

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="JavaScript">

		var nDivisions = 0;

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
			if (emptyField(document.Form1.UserRoleId)) {
				alert("Please select a User Role.");
				validFlag = false;
				document.Form1.UserRoleId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.UserTypeId)) {
				alert("Please select a User Type.");
				validFlag = false;
				document.Form1.UserTypeId.focus();
			}}				

			if (validFlag) {
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please select a Primary Division.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.LocationId)) {
				alert("Please select a Location.");
				validFlag = false;
				document.Form1.LocationId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.LineManagerCode)) {
				alert("Please select a Line Manager.");
				validFlag = false;
				document.Form1.LineManagerCode.focus();
			}}
					
			if (validFlag) {
			if (emptyField(document.Form1.Name)) {
				alert("Please complete the Name field.");
				validFlag = false;
				document.Form1.Name.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Initials)) {
				alert("Please complete the Initials field.");
				validFlag = false;
				document.Form1.Initials.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Initials)) {
				alert("Please complete the Initials field.");
				validFlag = false;
				document.Form1.Initials.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Email)) {
				alert("Please complete the Email field.");
				validFlag = false;
				document.Form1.Email.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PW)) {
				alert("Please complete the Password field.");
				validFlag = false;
				document.Form1.PW.focus();
			}}

/*
			if (validFlag) {
			if (emptyField(document.Form1.DaysPerWeek)) {
				alert("Please select Days Per Week.");
				validFlag = false;
				document.Form1.DaysPerWeek.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.HoursPerDay)) {
				alert("Please enter Hours Per Day.");
				validFlag = false;
				document.Form1.HoursPerDay.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.HoursPerDay.value)) {
				alert("Please ensure Hours Per Day is a valid number.");
				validFlag = false;
				document.Form1.HoursPerDay.focus();
			}}	

			if (validFlag) {
			if (isNaN(document.Form1.ExpensesPerMonth.value)||document.Form1.ExpensesPerMonth.value.length==0) {
				alert("Please ensure Expenses Per Month is a valid number.");
				validFlag = false;
				document.Form1.ExpensesPerMonth.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.SalesBudget.value)||document.Form1.SalesBudget.value.length==0) {
				alert("Please ensure Sales Budget is a valid number.");
				validFlag = false;
				document.Form1.SalesBudget.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.ProspectsBudget.value)||document.Form1.ProspectsBudget.value.length==0) {
				alert("Please ensure Prospects Budget is a valid number.");
				validFlag = false;
				document.Form1.ProspectsBudget.focus();
			}}
*/

			if (!validFlag) {
				// Check that if permissions other than Visible is checked, and Visible is not checked that Visible is checked!
				var i = 1;
				while (i <= nDivisions){
					////alert('TESTING ' + i)
					if(!document.getElementById("Visible" + i).checked) {
						if(document.getElementById("Manager" + i).checked || document.getElementById("Quotes" + i).checked || document.getElementById("RFQ" + i).checked || document.getElementById("PurchaseOrders" + i).checked || document.getElementById("Payroll" + i).checked) {
							if(confirm('There are permissions setup for ' + document.getElementById("DivisionName" + i).value + ' and Visible is not checked.\n\nClick OK to set Visible to checked as well as the other permissions?\n\nClick Cancel to clear all permissions.')){
								document.getElementById("Visible" + i).checked = true;
							} else {
								//document.getElementById("Visible" + i).checked = true;
								document.getElementById("Visible" + i).checked = false;
								document.getElementById("MemberOf" + i).checked = false;
								document.getElementById("Manager" + i).checked = false;
								document.getElementById("Quotes" + i).checked = false;
								document.getElementById("RFQ" + i).checked = false;
								document.getElementById("PurchaseOrders" + i).checked = false;
								document.getElementById("Payroll" + i).checked = false;
							}
						}
					}
					i=i+1
				}
				

				// Check if there are divisions with Visible set at all
				var bHasVisible = false;
				i = 1;
				while (i <= nDivisions){
					if(document.getElementById("Visible" + i).checked) {
						bHasVisible = true;
					}
					i++
				}
				if(!bHasVisible) {
					if(!confirm('Do you realise that this user will not be able to view any data,\nas there are no permissions set for Visible at any Division?\n\nClick OK to proceed.\n\nClick Cancel to change this.')) {
						validFlag = false;
					}
				}
			}
		return validFlag;
		}

		</script>
		
		<style>
			.AccessRightsHdr {
				background-color: #cccccc;
				font-weight: bold;
				vertical-align: top;
				text-align: center;
				font-size:10px;
			}
			.AccessRightsRow {
				border-bottom: 1px solid black;
				text-align: center;
				font-size:10px;
			}
		</style>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/" class="Header2">Manage Users</a> / Add User /></span>
				<br/><br/>
				<li><a href="#" onclick="parent.ViewUserRoles('<%= Request.Cookies("ClientSettings")("WorkingDir") %>')">View User Roles</a></li>
				<br/><br/>
				<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
					<form method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/Add_Proc.asp" name="Form1" id="Form1" onSubmit="return checkForm();">
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>User Role</b></td>
						<td valign="top">
							<select name="UserRoleId" ID="Select4" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsU = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM UserRoles U WHERE UserRoleId ORDER BY UserRole"
Set rsU = dbConn.Execute(sql)

If Not(rsU.BOF And rsU.EOF) Then
	Do Until rsU.EOF
		Response.Write ("								<option value=""" & rsU("UserRoleId") & """>" & rsU("UserRole") & "</option>" & vbNewLine)
		rsU.MoveNext
	Loop
End If

If IsObject(rsU) Then
	rsU.Close
	Set rsU = Nothing
End If

%>
									</select>			
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>User Type</b></td>
						<td valign="top">
							<select name="UserTypeId" ID="Select3" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsU = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM UserTypes U "
If Request.Cookies("UserSettings")("UserTypeId") <= 5 Then
	sql = sql & "WHERE UserTypeId <= " & Request.Cookies("UserSettings")("UserTypeId")
End If
sql = sql & " ORDER BY InOrder"
Set rsU = dbConn.Execute(sql)

If Not(rsU.BOF And rsU.EOF) Then
	Do Until rsU.EOF
		Response.Write ("								<option value=""" & rsU("UserTypeId") & """>" & rsU("UserType") & "</option>" & vbNewLine)
		rsU.MoveNext
	Loop
End If

If IsObject(rsU) Then
	rsU.Close
	Set rsU = Nothing
End If

%>
									</select>			
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>Primary Division</b></td>
						<td valign="top">
							<select name="DivisionId" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		rsDiv.MoveNext
	Loop
End If

If IsObject(rsDiv) Then
	rsDiv.Close
	Set rsDiv = Nothing
End If

%>
							</select>			
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>Location</b></td>
						<td valign="top">
							<select name="LocationId" ID="Select2" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Locations INNER JOIN States ON States.StateId = Locations.StateId ORDER BY Company, States.State"
Set rsLoc = dbConn.Execute(sql)

If Not(rsLoc.BOF And rsLoc.EOF) Then
	Do Until rsLoc.EOF
		Response.Write ("								<option value=""" & rsLoc("LocationId") & """>" & rsLoc("Company") & " - " & rsLoc("State") & "</option>" & vbNewLine)
		rsLoc.MoveNext
	Loop
End If

If IsObject(rsLoc) Then
	rsLoc.Close
	Set rsLoc = Nothing
End If

%>
							</select>			
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top" style="font-weight:bold;">Line Manager</td>
						<td valign="top">
						<select name="LineManagerCode" ID="Select1" style="width:280px;">
							<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>

<%
Set rsUsers = Server.CreateObject("ADODB.RecordSet")
If Request.Cookies("UserSettings")("UserTypeId") => 5 Then
	sql = "Select * From Users Where Deleted = 0 AND Active = True ORDER BY Name"
Else
	sql = "Select * From Users Where Deleted = 0 AND Active = True AND Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ") ORDER BY Name"
End If
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	Do Until rsUsers.EOF
		If CStr(Trim(rsUsers("Code"))) = CStr(Trim(Request.Cookies("UserSettings")("Code"))) Then
%>
							<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") & " - " & rsUsers("Position") %></option>
<%
		Else
%>
							<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") & " - " & rsUsers("Position") %></option>
<%
		End If
		rsUsers.MoveNext
	Loop
End If

If IsObject(rsUsers) Then
	rsUsers.Close
	Set rsUsers = Nothing
End If
%>
						</select>
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>Name</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Name" id="Name"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Initials</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Initials" id="Initials"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Position</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Position" id="Text5"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Phone</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Phone" id="Text7"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Mobile</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Mobile" id="Text8"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top" style="font-weight:bold;">Email</td>
						<td valign="top"><input type="text" name="Email" style="width:280px;" maxlength=50 ID="Text1"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Password</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="PW" id="PW"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td valign="top"><b>Active</b></td>
						<td valign="top">
							<input type="radio" name="Active" id="Active" value="-1" checked> Yes<br/>
							<input type="radio" name="Active" id="Active" value="0"> No
						</td>
					</tr>
<!--
					<tr>
						<td valign="top"></td>
						<td valign="top"><b>Requires Timesheet</b></td>
						<td valign="top">
							<input type="radio" name="ReqTimesheet" id="Radio1" value="-1" /> Yes<br/>
							<input type="radio" name="ReqTimesheet" id="Radio2" value="0" checked /> No
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Days Per Week</b></td>
						<td valign="top">
						<select name="DaysPerWeek">
							<option value=""></option>
							<option value="1">1</option>
							<option value="2">2</option>
							<option value="3">3</option>
							<option value="4">4</option>
							<option value="5">5</option>
							<option value="6">6</option>
							<option value="7">7</option>
						</select>
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Hours Per Day</b></td>
						<td valign="top"><input size=20 maxlength=2 type="text" name="HoursPerDay" id="Text3"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Expenses Budget Per Month</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="ExpensesPerMonth" id="Text2" value=0></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Sales Budget Per Month</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="SalesBudget" id="Text4" value=0></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Prospects Budget Per Month</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="ProspectsBudget" id="Text6" value=0></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
-->
					<tr>
						<td colspan=3 valign="top">
							<a name="Permissions"></a>
							<br><strong>Permissions</strong><br><img src="/Images/Spacer.gif" width=10 height=5 border=0 alt=""><br>
							<table width="100%" cellpadding=3 cellspacing=0>
								<tr>
									<td class="AccessRightsHdr" style="text-align:left;">Division</td>
									<td class="AccessRightsHdr" width=60>Visible</td>
									<td class="AccessRightsHdr" width=60>Member Of</td>
									<td class="AccessRightsHdr" width=60>Manager</td>
									<td class="AccessRightsHdr" width=60>Quotes</td>
									<td class="AccessRightsHdr" width=60>RFQ</td>
									<td class="AccessRightsHdr" width=60>Purchase Orders</td>
									<td class="AccessRightsHdr" width=60>Payroll</td>
								</tr>
<%

Dim i
i = 1

Set rsD = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Order By Division"
Set rsD = dbConn.Execute(sql)

Do Until rsD.EOF

%>
								<tr>
									<td class="AccessRightsRow" style="text-align:left;"><%= i %>. <a href="#Permissions" onclick="alert('<%= rsD("Division") %>');"><%= rsD("DivisionCode") %></a><input type="hidden" name="DivisionName<%= i %>" id="DivisionName<%= i %>" value="<%= rsD("Division") %>"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Visible<%= rsD("DivisionId") %>" id="Visible<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="MemberOf<%= rsD("DivisionId") %>" id="MemberOf<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Manager<%= rsD("DivisionId") %>" id="Manager<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Quotes<%= rsD("DivisionId") %>" id="Quotes<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="RFQ<%= rsD("DivisionId") %>" id="RFQ<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="PurchaseOrders<%= rsD("DivisionId") %>" id="PurchaseOrders<%= i %>" value=1 style="border:0px 0px 0px 0px;"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Payroll<%= rsD("DivisionId") %>" id="Payroll<%= i %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox1"></td>
								</tr>
<%

	i = i + 1
	rsD.MoveNext
Loop

rsD.Close
Set rsD = Nothing

%>
							</table>
						</td>
					</tr>
					<tr>
						<td colspan=3 align="right"><input type="button" value="Cancel" onclick="document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/';" ID="Button1" NAME="Button1"> <input type="submit" value="Submit" id="Submit"></td>
					</tr>
					</form>
				</table>
			</td>
		</tr>
	</table>
<script language="javascript">
	nDivisions = <%= i-1 %>;
</script>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
