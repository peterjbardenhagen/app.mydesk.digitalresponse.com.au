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
Dim strFromFax
Dim strToFax
Dim strAttention

lngRFQId = CLng(Request("RFQId"))
strFromFax = Trim(Request("FromFax"))
strToFax = Request("ToFax")
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

strBody = "<style>body,p,td {font-family:arial;font-size:12px;}</style>"
strBody = strBody & "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached request for quote (RFQ # " & lngRFQId & ").<br><br>"
If Trim(rsQu("IntroText")) <> "" Then strBody = strBody & "<b>Notes:</b> " & rsQu("IntroText") & "<br><br>"
If Trim(rsQu("SupplierNotes")) <> "" Then strBody = strBody & "<b>Supplier Notes: </b>" & rsQu("SupplierNotes") & "<br><br>"
strBody = strBody & "Please note that you can reply online to this request for quote at https://" & Request.ServerVariables("SERVER_NAME") & "/Guest/" & Request.Cookies("ClientSettings")("Prefix") & "/?Page=RFQ_Reply.asp&RFQid=" & rsQu("RFQid") & "&Password=" & rsQu("Password") & ". Alternatively write in the blank values and fax back to us.<br><br>"
strBody = strBody & GetUserContactDetailsFax(rsQu("QDivisionId"),rsQu("Code"))

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
		.Item(cdoSMTPServer) = "localhost"
		.Item(cdoSMTPAuthenticate) = 1  
        .Item(cdoSendUsername) = "email@techlight.com.au" 
        .Item(cdoSendPassword) = "DResponse1802" 
		.Item(cdoSMTPServerPort) = 587
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
		.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/RFQ/Files") & "\" & lngRFQId & "\RFQ.html")
		.Send
	End With
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
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"> <input type="button" value=" View RFQ " onclick="document.location.href='View.asp?RFQId=<%= lngRFQId %>';" ID="Button4" NAME="Button1"> <input type="button" value=" Email " onclick="document.location.href='Email.asp?RFQId=<%= lngRFQId %>';" ID="Button2" NAME="Button2"></td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint">
			<tr>
				<td class="Header4">Fax Request For Quote / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Request For Quote fax not sent successfully. Please try again later.</td>
<%
Else
%>
				<td>Request For Quote fax sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->