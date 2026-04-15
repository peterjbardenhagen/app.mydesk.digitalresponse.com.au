<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngRFQId
Dim strFromEmail
Dim strToEmail
Dim strAttention

lngRFQId = CLng(Request("RFQId"))
strFromEmail = Request.Cookies("UserSettings")("Email")
strToEmail = Request("ToEmail")
strAttention = Request("Attention")

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select RFQ.*, RFQ.SupplierNotes As SN, RFQ.DivisionId As QDivisionId, Users.*, RFQStatus.RFQStatus From ((RFQ INNER JOIN Users ON RFQ.Code = Users.Code) INNER JOIN RFQStatus ON RFQStatus.RFQStatusId = RFQ.RFQStatusId) Where RFQId = " & lngRFQId
Set rsQu = dbConn.Execute(sql)

strCode = rsQu("Code")

If rsQu("RFQStatusId") <> 23 Then
	sql = "Update RFQ Set RFQStatusId = 23 Where RFQId = " & lngRFQId
	dbConn.Execute(sql)
End If

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

strSubject = rsDi("Division") & " (RFQ # " & lngRFQId & ")"

strBody = "<html><head><style>body,p,td{font-family:arial;font-size:10pt;}</style></head><body>"
strBody = strBody & "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached request for quote (RFQ # " & lngRFQId & ").<br><br>"
If Trim(rsQu("IntroText")) <> "" Then strBody = strBody & "<b>Notes:</b> " & rsQu("IntroText") & "<br><br>"
If Trim(rsQu("SupplierNotes")) <> "" Then strBody = strBody & "<b>Supplier Notes: </b>" & rsQu("SupplierNotes") & "<br><br>"
strBody = strBody & "Please note that you can reply online to this request for quote at <a href=""https://" & Request.ServerVariables("SERVER_NAME") & "/Guest/" & Request.Cookies("ClientSettings")("Prefix") & "/?Page=RFQ_Reply.asp&RFQid=" & rsQu("RFQid") & "&Password=" & rsQu("Password") & """>https://" & Request.ServerVariables("SERVER_NAME") & "/Guest/" & Request.Cookies("ClientSettings")("Prefix") & "/?Page=RFQ_Reply.asp&RFQid=" & rsQu("RFQid") & "&Password=" & rsQu("Password") & "</a>. Alternatively print the request for quote and write in the blank values and fax back to us.<br><br>"
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
		.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/RFQ/Files") & "\" & lngRFQId & "\RFQ.html")
		.Send
	End With
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
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"> <input type="button" value=" View RFQ " onclick="document.location.href='View.asp?RFQId=<%= lngRFQId %>';" ID="Button4" NAME="Button1"> <input type="button" value=" Email " onclick="document.location.href='Email.asp?RFQId=<%= lngRFQId %>';" ID="Button2" NAME="Button2"></td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint">
			<tr>
				<td class="Header4">Email Request For Quote / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Request For Quote email not sent successfully. Please try again later.</td>
<%
Else
%>
				<td>Request For Quote email sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->