<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

Response.Cookies("LoggedIn") = False

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Session("WorkingDir") %>/System/<%= Session("Stylesheet") %>">
		<!--<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>-->
		<script language="JavaScript">

		function setFocus() {
			document.Form1.Username.focus();
		}

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
			if (emptyField(document.Form1.CurrentPassword)) {
				alert("Please complete the Current Password field.");
				validFlag = false;
				document.Form1.CurrentPassword.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.NewPassword)) {
				alert("Please complete the New Password field.");
				validFlag = false;
				document.Form1.NewPassword.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.NewPasswordC)) {
				alert("Please complete the New Password Confirm field.");
				validFlag = false;
				document.Form1.NewPasswordC.focus();
			}}

			if (validFlag) {
			if (document.Form1.NewPassword.value != document.Form1.NewPasswordC.value) {
				alert("Please ensure that the New Password field matches the New Password Confirm field.");
				validFlag = false;
				document.Form1.NewPassword.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr>
			<td>
				<br/><br/>
				<span class="Header2">Change Password /></span>
				<br><br>
				It has been <b><%= Request("Days") %> days</b> since you last changed your password. Before you can login again, you must change your password.
				<br/><br/>
<%

If strMsg <> "" Then

	Response.Write("				<p class=""Error"">" & strMsg & "</p>")

End If

%>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
							<table>
								<form action="ChangePassword_Proc.asp" method="post" onSubmit="return checkForm();" target="_parent" name="Form1">
								<tr>
									<td class="Req">*</td>
									<td style="font-weight:bold;">Current Password:</td>
									<td><input type="password" name="CurrentPassword" id="CurrentPassword" maxlength=20></td>
								</tr>
								<tr>
									<td class="Req">*</td>
									<td style="font-weight:bold;">New Password:</td>
									<td><input type="password" name="NewPassword" ID="NewPassword" maxlength=20></td>
								</tr>
								<tr>
									<td class="Req">*</td>
									<td style="font-weight:bold;">New Password Confirm:</td>
									<td><input type="password" name="NewPasswordC" ID="NewPasswordC" maxlength=20></td>
								</tr>
								<tr>
									<td colspan=3 align="right"><input type="submit" value="Change Password"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->