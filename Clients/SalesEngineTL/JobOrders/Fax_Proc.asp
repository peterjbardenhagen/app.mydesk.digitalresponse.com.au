<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
Dim strFromFax
Dim strToFax
Dim strAttention
Dim strNotes

lngQid = CLng(Request("Qid"))
strFromFax = Trim(Request("FromFax"))
strToFax = Request("ToFax")
strAttention = Request("Attention")
strNotes = Trim(Request("Notes"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.DivisionId As QDivisionId, Users.*, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

strCode = rsQu("Code")

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsQu("QDivisionId")
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts_WithCustomersAndSuppliers Where ContactId = " & rsQu("ContactId")
Set rsCon = dbConn.Execute(sql)

Set rsUser = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
Set rsUser = dbConn.Execute(sql)

strSubject = rsDi("Division") & " (Quote # " & lngQid & ")"

strBody = "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached quote (Quote # " & lngQid & ") "
If rsQu("QuoteCOSId") > 0 Then
	strBody = strBody & " and conditions of sale"
End If
strBody = strBody & ".<br><br>"
If Trim(strNotes) <> "" Then strBody = strBody & "<b>Notes:</b> " & Replace(strNotes, vbcrlf, "<br>") & "<br><br>"
strBody = strBody & GetUserContactDetailsFax(rsQu("Code"))

Sub SendMail(strFromFax, strToFax, strSubject, strBody)
	Dim objCDO
	Dim iConf
	Dim Flds

	Const cdoSendUsingPort = 2

	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")

	Set Flds = iConf.Fields
	With Flds
		.Item(cdoSendUsingMethod) = cdoSendUsingPort
		.Item(cdoSMTPServer) = "techlight-com-au.mail.protection.outlook.com"
		.Item(cdoSMTPAuthenticate) = 1  
        .Item(cdoSendUsername) = "bertb@techlight.com.au" 
        .Item(cdoSendPassword) = "mnzpznkrgrdodnmo" 
		.Item(cdoSMTPServerPort) = 25
		.Item(cdoSMTPconnectiontimeout) = 10
		.Update

	End With

	Set objCDO.Configuration = iConf
	With objCDO
		.From = Request.Cookies("UserSettings")("Email")
		.To = strToFax & "@venali.net"
		.Cc = "peterb@digitalresponse.com.au"
		.Subject = strSubject
		.HtmlBody = strBody
		.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/Files") & "\" & lngQid & "\Quote.html")
		If rsQu("QuoteCOSId") > 0 Then
			.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rsQu("QuoteCOSFile"))
		End If
		.Send
	End With

	' Audit trail
	sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Issued by fax to " & strToFax & "', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End Sub

If strToFax <> "" Then
	SendMail strFromFax, strToFax, strSubject, strBody
End If

'Cleanup
Set ObjCDO = Nothing
Set iConf = Nothing
Set Flds = Nothing
%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="button" value=" View Quote " onclick="document.location.href='View.asp?Qid=<%= lngQid %>';" ID="Button4" NAME="Button1"> <input type="button" value=" Email " onclick="document.location.href='Email.asp?Qid=<%= lngQid %>';" ID="Button2" NAME="Button2"></td>
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
		<table class="NoPrint">
			<tr>
				<td class="Header4">Fax Quote / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Quote fax not sent successfully. Please try again later.</td>
<%
Else
%>
				<td>Quote fax sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->