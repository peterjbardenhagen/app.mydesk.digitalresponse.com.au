<% 

Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expiresabsolute = ServerToEST(Now()) - 1 
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

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
		<!--<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>-->
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
			if (emptyField(document.Form1.UploadFrom)) {
				alert("Please complete the Upload From Date field.");
				validFlag = false;
				document.Form1.UploadFrom.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.MinimumValue)) {
				alert("Please complete the Minimum Value field.");
				validFlag = false;
				document.Form1.MinimumValue.focus();
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
sql = "Select * From [Parameters]"
Set rs = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("Manager") Then

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Parameters
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Upload From Date</td>
									<td valign="top"><input type="input" value="<%= FormatDateU(rs("UploadFrom"), False) %>" name="UploadFrom" readonly ID="Input2"> <a href="javascript:showCal('Calendar6')"><img src="/Images/Calendar.gif" border=0></a></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Minimum Value</td>
									<td valign="top"><input type="text" name="MinimumValue" style="width:280px;" ID="Text1" value="<%= rs("MinimumValue") %>"></td>
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