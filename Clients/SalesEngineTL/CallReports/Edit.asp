<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngCallReportId
lngCallReportId = CLng(Request("CallReportId"))

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
			if (emptyField(document.Form1.CallReportTypeId)) {
				alert("Please select a Call Report Type.");
				validFlag = false;
				document.Form1.CallReportTypeId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.DateEntered)) {
				alert("Please select a Date of Call.");
				validFlag = false;
				document.Form1.DateEntered.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select a Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.CallPurpose)) {
				alert("Please complete the Call Purpose field.");
				validFlag = false;
				document.Form1.CallPurpose.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Comment)) {
				alert("Please complete the Comment field.");
				validFlag = false;
				document.Form1.Comment.focus();
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
sql = "Select CallReports.*, Contacts_WithCustomersAndSuppliers_V2.* From CallReports Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = CallReports.ContactId Where CallReportId = " & lngCallReportId
Set rs = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("Manager") Or rs("Code") = Request.Cookies("UserSettings")("Code") Then

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Call Reports</a> / Edit Call Report /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/CallReports/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="CallReportId" value="<%= rs("CallReportId") %>">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Sales Project</td>
									<td valign="top">
										<input type="text" name="SalesProjectId" value="<% If rs("SalesProjectId") = "" Then Response.Write(rs("SalesProjectId")) Else Response.Write(0) %>" style="width:50px;" readonly ID="Text3">
										<a href="#" onclick="parent.SelectSalesProject('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', document.Form1.SalesProjectId.value);">Select Sales Project</a>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Call Report Type</td>
									<td valign="top">
									<select name="CallReportTypeId" ID="Select1" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsA = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM CallReportTypes WHERE Visible = 1 ORDER BY CallReportType"
Set rsA = dbConn.Execute(sql)

If Not(rsA.BOF And rsA.EOF) Then
	Do Until rsA.EOF
		If CLng(rsA("CallReportTypeId")) = CLng(rs("CallReportTypeId")) Then
%>
										<option selected value="<%= rsA("CallReportTypeId") %>"><%= rsA("CallReportType") %></option>
<%
		Else
%>
										<option value="<%= rsA("CallReportTypeId") %>"><%= rsA("CallReportType") %></option>
<%
		End If
		rsA.MoveNext
	Loop
End If

If IsObject(rsA) Then
	rsA.Close
	Set rsA = Nothing
End If

%>
									</select>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Date of Call</td>
									<td valign="top"><input type="input" value="<%= FormatDateU(rs("DateEntered"), False) %>" name="DateEntered" readonly ID="Input1"> <a href="javascript:showCal('Calendar1')"><img src="/Images/Calendar.gif" border=0></a></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Contact</td>
									<td valign="top">
									<select name="ContactId" ID="Select2" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
										<option selected value="<%= rs("ContactId") %>"><%= rs("CompanyName") %> - <%= rs("Surname") %>, <%= rs("FirstName") %></option>
										<option value=""> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>

<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & rs("Code") & "' ORDER BY CompanyName, Surname, FirstName"
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
									<td valign="top" style="font-weight:bold;">Call Purpose</td>
									<td valign="top">
									<textarea name="CallPurpose" id="CallPurpose" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)"><%= rs("CallPurpose") %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<%= 500 - Len(rs("CallPurpose")) %>" readonly ID="Text2">
									</td>								
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Comment</td>
									<td valign="top">
									<textarea name="Comment" id="Comment" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Comment") %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<%= 500 - Len(rs("Comment")) %>" readonly ID="Text1">
									</td>								
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
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
