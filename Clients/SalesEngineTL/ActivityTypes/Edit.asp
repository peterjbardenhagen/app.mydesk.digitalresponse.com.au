<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intActivityTypeId
intActivityTypeId = CLng(Request("ActivityTypeId"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
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
			if (emptyField(document.Form1.ActivityCode)) {
				alert("Please complete the Activity Code field.");
				validFlag = false;
				document.Form1.ActivityCode.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.ActivityType)) {
				alert("Please complete the Activity Type field.");
				validFlag = false;
				document.Form1.ActivityType.focus();
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
sql = "Select * From ActivityTypes Where ActivityTypeId = " & intActivityTypeId
Set rs = dbConn.Execute(sql)

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / <a href="Default.asp">Activity Types</a> / Edit Activity Type /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/ActivityTypes/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" value="<%= rs("ActivityTypeId") %>" name="ActivityTypeId">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Activity Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="ActivityCode" id="ActivityCode" style="width:280px;" value="<%= rs("ActivityCode") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Activity Type</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="ActivityType" id="Text1" style="width:280px;" value="<%= rs("ActivityType") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Form Required</td>
									<td valign="top">
										<input type="radio" name="FormRequired" value="-1" ID="Radio3" <% If rs("FormRequired") Then %>checked<% End If %>> Yes<br/>
										<input type="radio" name="FormRequired" value="0" ID="Radio4" <% If Not rs("FormRequired") Then %>checked<% End If %>> No
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
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->