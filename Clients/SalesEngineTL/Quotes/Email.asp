<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
Dim intQuoteStatusId

lngQid = CLng(Request("Qid"))
intQuoteStatusId = CInt(Request("QuoteStatusId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.DivisionId As QDivisionId, Users.*, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsQu("QDivisionId")
Set rsDi = dbConn.Execute(sql)

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts Where ContactId = " & rsQu("ContactId")
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

		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
<%

	If rsQu("QuoteCOSId") <> 0 Then

%>
			<tr>
				<td><li><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/FilesLibrary/Files/<%= rsQu("QuoteCOSFile") %>" target="_blank">Download Conditions of Sale</a></td>
			</tr>
<%

	End If

%>

	</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">Email Quote</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table cellpadding=5>
			<form method="post" action="GenerateQuote.asp?WorkingDir=<%= Request.Cookies("ClientSettings")("WorkingDir") %>" name="Form1" onsubmit="return checkForm();">
			<input type="hidden" name="Mode" value="1" ID="Hidden1">
			<input type="hidden" name="Qid" value="<%= lngQid %>">
			<tr>
				<td nowrap style="font-weight:bold;vertical-align:top;color:red;">Attention</td>
				<td style="text-align:left;vertical-align:top;"><input type="text" name="Attention" style="width:250px;" ID="Text2" value="<%= rsQu("Attention") %>"></td>
			</tr>
			<tr>
				<td nowrap style="font-weight:bold;vertical-align:top;">Notes <small>These notes will appear in the email</small></td>
				<td style="text-align:left;vertical-align:top;"><textarea name="Notes" style="width:500px;height:100px;" ID="Textarea1"><%= rsQu("CustomerNotes") %></textarea></td>
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