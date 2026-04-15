<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

function Sleep(seconds)
            set oShell = CreateObject("Wscript.Shell")
            cmd = "%COMSPEC% /c timeout " & seconds & " /nobreak"
            oShell.Run cmd,0,1
End function

Sleep(5)

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid
Dim strFromEmail
Dim strToEmail
Dim strAttention
Dim strNotes

lngPOid = CLng(Request("POid"))
strFromEmail = Request.Cookies("UserSettings")("Email")
strToEmail = Request("ToEmail")
strAttention = Request("Attention")
strNotes = Trim(Request("Notes")) & ""
strNotes = Replace(strNotes, vbcrlf, "<br>")

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT [PO].*, PO.IntroText AS IT, [Users].*, PO.DivisionId AS DivisionId, PO.InternalNotes AS [IN],  Users.DivisionId As UserDivisionid, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM (Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

strCode = rsPO("Code")

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsPO("LocationId")
Set rsLoc = dbConn.Execute(sql)

Set rsCon = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsPO("ContactId")
Set rsCon = dbConn.Execute(sql)

Set rsUser = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
Set rsUser = dbConn.Execute(sql)

strSubject = rsDi("Division") & " (Purchase Order # " & lngPOid & ")"

strBody = "<html><head><style>body,p,td{font-family:arial;font-size:10pt;}</style></head><body>"
strBody = strBody & "Dear " & strAttention & ",<br><br>"
strBody = strBody & "Please find attached Purchase Order (Purchase Order # " & lngPOid & ").<br><br>"
If Len(Trim(strNotes)) > 0 Then strBody = strBody & "<b>Notes:</b> " & strNotes & "<br><br>"
strBody = strBody & GetUserContactDetails(rsPO("DivisionId"),rsPO("Code"))
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
		.AddAttachment(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/PurchaseOrders/Files") & "\PurchaseOrder.pdf")
		.Send

		' Audit trail
		sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Issued by email to " & strToEmail & "', '" & ServerToEST(Now()) & "')"
		dbConn.Execute(sql)
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
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint">
			<tr>
				<td class="Header4">Email Purchase Order / Result</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=400 ID="Table1">
			<tr>
<%
If err.Description <> "" Then
%>
				<td style="color:red;">Purchase Order email not sent successfully. Please try again later.</td>
<%
Else
%>
				<td>Purchase Order email sent successfully</td>
<%
End If
%>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->