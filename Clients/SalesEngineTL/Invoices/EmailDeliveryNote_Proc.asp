<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngInvoiceId
Dim strFromEmail
Dim strToEmail
Dim strAttention
Dim strNotes

lngInvoiceId = CLng(Request("InvoiceId"))
strFromEmail = Request.Cookies("UserSettings")("Email")
strToEmail = Request("ToEmail")
strAttention = Request("Attention")
strNotes = Trim(Replace(Request("Notes"),CHR(10),"<BR>"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsInv = Server.CreateObject("ADODB.RecordSet")
sql = "Select Invoices.*, Invoices.CustomerNotes As CN, Invoices.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, InvoiceStatus.InvoiceStatus From ((Invoices INNER JOIN Users ON Invoices.Code = Users.Code) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) Where InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(sql)

strCode = rsInv("Code")
intInvoiceStatusId = rsInv("InvoiceStatusId")

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsInv("QDivisionId")
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsInv("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsUser = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
Set rsUser = dbConn.Execute(sql)

strSubject = rsDi("Division") & " (Delivery Note # " & lngInvoiceId & ")"

strBody = "<html><head><style>body,p,td{font-family:arial;font-size:10pt;}</style></head><body>"
strBody = strBody & "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached Delivery Note (Invoice # " & lngInvoiceId & ") "
strBody = strBody & ".<br><br>"
If Trim(strNotes) <> "" Then strBody = strBody & "<b>Notes:</b> " & Replace(strNotes, vbcrlf, "<br>") & "<br><br>"
strBody = strBody & GetUserContactDetails(rsInv("QDivisionId"), rsInv("Code"))
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
	
	dim pdf
	pdf = ""
	pdf = Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/DeliveryNotes") & "\Files\DeliveryNote.pdf"

	Set objCDO.Configuration = iConf
	With objCDO
		.From = strFromEmail
		.To = strToEmail
		.Bcc = "bertb@techlight.com.au;admin@techlight.com.au"
		.Subject = strSubject
		.HtmlBody = strBody
		.AddAttachment(pdf)
		.Send
	End With

	' Audit trail
	sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Delivery Note issued by email to " & strToEmail & "', '" & ServerToEST(Now()) & "')"
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
				<td>
				<!--#include file="NavBarDeliveryNote.asp"-->
				</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint">
			<tr>
				<td class="Header4">Email Delivery Note / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Delivery Note email not sent successfully. Please try again later.</td>
<%
Else
%>
				<td>Delivery Note email sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->