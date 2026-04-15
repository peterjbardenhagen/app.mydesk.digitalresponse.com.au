<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid
Dim sql
Dim rs

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PurchaseOrders Inner Join PurchaseOrderStatus PS On PS.POStatusId = PurchaseOrders.POStatusId Where POid = " & lngPOid
Set rs = dbConn.Execute(sql)

lngPOStatusId = CLng(rs("POStatusId"))
strPOStatus = rs("POStatus")

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

If lngPOStatusId = 7 Then

%>
<html>
	<head>
	</head>
	<body>
	<script language="javascript">
		alert('Cannot change status.');
		window.close();
	</script>
	</body>
</html>
<%

Else

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<style>
			body, p, td, th, select
			{
				font-family: Arial;
				font-size: 12px;
			}
			input
			{
				font-family: Arial;
				font-size: 10px;
			}
			.Header4
			{
				font-family: Arial;
				font-size: 14px;
				font-weight: bold;
				color: #000000;
			}
		</style>
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		// Check form for validation errors
		function checkForm() {

			var validFlag = true;

			if (validFlag) {
			if (emptyField(document.Form1.POStatusId)) {
				alert("Please select New Purchase Order Status.");
				validFlag = false;
				document.Form1.POStatusId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Notes)) {
				alert("Please complete Notes.");
				validFlag = false;
				document.Form1.Notes.focus();
			}}

			return validFlag;
		}

		var itemLines=1;
		</script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	</head>
	<body style="background-color:white;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td bgcolor="#DDDDDD" class="Header4">Update Purchase Order Status</td>
				<td bgcolor="#DDDDDD" align="right"><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
			<tr>
				<td colspan=2>
					<table cellpadding=5 cellspacing=0 border=0>
						<form name="Form1" id="Form1" method="post" action="UpdateStatus_Proc.asp" onsubmit="return checkForm();">
						<input type="hidden" name="POid" value="<%= lngPOid %>">
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Purchase Order #</td>
							<td valign="top"><%= lngPOid %></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Current Status</td>
							<td valign="top"><%= strPOStatus %></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">New Status</td>
							<td valign="top">
							<select name="POStatusId" style="width:280px;" ID="Select2">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%
	Set rsStatus = Server.CreateObject("ADODB.RecordSet")
	If Request.Cookies("UserSettings")("UserTypeId") = 6 Then
		sql = "Select * From PurchaseOrderStatus Order By POStatus"
	Else
		sql = "Select * From PurchaseOrderStatus Where POStatusId = 6 Or POStatusId = 7 Order By POStatus"
	End If
	Set rsStatus = dbConn.Execute(sql)

	If Not(rsStatus.BOF And rsStatus.EOF) Then
		Do Until rsStatus.EOF
			If Request.Cookies("UserSettings")("UserTypeId") = 6 Or (rsStatus("POStatusId") = 7 And lngPOStatusId = 4) Or (rsStatus("POStatusId") = 6) Then
				If (CLng(rsStatus("POStatusId")) = lngPOStatusId) Then %>
								<option selected value="<%= rsStatus("POStatusId") %>"><%= rsStatus("POStatus") %></option>
<%
				Else
%>
								<option value="<%= rsStatus("POStatusId") %>"><%= rsStatus("POStatus") %></option>
<%
				End If
			End If
			rsStatus.MoveNext
		Loop
	End If

	If IsObject(rsStatus) Then
		rsStatus.Close
		Set rsStatus = Nothing
	End If
%>
							</select>
							</td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top"><span style="font-weight:bold;">Notes</span><br>Delivery docket must be entered if received, reason must be provided if cancelled.</td>
							<td valign="top"><textarea name="Notes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount3',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" tabindex=7 ID="Textarea2"></textarea><br/>Characters Remaining: <input type="text" name="textcount3" size="4" value="500" readonly ID="Text2"></td>
						</tr>
						<tr>
							<td colspan=3 align="right"><br><input type="submit" value="Submit"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>
	</body>
</html>
<%

End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->