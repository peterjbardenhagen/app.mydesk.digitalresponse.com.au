<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Session("LoggedIn") Then
	Response.Redirect("PortalFrame.asp")
Else
'does not work
'	If Request.Cookies("LoggedIn") Then
'		Response.Redirect("PortalFrame.asp")
'	End If
End If

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
			if (emptyField(document.Form1.Username)) {
				alert("Please complete the Username field.");
				validFlag = false;
				document.Form1.Username.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Password)) {
				alert("Please complete the Password field.");
				validFlag = false;
				document.Form1.Password.focus();
			}}

		return validFlag 
		}

		// made redundant can't access in new browsers
		//parent.HeaderFrame.location.href = parent.HeaderFrame.location.href;

		</script>

	</head>
	<body bgcolor="#dddddd" onload="setFocus();">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=770 align="center" cellpadding=0 cellspacing=0 border=0>
		<tr>
			<td>
				<br/><br/>
				<form action="<%= Session("WorkingDir") %>/Portal/Validate.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
<%

If strMsg <> "" Then
	Response.Write("				<p class=""Error"">" & strMsg & "</p>")
End If

%>
				<center>
				<table>
					<tr>
						<td><strong>Username : </strong></td>
						<td><input type="text" name="Username" id="Username" size=20 maxlength=50></td>
					</tr>
					<tr>
						<td><strong>Password :	</strong></td>
						<td><input type="password" name="Password" id="Password" size=20 maxlength=50></td>
					</tr>
					<tr>
						<td colspan=2 align="right"><input type="submit" name="Submit" value="Login"></td>
					</tr>
				</table>
				</center>
				</form>
			</td>
		</tr>
	</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
