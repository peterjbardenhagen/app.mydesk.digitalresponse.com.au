<%

Response.Expires = -1

Dim strCode
strCode = Trim(Request("Code"))

%>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_open.inc"-->
<html>
	<head>
		<title></title>
		<link rel="stylesheet" type="text/css" href="System/Style.css">
		<script language="javascript" src="System/Global.js"></script>
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
			if (emptyField(document.Form1.Code)) {
				alert("Please complete the Code field.");
				validFlag = false;
				document.Form1.Code.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PW)) {
				alert("Please complete the PW field.");
				validFlag = false;
				document.Form1.PW.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/SalesEngine/System/ssi_Header.inc"-->

	<table width=780 cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr>
			<td>
				<br/><br/>
				<span class="Header2"><a href="Portal.asp"><font color="#666666">Home</font></a> / <a href="ManageUsers.asp"><font color="#666666">Manage Users</font></a> / Edit User /></span>
				<br/><br/>
<%

Dim rs
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM SalesPeople Where Code = '" & strCode & "'"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
				<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
					<form method="post" action="ManageUsers_Edit_Proc.asp" name="Form1" id="Form1" onSubmit="return checkForm();">
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
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Code</b></td>
						<td valign="top"><input size=50 maxlength=500 type="text" name="Code" id="Code" value="<%= rs("Code") %>"></td>
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
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Active</b></td>
						<td valign="top">
							<input type="radio" name="Active" id="Active" value="-1" <% If rs("Active") = -1 Then Response.Write("Checked") End If %>> Yes<br/>
							<input type="radio" name="Active" id="Active" value="0" <% If rs("Active") = 0 Then Response.Write("Checked") End If %>> No
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td valign="top" class="Req">*</td>
						<td valign="top"><b>Admin</b></td>
						<td valign="top">
							<input type="radio" name="Admin" id="Admin" value="-1" <% If rs("Admin") = -1 Then Response.Write("Checked") End If %>> Yes<br/>
							<input type="radio" name="Admin" id="Admin" value="0" <% If rs("Admin") = 0 Then Response.Write("Checked") End If %>> No
						</td>
					</tr>
					<tr height=2>
						<td colspan=3></td>
					</tr>
					<tr>
						<td colspan=3 align="right"><input type="button" value="Cancel" onclick="document.location.href='ManageUsers.asp';"> <input type="submit" value="Submit" id="Submit"></td>
					</tr>
					</form>
				</table>
<%

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
			</td>
		</tr>
	</table>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_close.inc"-->