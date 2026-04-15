<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTMailId
intTMailId = CLng(Request("TMailId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT TMail.*, Users.Name As [To], Users_1.Code As [FromCode], Users_1.Name As [From] FROM (TMail INNER JOIN Users ON TMail.FromCode = Users.Code) INNER JOIN Users AS Users_1 ON TMail.ToCode = Users_1.Code WHERE TMail.TMailId = " & intTMailId
Set rs = dbConn.Execute(sql)

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
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
			if (emptyField(document.Form1.ToCode)) {
				alert("Please complete the To User field.");
				validFlag = false;
				document.Form1.ToCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Subject)) {
				alert("Please complete the Subject field.");
				validFlag = false;
				document.Form1.Subject.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Message)) {
				alert("Please complete the Message field.");
				validFlag = false;
				document.Form1.Message.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">T-Mail</a> / Reply to Message /></span>
				<br/><br/>
				<table cellpadding=5 cellspacing=0 border=0>
					<tr>
						<td valign="top"><b>Subject:</b></td>
						<td valign="top"><%= rs("Subject") %></td>
					</tr>
					<tr>
						<td valign="top"><b>Message:</b></td>
						<td valign="top"><%= rs("Message") %></td>
					</tr>
				</table>
				<br>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Reply_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">To User</td>
									<td valign="top">
									<select name="ToCode" ID="Select1" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
										<option selected value="<%= rs("FromCode") %>"><%= rs("From") %></option>
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>

<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Visible") & ") Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If Trim(rsUsers("Code")) = Trim(rs("FromCode")) Then
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			Else
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
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
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Subject</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="Subject" id="Subject" style="width:280px;" value="re: <%= rs("Subject") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top"><span style="font-weight:bold;">Message</td>
									<td valign="top">
									<textarea name="Message" id="Message" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)"></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text3">
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
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
