<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT [PO].*, PO.IntroText AS IT, PO.InternalNotes AS [IN], [Users].*, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM (Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsPO("ContactId")
Set rsCon = dbConn.Execute(sql)

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
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
			if (emptyField(document.Form1.Attention)) {
				alert("Please complete the Attention field.");
				validFlag = false;
				document.Form1.Attention.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.FromFax)) {
				alert("Please enter the From Fax #.");
				validFlag = false;
				document.Form1.FromFax.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ToFax)) {
				alert("Please enter the To Fax #.");
				validFlag = false;
				document.Form1.ToFax.focus();
			}}

			if (validFlag) {
    			if(document.Form1.ToFax.value.charAt(0) == '0' || isNaN(document.Form1.ToFax.value)) {
    			    validFlag = false;
    			}
    		}
		
			return validFlag;
		}

		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">Fax Purchase Order</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 cellpadding=5>
			<form method="post" action="GeneratePO.aspx?WorkingDir=<%= Request.Cookies("ClientSettings")("WorkingDir") %>" name="Form1" onsubmit="return checkForm();">
			<input type="hidden" name="Mode" value="2" ID="Hidden1">
			<input type="hidden" name="POid" value="<%= lngPOid %>">
			<input type="hidden" name="CurrencyName" value="<%= Session("CurrencyName") %>" ID="Hidden2">
			<input type="hidden" name="CurrencyRate" value="<%= Session("CurrencyRate") %>" ID="Hidden3">
			<input type="hidden" name="CurrencyPrefix" value="<%= Session("CurrencyPrefix") %>" ID="Hidden4">
			<tr>
				<td style="font-weight:bold;vertical-align:top;color:red;">Attention</td>
				<td style="text-align:right;vertical-align:top;"><input type="text" name="Attention" style="width:250px;" ID="Text2"></td>
			</tr> 
			<tr>
				<td style="font-weight:bold;vertical-align:top;">Notes</td>
				<td style="text-align:right;vertical-align:top;"><textarea name="Notes" style="width:250px;height:100px;" ID="Textarea1"></textarea></td>
			</tr>
			<tr>
				<td style="font-weight:bold;vertical-align:top;color:red;">From Fax #<br><small>(format 61398555833)</small></td>
				<td style="text-align:right;vertical-align:top;"><input type="text" name="FromFax" style="width:250px;" ID="Text1"></td>
			</tr> 
			<tr>
				<td style="font-weight:bold;vertical-align:top;color:red;">To Fax #<br><small>(format 61398555833)</small></td>
				<td style="text-align:right;vertical-align:top;"><input type="text" name="ToFax" style="width:250px;"> <input type="button" onclick="SelectFaxFromContacts('<%= Request.Cookies("ClientSettings")("WorkingDir") %>');" value="Select fax number from contacts" /></td>
			</tr> 
			<tr>
				<td colspan=2 align="right"><input type="submit" value="Fax"></td>
			</tr>
			</form>
		</table>
		</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->