<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
Dim lngQuoteStatusId
Dim strMsg

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

lngQid = CLng(Request("Qid"))
lngQuoteStatusId = CLng(Request("QuoteStatusId"))

sql = "Select QuoteStatus From QuoteStatus Where QuoteStatusId = " & lngQuoteStatusId
Set rsStatus = dbConn.Execute(sql)

strQuoteStatus = rsStatus("QuoteStatus")

sql = "Update Quotes Set QuoteStatusId = " & lngQuoteStatusId & " Where Qid = " & lngQid
dbConn.Execute(sql)

strMsg = "Quote Status updated successfully."

' Audit trail
sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Status changed to " & strQuoteStatus & "', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

If lngQuoteStatusId = 9 Then
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : The next approver in the approval process is " & GetQuoteNextLineApprover(lngQid) & "." & QuoteDetails_ForEmail(lngQid)
	SendMail Request.Cookies("UserSettings")("Email"), GetQuoteNextLineApprover_Email(lngQid), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Waiting for your approval.", strBodyText
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<html>
<head>
<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<script language="javascript">
<%
If lngQuoteStatusId = 4 Then ' Accepted
%>
		if(confirm('Do you want to invoice this quote?')) {
			try {
				if(window.opener.document.parentWindow.location.href.indexOf('View.asp') > 0) {
					window.opener.document.parentWindow.window.opener.document.parentWindow.RedirectPage_Global('Transporter_QuoteToInvoice.asp?Qid=<%= lngQid %>');
					window.opener.document.parentWindow.RefreshWindowClose()
				} else {
					window.opener.document.parentWindow.RedirectPage_Global('Transporter_QuoteToInvoice.asp?Qid=<%= lngQid %>');
				}
			} catch(e) {
			}
		}
	try {
//		RefreshIFrame_Global_Opener();
		window.close();
	} catch(e) {
	}
<%
Else
%>
	alert('<%= strMsg %>');
	RefreshIFrame_Global_Opener();
	document.location.href = 'View.asp?Qid=<%= lngQid %>';
<%
End If
%>
</script>
</head>
<body>
</body>
</html>