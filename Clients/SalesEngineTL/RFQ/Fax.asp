<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngRFQId

lngRFQId = CLng(Request("RFQId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
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
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"> <input type="button" value=" View RFQ " onclick="document.location.href='View.asp?RFQId=<%= lngRFQId %>';" ID="Button4" NAME="Button1"> <input type="button" value=" Email " onclick="document.location.href='Email.asp?RFQId=<%= lngRFQId %>';" ID="Button2" NAME="Button2">&nbsp;<% If boolPrint Then %><input type="button" value="Print" style="font-weight:bold;color:red;" onclick="print();" ID="Button5" NAME="Button2"> (Make sure that you set the orientation to portrait)<% Else %><input type="button" value=" Print " onclick="if(confirm('If you proceed the RFQ\'s status will be set to issued.\nAre you sure you want to proceed?')){document.location.href='View.asp?RFQid=<%= lngRFQid %>&Print=True'}" ID="Button6" NAME="Button1"><% End If %></td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">Fax Request For Quote</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 cellpadding=5>
			<form method="post" action="GenerateRFQ.aspx?WorkingDir=<%= Request.Cookies("ClientSettings")("WorkingDir") %>" name="Form1" onsubmit="return checkForm();">
			<input type="hidden" name="Mode" value="2" ID="Hidden1">
			<input type="hidden" name="RFQId" value="<%= lngRFQId %>">
			<tr>
				<td style="font-weight:bold;vertical-align:top;color:red;">Attention</td>
				<td style="text-align:right;vertical-align:top;"><input type="text" name="Attention" style="width:250px;" ID="Text2"></td>
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