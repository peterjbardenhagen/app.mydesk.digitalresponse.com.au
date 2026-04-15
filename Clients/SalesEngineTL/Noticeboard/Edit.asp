<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngNid
lngNid = CLng(Request("Nid"))

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
			if (emptyField(document.Form1.Heading)) {
				alert("Please complete the Heading field.");
				validFlag = false;
				document.Form1.Heading.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Message)) {
				alert("Please complete the Notice field.");
				validFlag = false;
				document.Form1.Message.focus();
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
sql = "Select * From Noticeboard Where Nid = " & lngNid
Set rs = dbConn.Execute(sql)

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Noticeboard</a> / Edit Notice /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Noticeboard/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" value="<%= rs("Nid") %>" name="Nid">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Date Expires</td>
									<td valign="top"><input type="input" value="<%= FormatDateU(rs("DateExpires"), False) %>" name="DateExpires" readonly ID="Input1"> <a href="javascript:showCal('Calendar5')"><img src="/Images/Calendar.gif" border=0></a></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Heading</td>
									<td valign="top"><input value="<%= rs("Heading") %>" type="text" size=50 maxlength=50 name="Heading" id="Heading" style="width:280px;"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top"><span style="font-weight:bold;">Notice</td>
									<td valign="top">
									<textarea name="Message" id="Message" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Message") %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<%= 500 - Len(rs("Message")) %>" readonly ID="Text3">
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
