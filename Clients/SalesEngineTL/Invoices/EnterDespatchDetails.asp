<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngInvoiceId = CInt(Request("InvoiceId"))

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
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
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

		// Check form for validation errors
		function checkForm() {

			var validFlag = true;

			if (validFlag) {
			if (emptyField(document.Form1.DespatchDate)) {
				alert("Please complete the Despatch Date field.");
				validFlag = false;
				document.Form1.DespatchDate.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Carrier)) {
				alert("Please complete the Carrier field.");
				validFlag = false;
				document.Form1.Carrier.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.CarrierRef)) {
				alert("Please complete the Carrier Ref. field.");
				validFlag = false;
				document.Form1.CarrierRef.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PackageDetails)) {
				alert("Please complete the Package Details field.");
				validFlag = false;
				document.Form1.PackageDetails.focus();
			}}

/*
			if (validFlag) {
			if (emptyField(document.Form1.InternalNotes)) {
				alert("Please complete the Internal Notes field.");
				validFlag = false;
				document.Form1.InternalNotes.focus();
			}}
*/
			
			return validFlag;
		}
		</script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Invoices</a> / Enter Despatch Details</span>
				<br/><br/>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<form method="post" name="Form1" action="EnterDespatchDetails_Proc.asp" onSubmit="return checkForm();" ID="Form1">
						<input type="hidden" name="InvoiceId" value="<%= lngInvoiceId %>" ID="Hidden1">
						<input type="hidden" name="Code" value="<%= Request.Cookies("Code") %>" ID="Hidden2">
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Despatch Date</td>
							<td valign="top"><input type="input" value="" name="DespatchDate" readonly ID="Input1"> <a href="javascript:showCal('Calendar22')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Carrier</td>
							<td valign="top"><input type="text" name="Carrier" maxlength=50></td>								
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Carrier Ref.</td>
							<td valign="top"><input type="text" name="CarrierRef" maxlength=50 ID="Text3"></td>								
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Package Details</td>
							<td valign="top">
							<textarea name="PackageDetails" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)" ID="Textarea2"></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="500" readonly ID="Text1">
							</td>								
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Internal Notes</td>
							<td valign="top">
							<textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)" ID="Textarea1"></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text2">
							</td>								
						</tr>
						<tr>
							<td colspan=4 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){window.close()};" ID="Button1" NAME="Button1"> <input type="submit" value="Next" ID="Submit2" NAME="Submit1"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->