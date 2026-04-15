<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid
Dim lngPOStatusId
Dim strNotes
Dim strMsg

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

lngPOid = CLng(Request("POid"))
lngPOStatusId = CLng(Request("POStatusId"))
strNotes = Trim(Replace(Request("Notes"), "'", "''"))

sql = "Select POStatus From PurchaseOrderStatus Where POStatusId = " & lngPOStatusId
Set rsStatus = dbConn.Execute(sql)

sql = "Update PurchaseOrders Set POStatusId = " & lngPOStatusId & " Where POid = " & lngPOid
dbConn.Execute(sql)

' Audit trail
sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Status updated: " & rsStatus("POStatus") & "', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

strMsg = "Purchase Order Status updated successfully."

If lngPOStatusId = 2 Then
	sql = "Select * From PurchaseOrders Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)

	' Approval Process
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " is now Pending Approval. The next approver in the approval process is " & GetPONextLineApprover(lngPOid, CBool(rsPO("HasCapEx"))) & "." & PurchaseOrderDetails_ForEmail(lngPOid)
	SendMail Request.Cookies("UserSettings")("Email"), GetPONextLineApprover_Email(lngPOid, CBool(rsPO("HasCapEx"))), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Waiting for your approval.", strBodyText
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<html>
<head>
<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<script language="javascript">
	alert('<%= strMsg %>');
	RefreshIFrame_Global_Opener();
	document.location.href = 'default.asp';
</script>
</head>
<body>
</body>
</html>