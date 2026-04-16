<% 

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

Dim intUserId
intUserId = Trim(Request("UserId"))

Dim strWorkingDir
strWorkingDir = ""
On Error Resume Next
If Not Request.Cookies("ClientSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) And Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
		strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
	End If
End If
If Err.Number <> 0 Or strWorkingDir = "" Then
	strWorkingDir = "/Clients/SalesEngineTL"
End If
On Error GoTo 0

%>
<!--#include virtual="/System/ssi_ResponseHeaders.inc"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<html lang="en">
	<head>
		<title>Edit User - Techlight MyDesk</title>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<link rel="icon" type="image/x-icon" href="/favicon.ico">
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
		return validFlag 
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
	<body class="tl-bg-light">

<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/" class="Header2">Manage Users</a> / Edit User /></span>
				<br/><br/>
				<li><a href="#" onclick="parent.ViewUserRoles('<%= Request.Cookies("ClientSettings")("WorkingDir") %>')">View User Roles</a></li>
				<br/><br/>
<%

Dim rs
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND UserId = " & intUserId & " AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ")"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
				<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
					<form method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/Edit_Proc.asp" name="Form1" id="Form1" onSubmit="return checkForm();">
					<input type="hidden" name="UserId" id="UserId" value="<%= intUserId %>">
					<tr>
						<td valign="top" class="Req">*</td>
						<td width=100 valign="top"><b>User Role</b></td>
						<td valign="top">
							<select name="UserRoleId" ID="Select6" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsU = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM UserRoles U WHERE UserRoleId ORDER BY UserRole"
Set rsU = dbConn.Execute(sql)

If Not(rsU.BOF And rsU.EOF) Then
	Do Until rsU.EOF
		If CLng(rsU("UserRoleId")) = CLng(rs("UserRoleId")) Then
			Response.Write ("								<option selected value=""" & rsU("UserRoleId") & """>" & rsU("UserRole") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsU("UserRoleId") & """>" & rsU("UserRole") & "</option>" & vbNewLine)
		End If
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
sql = "SELECT * FROM UserTypes "
If Request.Cookies("UserSettings")("UserTypeId") <= 5 Then
	sql = sql & " WHERE UserTypeId <= " & Request.Cookies("UserSettings")("UserTypeId")
End If
sql = sql & " ORDER BY InOrder"
Set rsU = dbConn.Execute(sql)

If Not(rsU.BOF And rsU.EOF) Then
	Do Until rsU.EOF
		If CLng(rsU("UserTypeId")) = CLng(rs("UserTypeId")) Then
			Response.Write ("								<option selected value=""" & rsU("UserTypeId") & """>" & rsU("UserType") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsU("UserTypeId") & """>" & rsU("UserType") & "</option>" & vbNewLine)
		End If
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
							<select name="DivisionId" style="width:280px;" ID="Select2">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CLng(rsDiv("DivisionId")) = CLng(rs("DivisionId")) Then
			Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		End If
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
							<select name="LocationId" ID="Select5" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Locations INNER JOIN States ON States.StateId = Locations.StateId ORDER BY Company, States.State"
Set rsLoc = dbConn.Execute(sql)

If Not(rsLoc.BOF And rsLoc.EOF) Then
	Do Until rsLoc.EOF
		If CLng(rsLoc("LocationId")) = CLng(rs("LocationId")) Then
			Response.Write ("								<option selected value=""" & rsLoc("LocationId") & """>" & rsLoc("Company") & " - " & rsLoc("State") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsLoc("LocationId") & """>" & rsLoc("Company") & " - " & rsLoc("State") & "</option>" & vbNewLine)
		End If
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

					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top" style="font-weight:bold;">Line Manager</td>
						<td valign="top">
						<select name="LineManagerCode" ID="Select4" style="width:280px;">
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
		If CStr(Trim(rsUsers("Code"))) = CStr(Trim(rs("LineManagerCode")&"")) Then
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
						<td valign="top"><input size=50 maxlength=500 type="text" name="Name" id="Name" value="<%= rs("Name") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Initials</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Initials" id="Initials" value="<%= rs("Initials") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Position</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Position" id="Text5" value="<%= rs("Position") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Phone</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Phone" id="Text7" value="<%= rs("Phone") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td width=100 valign="top"><b>Mobile</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Mobile" id="Text8" value="<%= rs("Mobile") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top" style="font-weight:bold;">Email</td>
						<td valign="top"><input type="text" name="Email" style="width:280px;" maxlength=50 ID="Text1" value="<%= rs("Email") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Password</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="PW" id="PW" value="<%= rs("PW") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td valign="top"><b>Active</b></td>
						<td valign="top">
							<input type="radio" name="Active" id="Active" value="-1" <% If rs("Active") = -1 Then Response.Write("Checked") End If %>> Yes<br/>
							<input type="radio" name="Active" id="Active" value="0" <% If rs("Active") = 0 Then Response.Write("Checked") End If %>> No
						</td>
					</tr>
					<tr>
						<td valign="top"></td>
						<td valign="top"><b>Requires Timesheet</b></td>
						<td valign="top">
							<input type="radio" name="ReqTimesheet" id="Radio1" value="-1" <% If rs("RequiresTimesheet") = -1 Then Response.Write("Checked") End If %>> Yes<br/>
							<input type="radio" name="ReqTimesheet" id="Radio2" value="0" <% If rs("RequiresTimesheet") = 0 Then Response.Write("Checked") End If %>> No
						</td>
					</tr>
<!--
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Days Per Week</b></td>
						<td valign="top">
						<select name="DaysPerWeek" ID="Select1">
							<option value=""></option>
<%
Dim i
i = 0
Do Until i = 7
	i=i+1
	If CLng(i) = CLng(rs("DaysPerWeek")) Then
		Response.Write("							<option selected value=""" & i & """>" & i & "</option>" & vbNewLine)
	Else
		Response.Write("							<option value=""" & i & """>" & i & "</option>" & vbNewLine)
	End If
Loop
%>
						</select>
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Hours Per Day</b></td>
						<td valign="top"><input size=20 maxlength=2 type="text" name="HoursPerDay" id="Text3" value="<%= rs("HoursPerDay") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Expenses Per Month</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="ExpensesPerMonth" id="Text2" value="<%= rs("ExpensesPerMonth") %>"></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Sales Budget</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="SalesBudget" id="Text4" value=<%= rs("SalesBudget") %>></td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Prospects Budget</b></td>
						<td valign="top">$<input size=20 maxlength=9 type="text" name="ProspectsBudget" id="Text6" value=<%= rs("ProspectsBudget") %>></td>
					</tr>
					-->
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td colspan=3 valign="top">
							<a name="Permissions"></a>
							<br><strong>Permissions</strong><br><img src="/Images/Spacer.gif" width=10 height=5 border=0 alt="">
							<table width="100%" cellpadding=3 cellspacing=0 ID="Table3">
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
Set rsD = Server.CreateObject("ADODB.RecordSet")
If Request.Cookies("UserSettings")("UserTypeId") <> 6 Then
	sql = "Select * From Divisions Where DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Order By Division"
Else
	sql = "Select * From Divisions Order By Division"
End If
Set rsD = dbConn.Execute(sql)

i = 1
Do Until rsD.EOF
	Set rsUA = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From UsersAccess Where DivisionId = " & rsD("DivisionId") & " And UserId = " & intUserId
	Set rsUA = dbConn.Execute(sql)
	If Not(rsUA.BOF And rsUA.EOF) Then
%>
								<tr>
									<td class="AccessRightsRow" style="text-align:left;"><%= i %>. <a href="#Permissions" onclick="alert('<%= rsD("Division") %>');"><%= rsD("DivisionCode") %></a><input type="hidden" name="DivisionName<%= i %>" id="DivisionName<%= i %>" value="<%= rsD("Division") %>"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Visible<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox1" <% If rsUA("Visible") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="MemberOf<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox1" <% If rsUA("MemberOf") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Manager<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox2" <% If rsUA("Manager") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Quotes<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox3" <% If rsUA("Quotes") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="RFQ<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox4" <% If rsUA("RFQ") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="PurchaseOrders<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox5" <% If rsUA("PurchaseOrders") Then Response.Write("checked") %>></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Payroll<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox7" <% If rsUA("Payroll") Then Response.Write("checked") %>></td>
								</tr>
<%
	Else
%>
								<tr>
									<td class="AccessRightsRow" style="text-align:left;"><%= i %>. <a href="#Permissions" onclick="alert('<%= rsD("Division") %>');"><%= rsD("DivisionCode") %></a><input type="hidden" name="DivisionName<%= i %>" id="Hidden1" value="<%= rsD("Division") %>"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Visible<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox8"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="MemberOf<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox1"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Manager<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox9"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Quotes<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox10"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="RFQ<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox11"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="PurchaseOrders<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox12"></td>
									<td class="AccessRightsRow"><input type="checkbox" name="Payroll<%= rsD("DivisionId") %>" value=1 style="border:0px 0px 0px 0px;" ID="Checkbox13"></td>
								</tr>
<%
	End If
	rsUA.Close
	Set rsUA = Nothing
	i = i + 1
	rsD.MoveNext
Loop

rsD.Close
Set rsD = Nothing

%>
							</table>
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td colspan=3 align="right"><input type="button" value="Cancel" onclick="document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/';"> <input type="submit" value="Submit" id="Submit"></td>
					</tr>
					</form>
				</table>
<%

Else
	Response.Redirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Portal/AccessDenied.asp")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
			</td>
		</tr>
	</table>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
