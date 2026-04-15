<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
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

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
Set rsDi = dbConn.Execute(sql)

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsPO("LocationId")
Set rsLoc = dbConn.Execute(sql)

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
			if (emptyField(document.Form1.ToEmail)) {
				alert("Please enter an Email Address.");
				validFlag = false;
				document.Form1.ToEmail.focus();
			}}
			
			return validFlag;
		}

		function formatNotes() {
			document.Form1.Notes.value = document.Form1.Notes.value.replace(new RegExp("\\n","g"),"{newline}").replace(/\r/g,'');
			alert(document.Form1.Notes.value);
		}

		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">Email Purchase Order</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 cellpadding=5>
			<form method="post" action="GeneratePO.asp?WorkingDir=<%= Request.Cookies("ClientSettings")("WorkingDir") %>" name="Form1" onsubmit="return checkForm();">
			<input type="hidden" name="Mode" value="1" ID="Hidden1">
			<input type="hidden" name="POid" value="<%= lngPOid %>">
			<input type="hidden" name="CurrencyName" value="<%= Session("CurrencyName") %>" ID="Hidden2">
			<input type="hidden" name="CurrencyRate" value="<%= Session("CurrencyRate") %>" ID="Hidden3">
			<input type="hidden" name="CurrencyPrefix" value="<%= Session("CurrencyPrefix") %>" ID="Hidden4">
			<tr>
				<td nowrap style="font-weight:bold;vertical-align:top;color:red;">Attention</td>
				<td style="text-align:left;vertical-align:top;"><input type="text" name="Attention" style="width:250px;" ID="Text2"></td>
			</tr>
			<tr>
				<td nowrap style="font-weight:bold;vertical-align:top;">Notes <small>These notes will appear in the email</small></td>
				<td style="text-align:left;vertical-align:top;"><textarea name="Notes" id="Notes" style="width:500px;height:100px;"><%= rsPO("IntroText") %></textarea></td>
			</tr>
			<tr>
				<td colspan="2">
					<strong>Select contact to get email address:</strong></br>
					<select name="ContactId" ID="ContactId" style="width:100%;" onchange="document.forms[0].ToEmail.value = document.getElementById('ContactId').value;">
						<option></option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
						<option value="<%= rsContacts("Email") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %> - Email is <%= rsContacts("Email") %></option>
<%

		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
					</select>
				</td>
			</tr>
			<tr>
				<td nowrap style="font-weight:bold;color:red; vertical-align:top;">Email Address</td>
				<td align="right" nowrap><input type="text" name="ToEmail" value="<%= rsCon("Email") %>" style="width:250px;">&nbsp;<input type="button" value="Set to my email" onclick="document.forms[0].ToEmail.value = '<%= Request.Cookies("UserSettings")("Email") %>';" /></td>
			</tr> 
			<tr>
				<td colspan=2 align="right"><br><br><input type="submit" value="Send"></td>
			</tr>
			</form>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->