<%

Response.Expires = -1

%>
<html>
	<head>
		<title>Morcare</title>
		<link rel="stylesheet" type="text/css" href="System/Style.css">
		<script language="JavaScript">

		function setFocus() {
			document.Form1.Username.focus();
		}

		</script>

	</head>
	<body bgcolor="#dddddd" onload="setFocus();">

<!--#include virtual="/SalesEngine/System/ssi_Header_Morcare.inc"-->

	<table width=780 cellpadding=0 cellspacing=0 border=0>
		<tr>
			<td>
				<br/><br/>
				<form action="Validate.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
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
						<td colspan=2 align="right"><input type="button" name="Submit" value="Login"></td>
					</tr>
				</table>
				</form>
			</td>
		</tr>
	</table>

	</body>
</html>