<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTableId
Dim intItemId

intTableId = CLng(Request("TableId"))
intItemId = CLng(Request("ItemId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
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
			if(document.Form1.FollowUpRequired.checked){
				if (emptyField(document.Form1.FollowUpDate)) {
					alert("Please complete the Follow-Up Date field.");
					validFlag = false;
					document.Form1.FollowUpDate.focus();
				}}
			}
			
			if (validFlag) {
			if (emptyField(document.Form1.Comment)) {
				alert("Please complete the Comment field.");
				validFlag = false;
				document.Form1.Comment.focus();
			}}

		return validFlag 
		}
		
		function FollowUpReq() {
			if(!document.Form1.FollowUpRequired.checked) {
				document.Form1.FollowUpDate.value = '';
			}
		}

		</script>
	</head>
	<body>
		<table width="100%" cellpadding=0 cellspacing=0 border=0>
			<tr>
				<td><span class="Header3">Add Comment</span></td>
				<td align="right"><a href="#" onclick="history.go(-1);"><< Back</a></td>
			</tr>
		</table>
		<br>
		<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
			<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
			<input type="hidden" name="TableId" id="TableId" value="<%= intTableId %>">
			<input type="hidden" name="ItemId" id="ItemId" value="<%= intItemId %>">
			<tr>
				<td valign="top"></td>
				<td valign="middle"><span style="font-weight:bold;">Follow-Up Required</td>
				<td valign="top">
					<table>
						<tr>
							<td valign="middle"><input type="checkbox" name="FollowUpRequired" value="True" style="border:0px;" checked onclick="FollowUpReq();" ID="Checkbox1"></td>
							<td valign="middle" style="font-weight:bold;">Date</td>
							<td valign="middle"><input type="text" value="<%= FormatDateU(DateAdd("d", 14, ServerToEST(Now())), False) %>" name="FollowUpDate" readonly ID="Input1"></td>
							<td valign="middle"><a href="javascript:showCal('Calendar11')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
					</table>
			   </td>
			</tr>
			<tr>
				<td valign="top" class="Req"></td>
				<td valign="top" style="font-weight:bold;">Copy in User</td>
				<td valign="top">
				<select name="ToCode" ID="Select1" style="width:280px;">
					<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>

<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
%>
					<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
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
				<td valign="top"><span style="font-weight:bold;">Comment</td>
				<td valign="top">
				<textarea name="Comment" id="Comment" rows="5" cols="30" style="width:250px;" onkeyup="parent.parent.TrackCount(this,'textcount2',500)" onkeypress="parent.parent.LimitText(this,500)"></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text3">
				</td>
			</tr>
			<input type="hidden" name="Reply" value="False" ID="Hidden1">
			<tr>
				<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
			</tr>
			</form>
		</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->