<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
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
			if (emptyField(document.Form1.ExpenseTypeGroupId)) {
				alert("Please complete the Expense Type Group field.");
				validFlag = false;
				document.Form1.ExpenseTypeGroupId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ExpenseCode)) {
				alert("Please complete the Expense Code field.");
				validFlag = false;
				document.Form1.ExpenseCode.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.ExpenseType)) {
				alert("Please complete the Expense Type field.");
				validFlag = false;
				document.Form1.ExpenseType.focus();
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
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / <a href="Default.asp">Expense Types</a> / Add Expense Type /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Expense Type Group</td>
									<td valign="top">
									<select name="ExpenseTypeGroupId" ID="Select1" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%
Set rsEX = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpenseTypeGroups ORDER BY ExpenseTypeGroup"
Set rsEX = dbConn.Execute(sql)

If Not(rsEX.BOF And rsEX.EOF) Then
	Do Until rsEX.EOF
%>
										<option value="<%= rsEX("ExpenseTypeGroupId") %>"><%= rsEX("ExpenseTypeGroup") %></option>
<%
		rsEX.MoveNext
	Loop
End If
If IsObject(rsEX) Then
	rsEX.Close
	Set rsEX = Nothing
End If
%>
									</select>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Expense Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="ExpenseCode" id="ExpenseCode" style="width:280px;"></td>
								</tr>
								<tr>
									<td valign="top" class="Req"></td>
									<td valign="top" style="font-weight:bold;">FBT Expense Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="FBTExpenseCode" id="Text2" style="width:280px;"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Expense Type</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="ExpenseType" id="Text1" style="width:280px;"></td>
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