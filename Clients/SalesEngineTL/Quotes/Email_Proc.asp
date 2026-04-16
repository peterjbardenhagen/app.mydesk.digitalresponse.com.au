<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

function Sleep(seconds)
            set oShell = CreateObject("Wscript.Shell")
            cmd = "%COMSPEC% /c timeout " & seconds & " /nobreak"
            oShell.Run cmd,0,1
End function

Sleep(5)


If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
Dim strFromEmail
Dim strToEmail
Dim strAttention
Dim strNotes

lngQid = CLng(Request("Qid"))
strFromEmail = Request.Cookies("UserSettings")("Email")
strToEmail = Request("ToEmail")
strAttention = Request("Attention")
strNotes = Replace(Request("Notes") & "", vbcrlf, "<br>")

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.DivisionId As QDivisionId, Users.*, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

strCode = rsQu("Code")
intQuoteStatusId = rsQu("QuoteStatusId")

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsQu("QDivisionId")
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsQu("ContactId")
Set rsCon = dbConn.Execute(sql)

Set rsUser = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
Set rsUser = dbConn.Execute(sql)

strSubject = rsDi("Division") & " (Quote # " & lngQid & ")"

strBody = "<html><head><style>body,p,td{font-family:arial;font-size:10pt;}</style></head><body>"
strBody = strBody & "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached quote (Quote # " & lngQid & ") "
If rsQu("QuoteCOSId") > 0 Then
	strBody = strBody & " and conditions of sale"
End If
strBody = strBody & ".<br><br>"
If Trim(strNotes) <> "" Then strBody = strBody & "<b>Notes:</b> " & strNotes & "<br><br>"
strBody = strBody & GetUserContactDetails(rsQu("QDivisionId"),rsQu("Code"))
strBody = strBody & "</body></html>"

Sub SendMail(strFromEmail, strToEmail, strSubject, strBody)


	Dim objCDO
	Dim iConf
	Dim Flds

	Const cdoSendUsingPort = 2

	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")

	Set Flds = iConf.Fields

	With Flds
'        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "techlight-com-au.mail.protection.outlook.com"
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 587
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
'        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "bertb@techlight.com.au"
'        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "mnzpznkrgrdodnmo"
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpusessl") = True


        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "smtp.sendgrid.net"
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 587
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "apikey"
        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "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"
		.Item("http://schemas.microsoft.com/cdo/configuration/sendtls") = true	

        .Update
	End With

	Set objCDO.Configuration = iConf
	With objCDO
		.From = strFromEmail
		.To = strToEmail
		.Cc = "bertb@techlight.com.au"
		.Subject = strSubject
		.HtmlBody = strBody
		.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/Files") & "\Quote.pdf")
		If rsQu("QuoteCOSId") > 0 Then
			.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rsQu("QuoteCOSFile"))
		End If
		.Send
	End With

	' Audit trail
	sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Issued by email to " & strToEmail & "', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End Sub

If strFromEmail <> "" Then
	SendMail strFromEmail, strToEmail, strSubject, strBody
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
				<td class="Header4">Email Quote / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Quote email not sent successfully. Please try again later. Error: <%= err.Description %><%= Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/Quotes/Files") & "\Quote.pdf" %></td>
<%
Else
%>
				<td>Quote email sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->