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
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please complete the Division field.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.CustomerCode) && (emptyField(document.Form1.SupplierCode))) {
				alert("Please complete either Supplier Code or Customer Code.");
				validFlag = false;
				document.Form1.CustomerCode.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.CustomerCode) && !(emptyField(document.Form1.SupplierCode))) {
				alert("Please ensure that the company does not have a Supplier Code and a Customer Code.");
				validFlag = false;
				document.Form1.CustomerCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Company)) {
				alert("Please complete the Company field.");
				validFlag = false;
				document.Form1.Company.focus();
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
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / <a href="Default.asp">Companies</a> / Add Company /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Division</td>
									<td valign="top">
									<select name="DivisionId" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%
Set rsDV = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDV = dbConn.Execute(sql)

If Not(rsDV.BOF And rsDV.EOF) Then
	Do Until rsDV.EOF
%>
										<option value="<%= rsDV("DivisionId") %>"><%= rsDV("Division") %></option>
<%
		rsDV.MoveNext
	Loop
End If
If IsObject(rsDV) Then
	rsDV.Close
	Set rsDV = Nothing
End If
%>
									</select>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req"></td>
									<td valign="top" style="font-weight:bold;">Customer Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="CustomerCode" id="CustomerCode" style="width:280px;"></td>
								</tr>
								<tr>
									<td valign="top" class="Req"></td>
									<td valign="top" style="font-weight:bold;">Supplier Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="SupplierCode" id="Text2" style="width:280px;"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Company</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="Company" id="Text1" style="width:280px;"></td>
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